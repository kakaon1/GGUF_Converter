gguf_Converter

llama.cpp 에서 사용할 수 있도록 모델을 GGUF 형식으로 변환·양자화하는 Windows GUI 도구.

변환의 목적은 llama.cpp 에서 해당 모델을 구동하는 것이다.
따라서 변환이 끝나면 GGUF 헤더 검증에 그치지 않고,
llama.cpp 실행 파일로 모델을 실제로 로드해 보는 검증까지 수행한다.

==================
개발 환경
==================
- 운영체제     : Windows
- 언어         : C#
- 프레임워크   : .NET 8.0 (net8.0-windows)
- UI           : WinForms (WPF 미사용)
- IDE          : Visual Studio 2022 이상

==================
프로젝트 구조
==================
gguf_Converter\                  솔루션 루트 (.vs 는 여기에만 생성된다)
├─ CLAUDE.md                     에이전트 작업 지침 문서 (루트에 하나만)
├─ README.md                     본 문서
├─ CODEMAP.md                    오류 이력 및 조치 기록 (지침 7번)
├─ gguf_Converter.sln
├─ gguf_Converter.csproj
├─ Program.cs                    진입점, 기본 폰트 설정
├─ Form1.cs                      비즈니스 로직
├─ Form1.Designer.cs             UI 자동 생성 코드
├─ Form1.resx                    리소스 파일 (폼 아이콘 포함)
├─ GgufValidator.cs              GGUF 자체 검증기 (외부 의존 없음)
└─ app.ico                       실행 파일 아이콘

==================
사전 준비
==================
본 프로그램은 llama.cpp 를 포함하지 않는다. 아래가 준비되어 있어야 한다.

1. Python 설치
   - 명령 프롬프트에서 python 명령이 동작해야 한다. (PATH 등록 필요)
   - llama.cpp 변환 스크립트가 요구하는 패키지 설치
       cd [llama.cpp 폴더]
       pip install -r requirements.txt

2. llama.cpp 소스
   - convert_hf_to_gguf.py          (HuggingFace 모델 변환용)
   - convert_llama_ggml_to_gguf.py  (구형 GGML 파일 변환용)

3. llama.cpp 빌드 산출물
   - llama-quantize.exe : F16 이외의 양자화 형식 사용 시 필요
   - llama-bench.exe    : 보조 로드 검증용 (선택 / 없으면 llama-cli.exe, 그것도 없으면 건너뜀)
     ※ GGUF 검증 자체는 내장 검증기가 수행하므로 이 파일이 없어도 된다
   - 아래 경로를 우선 탐색하고, 없으면 llama.cpp 폴더 하위 전체를 탐색한다.
       build\bin\Release\
       build\bin\
       build\Release\

==================
지원 입력 형식
==================
입력 경로의 매직 바이트와 확장자를 함께 보고 형식을 자동 판별한다.

형식                          판별 방법                    처리
--------------------------------------------------------------------------------
HuggingFace 모델 폴더         폴더 + config.json           convert_hf_to_gguf.py
.safetensors 파일             상위 폴더의 config.json      상위 폴더를 변환
.bin / .pth / .pt (PyTorch)   상위 폴더의 config.json      상위 폴더를 변환
GGML 파일                     매직 ggml / ggmf / ggjt / ggla  convert_llama_ggml_to_gguf.py
.gguf 파일                    매직 GGUF                    1단계 생략, 양자화만 수행

- 파일을 골라도 HuggingFace 모델이면 상위 폴더 경로로 자동 치환하여 변환한다.
- 입력란에 파일이나 폴더를 끌어다 놓을 수 있다. (드래그 앤 드롭)
- 판별에 실패하면 변환을 시작하지 않고 지원 형식을 안내한다.

==================
변환 및 검증 절차
==================
1단계  GGUF(F16) 생성
       HF     : python convert_hf_to_gguf.py [모델폴더] --outfile [f16.gguf] --outtype f16
       GGML   : python convert_llama_ggml_to_gguf.py --input [파일] --output [f16.gguf]
       GGUF   : 생략 (입력 파일을 그대로 양자화 원본으로 사용)
       생성 직후 중간 파일도 GGUF 헤더 검증을 수행한다.

2단계  양자화 (양자화 형식이 F16 이면 생략)
       llama-quantize.exe [f16.gguf] [출력.gguf] [양자화형식]

