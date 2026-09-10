using System.Diagnostics;
using System.Text;

namespace gguf_Converter
{
    /// <summary>
    /// GGUF 변환기 메인 폼 (비즈니스 로직).
    /// UI 구성은 Form1.Designer.cs 에서 담당한다.
    ///
    /// 지원 입력 형식
    ///   1. HuggingFace 모델 폴더        (config.json 포함)      → convert_hf_to_gguf.py
    ///   2. .safetensors 파일            (상위 폴더에 config.json) → 상위 폴더를 변환
    ///   3. .bin / .pth / .pt (PyTorch)  (상위 폴더에 config.json) → 상위 폴더를 변환
    ///   4. GGML 파일 (.bin, ggml/ggmf/ggjt 매직) → convert_llama_ggml_to_gguf.py
    ///   5. .gguf 파일                   → 1단계 생략, 양자화만 수행
    ///
    /// 변환 절차
    ///   1단계 : 위 형식에 맞는 변환 스크립트로 F16 GGUF 생성
    ///   2단계 : llama-quantize.exe [f16.gguf] [출력.gguf] [양자화형식]
    ///   3단계 : 출력 파일 GGUF 헤더 검증 (매직 / 버전 / 텐서 수 / 메타데이터 수)
    ///   양자화 형식이 F16 이면 2단계를 생략한다.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>입력 경로의 형식 분류.</summary>
        private enum InputKind
        {
            HfFolder,        // HuggingFace 모델 폴더
            GgmlFile,        // 구형 GGML 파일
            GgufFile,        // 이미 GGUF (양자화만 수행)
            Unknown          // 판별 불가
        }

        private string _appRoot = string.Empty;
        private string _settingsPath = string.Empty;

        private Process? _currentProcess;
        private bool _isRunning;
        private bool _isCanceled;

        public Form1()
        {
            InitializeComponent();   // 항상 첫 번째
            if (DesignMode) return;  // 지침 4번 규칙 5 : 그 직후 DesignMode 체크

            InitializePaths();
            LoadFormIcon();
            LoadSettings();
        }

        /// <summary>
        /// 폼 아이콘(실행 후 제목표시줄 / 작업표시줄)을 설정한다.
        /// app.ico 를 어셈블리에 EmbeddedResource 로 넣고 스트림으로 직접 읽는다.
        /// resx 바이너리 임베드 방식은 런타임 타입 문자열 해석에 의존하므로 사용하지 않는다.
        /// 실패해도 기본 아이콘으로 동작해야 하므로 예외를 삼킨다.
        /// </summary>
        private void LoadFormIcon()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();

