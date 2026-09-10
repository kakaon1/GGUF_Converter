using System.Text;

namespace gguf_Converter
{
    /// <summary>
    /// GGUF 파일 자체 검증기.
    ///
    /// llama.cpp 실행 파일이 없어도 검증이 가능하도록, 파일 구조를 직접 파싱한다.
    /// llama.cpp 가 모델을 로드할 때 실제로 확인하는 항목들을 그대로 점검한다.
    ///
    /// 파일 구조 (전부 리틀엔디언)
    ///   [헤더]
    ///     char   magic[4]           "GGUF"
    ///     uint32 version            현재 최신 3
    ///     uint64 tensor_count
    ///     uint64 metadata_kv_count
    ///   [메타데이터 KV 블록]  key(string) + value_type(uint32) + value
    ///   [텐서 정보 블록]      name(string) + n_dims(uint32) + dims[n](uint64) + type(uint32) + offset(uint64)
    ///   [정렬 패딩]           general.alignment (기본 32) 배수로 맞춤
    ///   [텐서 데이터]         offset 은 이 블록 시작점 기준 상대값
    ///
    /// 참고 : https://github.com/ggml-org/ggml/blob/master/docs/gguf.md
    /// </summary>
    internal static class GgufValidator
    {
        // ── 메타데이터 값 타입 ──
        private const uint T_UINT8 = 0, T_INT8 = 1, T_UINT16 = 2, T_INT16 = 3;
        private const uint T_UINT32 = 4, T_INT32 = 5, T_FLOAT32 = 6, T_BOOL = 7;
        private const uint T_STRING = 8, T_ARRAY = 9, T_UINT64 = 10, T_INT64 = 11, T_FLOAT64 = 12;

        /// <summary>ggml 텐서 타입 : 이름 / 블록당 원소 수 / 블록당 바이트 수</summary>
        private static readonly Dictionary<uint, (string Name, int BlockSize, int TypeSize)> GgmlTypes = new()
        {
            [0]  = ("F32",      1,   4),
            [1]  = ("F16",      1,   2),
            [2]  = ("Q4_0",    32,  18),
            [3]  = ("Q4_1",    32,  20),
            [6]  = ("Q5_0",    32,  22),
            [7]  = ("Q5_1",    32,  24),
            [8]  = ("Q8_0",    32,  34),
            [9]  = ("Q8_1",    32,  36),
            [10] = ("Q2_K",   256,  84),
            [11] = ("Q3_K",   256, 110),
            [12] = ("Q4_K",   256, 144),
            [13] = ("Q5_K",   256, 176),
            [14] = ("Q6_K",   256, 210),
            [15] = ("Q8_K",   256, 292),
            [16] = ("IQ2_XXS", 256,  66),
            [17] = ("IQ2_XS",  256,  74),
            [18] = ("IQ3_XXS", 256,  98),
            [19] = ("IQ1_S",   256,  50),
            [20] = ("IQ4_NL",   32,  18),
            [21] = ("IQ3_S",   256, 110),
            [22] = ("IQ2_S",   256,  82),
            [23] = ("IQ4_XS",  256, 136),
            [24] = ("I8",        1,   1),
            [25] = ("I16",       1,   2),
            [26] = ("I32",       1,   4),
            [27] = ("I64",       1,   8),
            [28] = ("F64",       1,   8),
            [29] = ("IQ1_M",   256,  56),
            [30] = ("BF16",      1,   2),
            [34] = ("TQ1_0",   256,  54),
            [35] = ("TQ2_0",   256,  66),
        };

        internal sealed class TensorInfo
        {
            public string Name = string.Empty;
            public ulong[] Dims = Array.Empty<ulong>();
            public uint Type;
            public ulong Offset;
            public ulong ElementCount;
            public ulong ByteSize;
            public bool TypeKnown;
            public string TypeName = string.Empty;
        }

        internal sealed class Result
        {
            public bool Ok;
            public List<string> Errors = new();
            public List<string> Warnings = new();
            public List<string> Info = new();

            public uint Version;
            public ulong TensorCount;
            public ulong KvCount;
            public ulong Alignment = 32;
            public ulong DataStart;
            public string Architecture = string.Empty;
            public long FileSize;
            public ulong TotalParams;
            public Dictionary<string, int> TypeHistogram = new();