3단계  GGUF 자체 검증 (항상 수행 / llama.cpp 불필요)
       GgufValidator.cs 가 파일 구조를 직접 파싱하여 검증한다.
       외부 실행 파일에 의존하지 않으므로 llama.cpp 가 없어도 검증이 가능하다.

       [오류로 판정하는 항목 — 하나라도 걸리면 실패]
         - 매직이 "GGUF" 가 아님
         - version 이 1~3 범위 밖
         - tensor_count 또는 kv_count 가 0, 또는 100만 초과
         - 메타데이터 KV 전수 파싱 실패 (알 수 없는 값 타입, 길이 이상)
         - general.architecture 누락 — llama.cpp 가 모델 종류를 판단하지 못함
         - general.alignment 가 2의 거듭제곱이 아님
         - 텐서 이름 중복
         - 텐서 차원 수가 1~4 밖이거나 차원 크기가 0
         - 텐서 원소 수가 해당 양자화 타입의 블록 크기 배수가 아님
         - 텐서 offset 이 정렬 단위의 배수가 아님
         - 파일이 잘림 (데이터 시작 + 텐서 총 바이트 > 실제 파일 크기)

       [경고로만 처리하는 항목 — 변환은 성공으로 본다]
         - 토크나이저 메타데이터 없음
         - 이 도구가 모르는 새로운 ggml 타입
         - 텐서 데이터 뒤 여분 바이트

       [함께 산출하는 정보]
         아키텍처 / 텐서 수 / 메타데이터 수 / 파라미터 수 / 정렬 단위 /
         데이터 시작 위치 / 파일 크기 / 토크나이저 종류 / 텐서 타입 분포

       ggml 타입 31종의 블록 크기와 바이트 크기를 내장하여
       텐서별 정확한 바이트 수를 계산한다.
       (F32 / F16 / BF16 / Q4_0~Q8_1 / Q2_K~Q8_K / IQ 계열 / TQ 계열)

4단계  llama.cpp 로드 검증 (체크박스, 기본 켜짐 / 보조 수단)
       llama-bench.exe -m [출력.gguf] -p 0 -n 1 -r 1
       llama-bench 가 없으면 llama-cli.exe -m [출력.gguf] -p "hi" -n 1 -no-cnv --no-warmup

       실행 파일이 없으면 "건너뜀" 으로 기록하고 변환은 성공으로 처리한다.
       파일의 유효성은 3단계 자체 검증이 이미 판정했기 때문이다.
       실행 파일이 있는데 종료 코드가 0 이 아니면 실패로 처리한다.
       이 경우 파일 구조는 정상이므로, llama.cpp 빌드가 해당 아키텍처를
       지원하지 않을 가능성을 함께 안내한다.

       모델을 실제 메모리에 올리므로 시간과 RAM 이 필요하다.
       빠르게 확인만 하려면 체크를 해제한다. 3단계는 그대로 수행된다.

1~3단계를 모두 통과해야 완료로 처리한다.
2단계까지 끝나고 3단계에서 실패하면 완료로 보고하지 않는다.

==================
빌드 및 실행
==================
1. gguf_Converter.sln 을 Visual Studio 로 연다.
2. 빌드 구성을 Release 로 설정한다. (Debug 빌드는 사용하지 않는다)
3. 빌드를 실행한다.
4. AutoPublish 타깃이 자동으로 게시를 수행하여 아래에 단일 실행 파일이 생성된다.

   bin\Release\Publish\gguf_Converter.exe

Self-Contained 단일 파일이므로 .NET Runtime 미설치 PC 에서도 실행된다.
pdb 는 구성과 무관하게 생성되지 않는다. (전역 DebugType=none)

==================
사용 방법
==================
1. llama.cpp   : convert_hf_to_gguf.py 가 있는 폴더 지정
2. 입력 모델   : [폴더] 또는 [파일] 버튼으로 지정. 드래그 앤 드롭도 가능
                 지정 즉시 형식을 판별해 로그에 표시한다
3. 출력 파일   : 저장할 .gguf 경로. 입력 지정 시 자동 제안된다
4. 양자화      : F16 / Q8_0 / Q6_K / Q5_K_M / Q5_K_S /
                 Q4_K_M / Q4_K_S / Q4_0 / Q3_K_M / Q2_K
5. F16 유지    : 체크하면 양자화 후에도 중간 F16 파일을 남긴다
                 (입력이 이미 GGUF 인 경우 원본은 삭제하지 않는다)
6. 로드 검증   : llama.cpp 로 실제 로드까지 확인한다 (기본 켜짐)
7. [변환] 실행. 진행 상황은 하단 로그에 출력된다
8. [중지] 로 진행 중인 작업을 취소할 수 있다

설정(경로 / 양자화 형식 / 체크 상태)은 실행 파일과 같은 폴더의
settings.ini 에 자동 저장되어 다음 실행 시 복원된다.

==================
화면 구성
==================
영역      Dock    구성
-------------------------------
pnlTop    Top     llama.cpp 경로 / 입력 모델(폴더·파일) / 출력 GGUF 파일
pnlOption Top     1행 : 양자화 형식, 변환, 중지
                  2행 : F16 유지, llama.cpp 로드 검증
txtLog    Fill    로그 출력 (읽기 전용, Consolas)

폼 크기 : 440 x 300 (ClientSize)

==================
아이콘
==================
- 실행 전 (탐색기 / 작업표시줄) : app.ico 를 csproj 의 ApplicationIcon 으로 지정
- 실행 후 (폼 제목표시줄)       : app.ico 를 EmbeddedResource 로 포함하고
                                  Form1.LoadFormIcon() 에서 스트림으로 읽어 설정