                // 리소스 이름은 RootNamespace + 파일명 규칙을 따르되, 못 찾으면 끝이 맞는 것을 찾는다.
                string? resName = Array.Find(asm.GetManifestResourceNames(),
                    n => n.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase));

                if (resName == null) return;

                using Stream? stream = asm.GetManifestResourceStream(resName);
                if (stream == null) return;

                Icon = new System.Drawing.Icon(stream);
            }
            catch
            {
                // 아이콘이 없어도 프로그램 동작에는 지장이 없다.
            }
        }

        // ════════════════════════════════════════════════════════════
        // 초기화 (런타임 전용)
        // ════════════════════════════════════════════════════════════

        /// <summary>실행 경로 및 설정 파일 경로를 초기화한다.</summary>
        private void InitializePaths()
        {
            _appRoot = AppContext.BaseDirectory;
            _settingsPath = Path.Combine(_appRoot, "settings.ini");
        }

        /// <summary>이전 설정을 불러온다. 없으면 기본값을 사용한다.</summary>
        private void LoadSettings()
        {
            cboQuantType.SelectedIndex = cboQuantType.Items.IndexOf("Q4_K_M");
            if (cboQuantType.SelectedIndex < 0) cboQuantType.SelectedIndex = 0;

            try
            {
                if (File.Exists(_settingsPath))
                {
                    foreach (string line in File.ReadAllLines(_settingsPath, Encoding.UTF8))
                    {
                        int sep = line.IndexOf('=');
                        if (sep <= 0) continue;

                        string key = line.Substring(0, sep).Trim();
                        string value = line.Substring(sep + 1).Trim();

                        switch (key)
                        {
                            case "LlamaPath":
                                txtLlamaPath.Text = value;
                                break;
                            case "InputPath":
                                txtInputPath.Text = value;
                                break;
                            case "QuantType":
                                int idx = cboQuantType.Items.IndexOf(value);
                                if (idx >= 0) cboQuantType.SelectedIndex = idx;
                                break;
                            case "KeepIntermediate":
                                chkKeepIntermediate.Checked = (value == "1");
                                break;
                            case "VerifyLoad":
                                chkVerifyLoad.Checked = (value == "1");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("설정 파일을 읽지 못했습니다 : " + ex.Message);
            }

            AppendLog("GGUF Converter 시작");
            AppendLog("입력 : HF 폴더 / .safetensors / .bin / .pth / GGML / .gguf 지원");
            AppendLog("입력란에 파일이나 폴더를 끌어다 놓아도 됩니다.");
            AppendLog(new string('-', 60));
        }

        /// <summary>현재 설정을 저장한다.</summary>
        private void SaveSettings()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("LlamaPath=" + txtLlamaPath.Text.Trim());
                sb.AppendLine("InputPath=" + txtInputPath.Text.Trim());
                sb.AppendLine("QuantType=" + cboQuantType.Text);
                sb.AppendLine("KeepIntermediate=" + (chkKeepIntermediate.Checked ? "1" : "0"));
                sb.AppendLine("VerifyLoad=" + (chkVerifyLoad.Checked ? "1" : "0"));
                File.WriteAllText(_settingsPath, sb.ToString(), new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                AppendLog("설정 파일을 저장하지 못했습니다 : " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════
        // 경로 선택 / 드래그 앤 드롭
        // ════════════════════════════════════════════════════════════

        private void btnBrowseLlama_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "llama.cpp 폴더를 선택하십시오. (convert_hf_to_gguf.py 가 있는 폴더)"
            };

            if (Directory.Exists(txtLlamaPath.Text.Trim()))
                dlg.SelectedPath = txtLlamaPath.Text.Trim();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtLlamaPath.Text = dlg.SelectedPath;
                AppendLog("llama.cpp 경로 : " + dlg.SelectedPath);
            }
        }

        /// <summary>입력 모델 "폴더" 선택.</summary>
        private void btnBrowseInput_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "변환할 모델 폴더를 선택하십시오. (config.json 이 있는 폴더)"
            };

            string cur = txtInputPath.Text.Trim();
            if (Directory.Exists(cur)) dlg.SelectedPath = cur;

            if (dlg.ShowDialog(this) == DialogResult.OK)
                SetInputPath(dlg.SelectedPath);
        }

        /// <summary>입력 모델 "파일" 선택. (.safetensors / .bin / .pth / .pt / .gguf)</summary>
        private void btnBrowseInputFile_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "변환할 모델 파일을 선택하십시오.",
                Filter = "지원 모델 파일|*.safetensors;*.bin;*.pth;*.pt;*.gguf|"
                       + "SafeTensors (*.safetensors)|*.safetensors|"
                       + "PyTorch / GGML (*.bin;*.pth;*.pt)|*.bin;*.pth;*.pt|"
                       + "GGUF (*.gguf)|*.gguf|"
                       + "모든 파일 (*.*)|*.*",
                CheckFileExists = true
            };

            string cur = txtInputPath.Text.Trim();
            if (File.Exists(cur)) dlg.FileName = cur;

            if (dlg.ShowDialog(this) == DialogResult.OK)
                SetInputPath(dlg.FileName);
        }

        private void txtInputPath_DragEnter(object sender, DragEventArgs e)
        {
            if (_isRunning) return;
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void txtInputPath_DragDrop(object sender, DragEventArgs e)
        {
            if (_isRunning) return;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
                SetInputPath(paths[0]);
        }

        /// <summary>입력 경로를 설정하고 형식을 판별하여 로그에 남긴다.</summary>
        private void SetInputPath(string path)
        {
            txtInputPath.Text = path;

            InputKind kind = DetectInputKind(path, out string resolved, out string detail);
            AppendLog("입력 : " + path);
            AppendLog("판별 : " + DescribeKind(kind) + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));

            if (kind == InputKind.Unknown)
                AppendLog("경고 : 지원하지 않는 형식이거나 config.json 을 찾지 못했습니다.");

            SuggestOutputPath(resolved, kind);
        }

        private static string DescribeKind(InputKind kind) => kind switch
        {
            InputKind.HfFolder => "HuggingFace 모델",
            InputKind.GgmlFile => "GGML 파일 (구형)",
            InputKind.GgufFile => "GGUF 파일 (양자화만 수행)",
            _ => "판별 불가"
        };

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "저장할 GGUF 파일 경로를 지정하십시오.",
                Filter = "GGUF 파일 (*.gguf)|*.gguf|모든 파일 (*.*)|*.*",
                FileName = Path.GetFileName(txtOutputPath.Text.Trim())
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtOutputPath.Text = dlg.FileName;
                AppendLog("출력 파일 : " + dlg.FileName);
            }
        }

        /// <summary>입력 경로를 기준으로 출력 파일 경로를 자동 제안한다.</summary>
        private void SuggestOutputPath(string resolved, InputKind kind)
        {
            if (kind == InputKind.Unknown) return;
            if (string.IsNullOrWhiteSpace(resolved)) return;

            string baseDir, name;

            if (Directory.Exists(resolved))
            {
                baseDir = resolved;
                name = new DirectoryInfo(resolved).Name;
            }
            else
            {
                baseDir = Path.GetDirectoryName(resolved) ?? _appRoot;
                name = Path.GetFileNameWithoutExtension(resolved);
            }

            txtOutputPath.Text = Path.Combine(baseDir, name + "-" + cboQuantType.Text + ".gguf");
        }

        // ════════════════════════════════════════════════════════════
        // 입력 형식 판별
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 입력 경로의 형식을 판별한다.
        /// resolved 에는 실제로 변환 스크립트에 넘길 경로가 반환된다.
        /// (파일을 골랐어도 HF 모델이면 상위 폴더가 반환된다)
        /// </summary>
        private InputKind DetectInputKind(string path, out string resolved, out string detail)
        {
            resolved = path;
            detail = string.Empty;

            if (string.IsNullOrWhiteSpace(path)) return InputKind.Unknown;

            // ── 폴더인 경우 ──
            if (Directory.Exists(path))
            {
                if (File.Exists(Path.Combine(path, "config.json")))
                {
                    int st = Directory.GetFiles(path, "*.safetensors").Length;
                    int bn = Directory.GetFiles(path, "*.bin").Length;
                    detail = $"config.json 확인, safetensors {st}개 / bin {bn}개";
                    return InputKind.HfFolder;
                }

                detail = "폴더에 config.json 이 없습니다";
                return InputKind.Unknown;
            }

            if (!File.Exists(path)) return InputKind.Unknown;

            // ── 파일인 경우 : 매직 바이트 우선 판별 ──
            string magic = ReadMagic(path);

            if (magic == "GGUF")
            {
                detail = "GGUF 매직 확인";
                return InputKind.GgufFile;
            }

            if (magic == "ggml" || magic == "ggmf" || magic == "ggjt" || magic == "ggla")
            {
                detail = "GGML 매직 확인 (" + magic + ")";
                return InputKind.GgmlFile;
            }

            // ── 매직으로 판별 안 되면 확장자 + 상위 폴더 config.json ──
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string? parent = Path.GetDirectoryName(path);

            if (ext is ".safetensors" or ".bin" or ".pth" or ".pt")
            {
                if (parent != null && File.Exists(Path.Combine(parent, "config.json")))
                {
                    resolved = parent;
                    detail = "상위 폴더의 config.json 사용 : " + parent;
                    return InputKind.HfFolder;
                }

                detail = "상위 폴더에 config.json 이 없습니다";
                return InputKind.Unknown;
            }

            detail = "알 수 없는 확장자 : " + ext;
            return InputKind.Unknown;
        }

        /// <summary>파일 선두 4바이트를 ASCII 문자열로 읽는다.</summary>
        private static string ReadMagic(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                Span<byte> buf = stackalloc byte[4];
                if (fs.Read(buf) < 4) return string.Empty;
                return Encoding.ASCII.GetString(buf);
            }
            catch
            {
                return string.Empty;
            }
        }

        // ════════════════════════════════════════════════════════════
        // GGUF 검증
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// GGUF 파일을 자체 파서로 전수 검증한다. (llama.cpp 실행 파일 불필요)
        /// 헤더 / 메타데이터 / 텐서 정보 / 정렬 / 파일 크기 정합성까지 확인한다.
        /// verbose 가 true 이면 상세 정보를 로그에 출력한다.
        /// </summary>
        private (bool ok, string message) ValidateGguf(string path, bool verbose)
        {
            GgufValidator.Result r = GgufValidator.Validate(path);

            if (verbose)
            {
                foreach (string line in r.Info)
                    AppendLog("    " + line);
            }

            foreach (string w in r.Warnings)
                AppendLog("    [경고] " + w);

            foreach (string e in r.Errors)
                AppendLog("    [오류] " + e);

            return (r.Ok, r.Summary());
        }

        // ════════════════════════════════════════════════════════════
        // 변환 실행
        // ════════════════════════════════════════════════════════════

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            string llamaPath = txtLlamaPath.Text.Trim();
            string inputPath = txtInputPath.Text.Trim();
            string outputPath = txtOutputPath.Text.Trim();
            string quantType = cboQuantType.Text;

            // ── llama.cpp 경로 검증 ──
            if (!Directory.Exists(llamaPath))
            {
                ShowWarn("llama.cpp 경로가 올바르지 않습니다.");
                return;
            }

            // ── 입력 형식 판별 ──
            InputKind kind = DetectInputKind(inputPath, out string resolved, out string detail);

            if (kind == InputKind.Unknown)
            {
                ShowWarn("입력 형식을 판별하지 못했습니다.\n\n" + detail
                       + "\n\n지원 형식\n"
                       + " - HuggingFace 모델 폴더 (config.json 필요)\n"
                       + " - .safetensors / .bin / .pth / .pt (상위 폴더에 config.json 필요)\n"
                       + " - GGML 파일\n"
                       + " - .gguf 파일 (양자화만 수행)");
                return;
            }

            // ── 변환 스크립트 존재 확인 ──
            string script = kind == InputKind.GgmlFile
                ? Path.Combine(llamaPath, "convert_llama_ggml_to_gguf.py")
                : Path.Combine(llamaPath, "convert_hf_to_gguf.py");

            if (kind != InputKind.GgufFile && !File.Exists(script))
            {
                ShowWarn(Path.GetFileName(script) + " 를 찾을 수 없습니다.\n\n확인한 경로 :\n" + script);
                return;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                ShowWarn("출력 GGUF 파일 경로를 지정하십시오.");
                return;
            }

            string? outDir = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outDir) || !Directory.Exists(outDir))
            {
                ShowWarn("출력 폴더가 존재하지 않습니다 : " + outDir);
                return;
            }

            bool quantizeNeeded = !quantType.Equals("F16", StringComparison.OrdinalIgnoreCase);
            string quantizeExe = string.Empty;

            if (quantizeNeeded)
            {
                quantizeExe = FindQuantizeExe(llamaPath);
                if (string.IsNullOrEmpty(quantizeExe))
                {
                    ShowWarn("llama-quantize.exe 를 찾을 수 없습니다.\n\n"
                           + "llama.cpp 를 빌드한 뒤 다시 시도하거나,\n"
                           + "양자화 형식을 F16 으로 지정하십시오.");
                    return;
                }
            }

            // 입력이 이미 GGUF 인데 F16 을 고르면 할 일이 없다.
            if (kind == InputKind.GgufFile && !quantizeNeeded)
            {
                ShowWarn("입력이 이미 GGUF 이고 양자화 형식이 F16 입니다.\n수행할 작업이 없습니다.");
                return;
            }

            // 입력 파일과 출력 파일이 같으면 원본을 읽으면서 동시에 덮어쓰게 되어 파일이 깨진다.
            if (File.Exists(resolved) && PathEquals(resolved, outputPath))
            {
                ShowWarn("입력 파일과 출력 파일 경로가 같습니다.\n\n"
                       + "원본을 읽는 도중 같은 파일에 쓰게 되어 파일이 손상됩니다.\n"
                       + "출력 경로를 다르게 지정하십시오.");
                return;
            }

            // 출력 파일이 이미 있으면 덮어쓰기 전에 확인한다.
            if (File.Exists(outputPath))
            {
                var overwrite = MessageBox.Show(this,
                    "출력 파일이 이미 존재합니다. 덮어쓰시겠습니까?\n\n" + outputPath,
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (overwrite == DialogResult.No) return;
            }

            // 중간(F16) 파일 경로 결정
            string f16Path;
            bool f16IsIntermediate;   // true 인 경우에만 완료 후 삭제 대상이 된다

            if (kind == InputKind.GgufFile)
            {
                f16Path = resolved;          // 입력 GGUF 를 그대로 양자화 원본으로 사용
                f16IsIntermediate = false;   // 사용자의 원본이므로 절대 삭제하지 않는다
            }
            else if (quantizeNeeded)
            {
                f16Path = Path.Combine(outDir, Path.GetFileNameWithoutExtension(outputPath) + ".f16.gguf");

                // 같은 이름의 파일이 이미 있으면 사용자의 파일일 수 있다.
                // 덮어쓰고 나중에 삭제해 버리면 남의 파일을 지우는 셈이므로 이름을 비켜 간다.
                if (File.Exists(f16Path))
                {
                    string baseName = Path.Combine(outDir, Path.GetFileNameWithoutExtension(outputPath));
                    string candidate = string.Empty;

                    for (int n = 1; n <= 100; n++)
                    {
                        candidate = $"{baseName}.f16.tmp{n}.gguf";
                        if (!File.Exists(candidate)) break;
                        candidate = string.Empty;
                    }

                    if (string.IsNullOrEmpty(candidate))
                    {
                        ShowWarn("중간 파일 이름을 정하지 못했습니다.\n출력 폴더를 정리한 뒤 다시 시도하십시오.");
                        return;
                    }

                    AppendLog("중간 파일명 충돌 회피 : " + Path.GetFileName(f16Path)
                            + " → " + Path.GetFileName(candidate));
                    f16Path = candidate;
                }

                f16IsIntermediate = true;
            }
            else
            {
                f16Path = outputPath;
                f16IsIntermediate = false;
            }

            SaveSettings();
            SetRunning(true);
            _isCanceled = false;

            // 단계 수 = 변환(1) + 양자화(0~1) + 헤더검증(1) + 로드검증(0~1)
            int totalSteps = (kind == InputKind.GgufFile ? 0 : 1)
                           + (quantizeNeeded ? 1 : 0)
                           + 1
                           + (chkVerifyLoad.Checked ? 1 : 0);
            int step = 0;

            try
            {
                AppendLog(string.Empty);
                AppendLog(new string('=', 60));
                AppendLog("변환 시작  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                AppendLog("형식   : " + DescribeKind(kind));
                AppendLog("입력   : " + resolved);
                AppendLog("출력   : " + outputPath);
                AppendLog("양자화 : " + quantType);
                AppendLog(new string('=', 60));

                // ── 1단계 : GGUF(F16) 생성 ──
                if (kind != InputKind.GgufFile)
                {
                    step++;
                    AppendLog(string.Empty);
                    AppendLog($"[{step}/{totalSteps}] GGUF(F16) 변환 중...");

                    string args = kind == InputKind.GgmlFile
                        ? $"\"{script}\" --input \"{resolved}\" --output \"{f16Path}\""
                        : $"\"{script}\" \"{resolved}\" --outfile \"{f16Path}\" --outtype f16";

                    int exit = await RunProcessAsync("python", args, llamaPath);

                    if (_isCanceled) { AppendLog("사용자가 변환을 중지했습니다."); return; }

                    if (exit != 0)
                    {
                        AppendLog("변환 실패 (종료 코드 " + exit + ")");
                        ShowWarn("GGUF 변환에 실패했습니다.\n로그를 확인하십시오.");
                        return;
                    }

                    if (!File.Exists(f16Path))
                    {
                        AppendLog("결과 파일이 생성되지 않았습니다 : " + f16Path);
                        ShowWarn("변환 결과 파일이 생성되지 않았습니다.");
                        return;
                    }

                    // 중간 산출물도 GGUF 로 유효한지 즉시 확인한다.
                    var (midOk, midMsg) = ValidateGguf(f16Path, verbose: false);
                    AppendLog("중간 파일 검증 : " + (midOk ? "정상 — " : "실패 — ") + midMsg);

                    if (!midOk)
                    {
                        ShowWarn("1단계 결과가 올바른 GGUF 가 아닙니다.\n\n" + midMsg);
                        return;
                    }
                }

                // ── 2단계 : 양자화 ──
                if (quantizeNeeded)
                {
                    step++;
                    AppendLog(string.Empty);
                    AppendLog($"[{step}/{totalSteps}] {quantType} 양자화 중...");

                    string qArgs = $"\"{f16Path}\" \"{outputPath}\" {quantType}";
                    int exit = await RunProcessAsync(quantizeExe, qArgs, llamaPath);

                    if (_isCanceled) { AppendLog("사용자가 변환을 중지했습니다."); return; }

                    if (exit != 0)
                    {
                        AppendLog("양자화 실패 (종료 코드 " + exit + ")");
                        ShowWarn("양자화에 실패했습니다.\n로그를 확인하십시오.");
                        return;
                    }
                }

                // ── 3단계 : 최종 산출물 검증 ──
                step++;
                AppendLog(string.Empty);
                AppendLog($"[{step}/{totalSteps}] GGUF 자체 검증 중... (llama.cpp 불필요)");

                var (ok, message) = ValidateGguf(outputPath, verbose: true);

                if (!ok)
                {
                    AppendLog("자체 검증 실패 : " + message);
                    ShowWarn("변환은 끝났으나 출력 파일이 올바른 GGUF 가 아닙니다.\n\n" + message);
                    return;
                }

                AppendLog("자체 검증 통과 : " + message);

                // ── 4단계 : llama.cpp 실제 로드 검증 (있을 때만, 선택) ──
                // 자체 검증으로 구조는 이미 보장된다.
                // 이 단계는 실제 llama.cpp 빌드와의 호환성을 추가로 확인하는 보조 수단이다.
                if (chkVerifyLoad.Checked)
                {
                    step++;
                    AppendLog(string.Empty);
                    AppendLog($"[{step}/{totalSteps}] llama.cpp 로드 검증 (보조)...");

                    var (loadOk, loadMsg, toolFound) = await VerifyLoadWithLlamaAsync(llamaPath, outputPath);

                    if (_isCanceled) { AppendLog("사용자가 변환을 중지했습니다."); return; }

                    if (!toolFound)
                    {
                        // 실행 파일이 없는 것은 변환 실패가 아니다. 자체 검증 결과로 판정한다.
                        AppendLog("건너뜀 : " + loadMsg);
                        AppendLog("        자체 검증을 통과했으므로 파일 자체는 정상입니다.");
                        message += " / 로드검증 건너뜀";
                    }
                    else if (!loadOk)
                    {
                        AppendLog("로드 검증 실패 : " + loadMsg);
                        ShowWarn("자체 검증은 통과했으나 llama.cpp 가 모델을 로드하지 못했습니다.\n\n"
                               + loadMsg + "\n\n"
                               + "파일 구조는 정상이므로, llama.cpp 빌드 버전이 이 아키텍처를\n"
                               + "지원하지 않을 가능성이 있습니다. 로그를 확인하십시오.");
                        return;
                    }
                    else
                    {
                        AppendLog("로드 검증 통과 : " + loadMsg);
                        message += " / " + loadMsg;
                    }
                }

                // ── 중간 파일 정리 ──
                if (f16IsIntermediate && !chkKeepIntermediate.Checked
                    && File.Exists(f16Path) && !PathEquals(f16Path, outputPath))
                {
                    try
                    {
                        File.Delete(f16Path);
                        AppendLog("중간 F16 파일 삭제 완료");
                    }
                    catch (Exception ex)
                    {
                        AppendLog("중간 파일 삭제 실패 : " + ex.Message);
                    }
                }

                AppendLog(string.Empty);
                AppendLog(new string('=', 60));
                AppendLog("변환 완료  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                AppendLog("결과 : " + outputPath);
                AppendLog(new string('=', 60));

                MessageBox.Show(this,
                    "변환이 완료되었습니다.\n\n" + outputPath + "\n\n" + message,
                    "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("예외 발생 : " + ex.Message);
                ShowWarn("변환 중 오류가 발생했습니다.\n\n" + ex.Message);
            }
            finally
            {
                SetRunning(false);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            _isCanceled = true;
            AppendLog("중지 요청...");

            try { _currentProcess?.Kill(true); }
            catch (Exception ex) { AppendLog("프로세스 종료 실패 : " + ex.Message); }
        }

        // ════════════════════════════════════════════════════════════
        // 프로세스 실행
        // ════════════════════════════════════════════════════════════

        /// <summary>외부 프로세스를 실행하고 표준 출력/오류를 로그로 흘린다.</summary>
        private async Task<int> RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            AppendLog("> " + fileName + " " + arguments);

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // 파이썬 출력 인코딩 고정 (한글 깨짐 방지) 및 버퍼링 해제
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            proc.OutputDataReceived += (s, ev) => { if (ev.Data != null) AppendLog(ev.Data); };
            proc.ErrorDataReceived += (s, ev) => { if (ev.Data != null) AppendLog(ev.Data); };

            proc.Start();
            _currentProcess = proc;

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync();

            _currentProcess = null;
            return proc.ExitCode;
        }

        /// <summary>llama.cpp 폴더에서 llama-quantize 실행 파일을 찾는다.</summary>
        private string FindQuantizeExe(string llamaPath) => FindLlamaTool(llamaPath, "llama-quantize.exe", "quantize.exe");

        /// <summary>
        /// llama.cpp 빌드 산출물에서 지정한 실행 파일을 찾는다.
        /// 표준 빌드 경로를 먼저 확인하고, 없으면 하위 폴더 전체를 탐색한다.
        /// </summary>
        private string FindLlamaTool(string llamaPath, params string[] exeNames)
        {
            string[] dirs =
            {
                Path.Combine(llamaPath, "build", "bin", "Release"),
                Path.Combine(llamaPath, "build", "bin"),
                Path.Combine(llamaPath, "build", "Release"),
                llamaPath
            };

            foreach (string exe in exeNames)
                foreach (string dir in dirs)
                {
                    string p = Path.Combine(dir, exe);
                    if (File.Exists(p)) return p;
                }

            foreach (string exe in exeNames)
            {
                try
                {
                    string[] found = Directory.GetFiles(llamaPath, exe, SearchOption.AllDirectories);
                    if (found.Length > 0) return found[0];
                }
                catch
                {
                    // 접근 불가 폴더는 무시한다.
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// llama.cpp 실행 파일로 실제 로드를 시도하는 보조 검증.
        /// 실행 파일이 없으면 toolFound=false 로 반환하며, 이는 실패가 아니다.
        /// (파일 자체의 유효성은 GgufValidator 자체 검증이 이미 판정한다)
        /// </summary>
        private async Task<(bool ok, string message, bool toolFound)> VerifyLoadWithLlamaAsync(
            string llamaPath, string ggufPath)
        {
            string bench = FindLlamaTool(llamaPath, "llama-bench.exe");
            if (!string.IsNullOrEmpty(bench))
            {
                AppendLog("검증 도구 : " + bench);
                int exit = await RunProcessAsync(bench, $"-m \"{ggufPath}\" -p 0 -n 1 -r 1", llamaPath);
                if (_isCanceled) return (false, "사용자 중지", true);
                return exit == 0
                    ? (true, "llama-bench 로드 성공", true)
                    : (false, $"llama-bench 종료 코드 {exit}", true);
            }

            string cli = FindLlamaTool(llamaPath, "llama-cli.exe", "main.exe");
            if (!string.IsNullOrEmpty(cli))
            {
                AppendLog("검증 도구 : " + cli + "  (llama-bench 없음)");
                int exit = await RunProcessAsync(cli, $"-m \"{ggufPath}\" -p \"hi\" -n 1 -no-cnv --no-warmup", llamaPath);
                if (_isCanceled) return (false, "사용자 중지", true);
                return exit == 0
                    ? (true, "llama-cli 로드 성공", true)
                    : (false, $"llama-cli 종료 코드 {exit}", true);
            }

            return (false, "llama-bench.exe / llama-cli.exe 를 찾지 못했습니다.", false);
        }

        // ════════════════════════════════════════════════════════════
        // 공통 UI 처리
        // ════════════════════════════════════════════════════════════

        private static bool PathEquals(string a, string b) =>
            string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>실행 중 상태에 따라 컨트롤 활성화를 전환한다.</summary>
        private void SetRunning(bool running)
        {
            _isRunning = running;

            btnConvert.Enabled = !running;
            btnCancel.Enabled = running;
            btnBrowseLlama.Enabled = !running;
            btnBrowseInput.Enabled = !running;
            btnBrowseInputFile.Enabled = !running;
            btnBrowseOutput.Enabled = !running;
            txtLlamaPath.Enabled = !running;
            txtInputPath.Enabled = !running;
            txtOutputPath.Enabled = !running;
            cboQuantType.Enabled = !running;
            chkKeepIntermediate.Enabled = !running;
            chkVerifyLoad.Enabled = !running;

            Cursor = running ? Cursors.AppStarting : Cursors.Default;
        }

        private void ShowWarn(string message) =>
            MessageBox.Show(this, message, "확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        /// <summary>로그 창에 한 줄을 추가한다. (다른 스레드에서 호출 가능)</summary>
        private void AppendLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                try { txtLog.BeginInvoke(new Action<string>(AppendLog), message); }
                catch { /* 폼이 이미 닫힌 경우 무시 */ }
                return;
            }

            txtLog.AppendText(message + Environment.NewLine);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isRunning)
            {
                var result = MessageBox.Show(this,
                    "변환이 진행 중입니다. 종료하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                _isCanceled = true;
                try { _currentProcess?.Kill(true); } catch { }
            }

            SaveSettings();
            base.OnFormClosing(e);
        }
    }
}