            /// <summary>한 줄 요약.</summary>
            public string Summary()
            {
                if (!Ok) return string.Join(" / ", Errors);

                string quant = string.Join(", ",
                    TypeHistogram.OrderByDescending(kv => kv.Value).Take(3).Select(kv => $"{kv.Key} {kv.Value}"));

                return $"GGUF v{Version} / arch={Architecture} / 텐서 {TensorCount:N0} / "
                     + $"파라미터 {FormatParams(TotalParams)} / 주요타입 {quant}";
            }

            private static string FormatParams(ulong n) =>
                n >= 1_000_000_000 ? (n / 1e9).ToString("0.##") + "B"
              : n >= 1_000_000     ? (n / 1e6).ToString("0.##") + "M"
              : n >= 1_000         ? (n / 1e3).ToString("0.##") + "K"
              : n.ToString();
        }

        /// <summary>GGUF 파일을 파싱하고 llama.cpp 로드 가능성을 점검한다.</summary>
        internal static Result Validate(string path)
        {
            var r = new Result();

            try
            {
                var fi = new FileInfo(path);

                if (!fi.Exists)
                {
                    r.Errors.Add("파일이 존재하지 않습니다.");
                    return r;
                }

                r.FileSize = fi.Length;

                if (fi.Length < 24)
                {
                    r.Errors.Add($"파일이 너무 작습니다. ({fi.Length} bytes / 헤더 최소 24 bytes)");
                    return r;
                }

                using var fs = File.OpenRead(path);
                using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

                // ── 1. 헤더 ──
                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (magic != "GGUF")
                {
                    r.Errors.Add($"GGUF 매직이 아닙니다. (읽은 값 \"{Sanitize(magic)}\")");
                    return r;
                }

                r.Version = br.ReadUInt32();
                if (r.Version == 0 || r.Version > 3)
                {
                    r.Errors.Add($"지원 범위를 벗어난 버전입니다. (version={r.Version} / 알려진 최신 3)");
                    return r;
                }

                r.TensorCount = br.ReadUInt64();
                r.KvCount = br.ReadUInt64();

                if (r.TensorCount == 0) r.Errors.Add("텐서 개수가 0 입니다.");
                if (r.KvCount == 0) r.Errors.Add("메타데이터 항목이 0 입니다.");
                if (r.TensorCount > 1_000_000) r.Errors.Add($"텐서 개수가 비정상입니다. ({r.TensorCount})");
                if (r.KvCount > 1_000_000) r.Errors.Add($"메타데이터 개수가 비정상입니다. ({r.KvCount})");
                if (r.Errors.Count > 0) return r;

                // ── 2. 메타데이터 KV ──
                var meta = new Dictionary<string, string>(StringComparer.Ordinal);

                for (ulong i = 0; i < r.KvCount; i++)
                {
                    string key = ReadString(br, fi.Length);
                    uint vtype = br.ReadUInt32();
                    string? scalar = ReadValue(br, vtype, fi.Length, out bool isArray, out ulong arrCount);

                    meta[key] = isArray ? $"[배열 {arrCount:N0}개]" : (scalar ?? string.Empty);
                }

                // ── 3. 필수 메타데이터 확인 (llama.cpp 로드 조건) ──
                if (meta.TryGetValue("general.architecture", out string? arch) && !string.IsNullOrWhiteSpace(arch))
                {
                    r.Architecture = arch;
                }
                else
                {
                    r.Errors.Add("general.architecture 가 없습니다. llama.cpp 가 모델 종류를 판단할 수 없습니다.");
                }

                if (meta.TryGetValue("general.alignment", out string? alignText)
                    && ulong.TryParse(alignText, out ulong align) && align > 0)
                {
                    r.Alignment = align;
                    if ((align & (align - 1)) != 0)
                        r.Errors.Add($"general.alignment 가 2의 거듭제곱이 아닙니다. ({align})");
                }

                bool hasTokModel = meta.ContainsKey("tokenizer.ggml.model");
                bool hasTokens = meta.ContainsKey("tokenizer.ggml.tokens");

                if (!hasTokModel && !hasTokens)
                    r.Warnings.Add("토크나이저 메타데이터가 없습니다. 텍스트 생성 모델이라면 llama.cpp 로드에 실패합니다.");
                else if (!hasTokens)
                    r.Warnings.Add("tokenizer.ggml.tokens 가 없습니다.");

                // ── 4. 텐서 정보 ──
                var tensors = new List<TensorInfo>((int)Math.Min(r.TensorCount, 100_000));
                var names = new HashSet<string>(StringComparer.Ordinal);

                for (ulong i = 0; i < r.TensorCount; i++)
                {
                    var t = new TensorInfo { Name = ReadString(br, fi.Length) };

                    if (!names.Add(t.Name))
                        r.Errors.Add($"텐서 이름이 중복됩니다 : {Sanitize(t.Name)}");

                    uint nDims = br.ReadUInt32();
                    if (nDims == 0 || nDims > 4)
                    {
                        r.Errors.Add($"텐서 \"{Sanitize(t.Name)}\" 의 차원 수가 비정상입니다. ({nDims} / 허용 1~4)");
                        return r;
                    }

                    t.Dims = new ulong[nDims];
                    ulong elems = 1;

                    for (uint d = 0; d < nDims; d++)
                    {
                        t.Dims[d] = br.ReadUInt64();
                        if (t.Dims[d] == 0)
                            r.Errors.Add($"텐서 \"{Sanitize(t.Name)}\" 의 {d}번 차원이 0 입니다.");
                        elems = checked(elems * Math.Max(t.Dims[d], 1));
                    }

                    t.ElementCount = elems;
                    t.Type = br.ReadUInt32();
                    t.Offset = br.ReadUInt64();

                    if (GgmlTypes.TryGetValue(t.Type, out var info))
                    {
                        t.TypeKnown = true;
                        t.TypeName = info.Name;

                        if (elems % (ulong)info.BlockSize != 0)
                            r.Errors.Add($"텐서 \"{Sanitize(t.Name)}\" 원소 수({elems})가 "
                                       + $"{info.Name} 블록 크기({info.BlockSize})의 배수가 아닙니다.");

                        t.ByteSize = elems / (ulong)info.BlockSize * (ulong)info.TypeSize;
                        r.TypeHistogram[info.Name] = r.TypeHistogram.GetValueOrDefault(info.Name) + 1;
                    }
                    else
                    {
                        t.TypeName = $"unknown({t.Type})";
                        r.Warnings.Add($"텐서 \"{Sanitize(t.Name)}\" 의 타입 {t.Type} 을 모릅니다. "
                                     + "이 도구보다 새로운 ggml 타입일 수 있습니다.");
                        r.TypeHistogram[t.TypeName] = r.TypeHistogram.GetValueOrDefault(t.TypeName) + 1;
                    }

                    if (t.Offset % r.Alignment != 0)
                        r.Errors.Add($"텐서 \"{Sanitize(t.Name)}\" 의 offset({t.Offset})이 "
                                   + $"정렬 단위({r.Alignment})의 배수가 아닙니다.");

                    r.TotalParams += elems;
                    tensors.Add(t);
                }

                // ── 5. 텐서 데이터 시작 위치 (정렬 패딩 적용) ──
                ulong pos = (ulong)fs.Position;
                r.DataStart = (pos + r.Alignment - 1) / r.Alignment * r.Alignment;

                // ── 6. 파일 크기와 텐서 데이터 정합성 ──
                ulong required = 0;
                foreach (var t in tensors)
                {
                    if (!t.TypeKnown) continue;
                    ulong end = t.Offset + t.ByteSize;
                    if (end > required) required = end;
                }

                ulong needTotal = r.DataStart + required;

                if ((ulong)fi.Length < needTotal)
                {
                    r.Errors.Add($"파일이 잘렸습니다. 필요 {needTotal:N0} bytes / 실제 {fi.Length:N0} bytes "
                               + $"(부족 {needTotal - (ulong)fi.Length:N0} bytes)");
                }
                else
                {
                    ulong extra = (ulong)fi.Length - needTotal;
                    if (extra > r.Alignment)
                        r.Warnings.Add($"텐서 데이터 뒤에 {extra:N0} bytes 의 여분이 있습니다.");
                }

                // ── 7. 정보 요약 ──
                r.Info.Add($"버전            : {r.Version}");
                r.Info.Add($"아키텍처        : {r.Architecture}");
                r.Info.Add($"텐서 수         : {r.TensorCount:N0}");
                r.Info.Add($"메타데이터 수   : {r.KvCount:N0}");
                r.Info.Add($"파라미터 수     : {r.TotalParams:N0}");
                r.Info.Add($"정렬 단위       : {r.Alignment}");
                r.Info.Add($"데이터 시작     : {r.DataStart:N0}");
                r.Info.Add($"파일 크기       : {r.FileSize:N0} bytes");
                r.Info.Add($"토크나이저      : {(hasTokModel ? meta["tokenizer.ggml.model"] : "없음")}"
                         + (hasTokens ? $" / tokens {meta["tokenizer.ggml.tokens"]}" : ""));

                foreach (string key in new[] { "general.name", "general.file_type", "general.quantization_version" })
                    if (meta.TryGetValue(key, out string? v))
                        r.Info.Add($"{key,-15} : {v}");

                r.Info.Add("텐서 타입 분포  : " + string.Join(", ",
                    r.TypeHistogram.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}")));

                r.Ok = r.Errors.Count == 0;
                return r;
            }
            catch (EndOfStreamException)
            {
                r.Errors.Add("파일이 예상보다 일찍 끝났습니다. 변환이 중단되었거나 파일이 손상되었습니다.");
                return r;
            }
            catch (OverflowException)
            {
                r.Errors.Add("텐서 크기 계산이 오버플로했습니다. 헤더 값이 손상된 것으로 보입니다.");
                return r;
            }
            catch (Exception ex)
            {
                r.Errors.Add("파싱 중 오류 : " + ex.Message);
                return r;
            }
        }

        // ════════════════════════════════════════════════════════════
        // 내부 읽기 도우미
        // ════════════════════════════════════════════════════════════

        /// <summary>GGUF 문자열 : uint64 길이 + UTF-8 바이트</summary>
        private static string ReadString(BinaryReader br, long fileSize)
        {
            ulong len = br.ReadUInt64();

            if (len > (ulong)fileSize)
                throw new InvalidDataException($"문자열 길이({len})가 파일 크기를 넘습니다.");

            if (len > 64 * 1024 * 1024)
                throw new InvalidDataException($"문자열 길이({len})가 비정상입니다.");

            if (len == 0) return string.Empty;

            byte[] buf = br.ReadBytes((int)len);
            if (buf.Length < (int)len) throw new EndOfStreamException();

            return Encoding.UTF8.GetString(buf);
        }

        /// <summary>메타데이터 값을 읽는다. 스칼라는 문자열로 반환하고, 배열은 건너뛴다.</summary>
        private static string? ReadValue(BinaryReader br, uint type, long fileSize,
                                         out bool isArray, out ulong arrayCount)
        {
            isArray = false;
            arrayCount = 0;

            switch (type)
            {
                case T_UINT8:   return br.ReadByte().ToString();
                case T_INT8:    return br.ReadSByte().ToString();
                case T_UINT16:  return br.ReadUInt16().ToString();
                case T_INT16:   return br.ReadInt16().ToString();
                case T_UINT32:  return br.ReadUInt32().ToString();
                case T_INT32:   return br.ReadInt32().ToString();
                case T_FLOAT32: return br.ReadSingle().ToString("G6");
                case T_BOOL:    return br.ReadByte() != 0 ? "true" : "false";
                case T_UINT64:  return br.ReadUInt64().ToString();
                case T_INT64:   return br.ReadInt64().ToString();
                case T_FLOAT64: return br.ReadDouble().ToString("G6");
                case T_STRING:  return ReadString(br, fileSize);

                case T_ARRAY:
                {
                    isArray = true;
                    uint elemType = br.ReadUInt32();
                    arrayCount = br.ReadUInt64();

                    if (arrayCount > (ulong)fileSize)
                        throw new InvalidDataException($"배열 길이({arrayCount})가 파일 크기를 넘습니다.");

                    for (ulong i = 0; i < arrayCount; i++)
                        ReadValue(br, elemType, fileSize, out _, out _);

                    return null;
                }

                default:
                    throw new InvalidDataException($"알 수 없는 메타데이터 값 타입 : {type}");
            }
        }

        /// <summary>로그/메시지에 넣기 안전하도록 제어문자를 제거한다.</summary>
        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsControl(c) ? '?' : c);
            return sb.Length > 120 ? sb.ToString(0, 120) + "..." : sb.ToString();
        }
    }
}