- 두 아이콘은 동일한 app.ico 파일 하나를 사용한다
- 포함 크기 : 16 / 20 / 24 / 32 / 40 / 48 / 64 / 128 / 256
- 아이콘 로드에 실패해도 기본 아이콘으로 정상 동작한다 (예외를 삼킨다)

==================
폼 디자이너
==================
- Solution Explorer 에서 Form1.cs 우클릭 → "디자이너 보기" (Shift+F7)
- 또는 코드 편집 창 상단의 [디자인] 탭으로 전환

Form1.Designer.cs 와 Form1.resx 는 DependentUpon 설정에 의해
Solution Explorer 에서 Form1.cs 하위로 접혀 표시된다.
Form1.cs 왼쪽 화살표를 펼쳐야 보인다.

지침 4번의 필수 7개 규칙을 적용하였다.

1. Form1.cs / Form1.Designer.cs / Form1.resx 3종 세트
2. csproj DependentUpon 연결 (+ SubType=Form 명시)
3. 기본 폰트를 csproj 가 아닌 Program.cs 에서 설정
4. Publish 전용 설정을 Release 조건부 PropertyGroup 으로 분리
5. 생성자 InitializeComponent() 직후 if (DesignMode) return;
6. Designer.cs 영문 폰트명, 이모지 미사용, Controls.Add 는 Fill → Top 역순
7. .vs 폴더는 솔루션 루트에만 위치

==================
안전장치
==================
데이터 손실을 막기 위해 변환 시작 전에 아래를 검사한다.

- 입력 파일 = 출력 파일 인 경우 변환을 시작하지 않는다.
  같은 파일을 읽으면서 쓰면 파일이 손상된다.
- 출력 파일이 이미 있으면 덮어쓰기 여부를 묻는다.
- 중간 F16 파일명이 기존 파일과 겹치면 .f16.tmpN.gguf 로 이름을 비켜 간다.
  겹친 파일을 덮어쓴 뒤 "중간 파일" 로 오인해 삭제하는 사고를 막는다.
- 입력이 이미 GGUF 인 경우 그 파일은 사용자 원본이므로 삭제 대상에서 제외한다.

==================
현재 상태
==================
- 정적 점검 : 필수 7개 규칙, 컨트롤·핸들러 정합성, 아이콘, 기능 구현 모두 통과
- 디자이너  : Visual Studio 에서 정상 표시 확인됨
- Release 빌드 : 2026-07-21 16:06 통과 확인
    bin\Release\Publish\gguf_Converter.exe  71,622,246 bytes (Self-Contained 단일 파일)
    pdb 0개, 실행 및 정상 종료 확인
  이후 아이콘 구현 방식 변경과 안전장치 4건을 반영한 재빌드도 통과, 실행 정상 확인.
- 실제 모델 변환 미검증
  실제 모델 파일과 llama.cpp 빌드 산출물이 있는 환경에서 확인이 필요하다.
- 자체 검증기는 합성 GGUF 9건(정상 1 / 이상 8)으로 알고리즘을 확인했으나
  실물 모델 파일로는 미확인이다.
- 빌드 또는 실행 오류 발생 시 CODEMAP.md 에 기록한 뒤 조치한다.

==================
참고 자료
==================
- GGUF 포맷 명세      https://github.com/ggml-org/ggml/blob/master/docs/gguf.md
- convert_hf_to_gguf  https://github.com/ggml-org/llama.cpp/blob/master/convert_hf_to_gguf.py
- llama-bench 사용법  https://github.com/ggml-org/llama.cpp/blob/master/tools/llama-bench/README.md
- gguf-py 도구        https://github.com/ggml-org/llama.cpp/blob/master/gguf-py/README.md

==================
문서 규약
==================
- 모든 문서는 한국어로 작성하며 UTF-8 (BOM 포함) 으로 저장한다.
- .bat 파일을 추가할 경우 ANSI 인코딩으로 저장한다.
- CLAUDE.md 와 README.md 는 항상 함께 갱신한다.

==================
변경 이력
==================
날짜           변경 내용                                       구분
--------------------------------------------------------------------------------
2026-07-21     최초 작성 (C# WinForms 프로젝트 신규 생성)      추가
2026-07-21     폼 크기 1/4 축소 (860x560 → 440x300)            변경
2026-07-21     아이콘 추가 (실행 전 / 실행 후 양쪽)            추가
2026-07-21     입력 형식 자동 판별 및 다중 확장자 지원         추가
2026-07-21     GGUF 헤더 검증 + llama.cpp 로드 검증 추가       추가
2026-07-21     자체 검증기(GgufValidator.cs) 도입              추가
               llama.cpp 없이도 전수 검증 가능하도록 변경
               llama.cpp 로드 검증은 보조 수단으로 격하
"# GGUF_Converter" 
