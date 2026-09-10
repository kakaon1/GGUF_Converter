통합 에이전트 작업 지침서

적용 대상: Claude / ChatGPT / Gemini 등 모든 AI 에이전트 공통 사용

※ 이 문서의 파일명은 현재 사용 중인 에이전트를 기준으로 결정한다.
   (예: Claude → CLAUDE.md / ChatGPT·Codex → AGENTS.md / Gemini → GEMINI.md)
   파일명을 이 문서에 고정하지 않는다.

================================================================================
프로젝트 기초 항목
================================================================================
- 프로젝트명   : gguf_Converter
- 운영체제     : Windows
- 사용 언어    : C# (.NET 8.0 / WinForms)
- 문서 작성일  : 2026-07-21

2번 스캔 결과
- ① 운영체제 : Windows (C:\Users\Administrator\Desktop\gguf_Converter) — 확정, 이후 재확인 생략
- ② 소스파일 / 프로젝트 파일 : 스캔 시점에 없음 → 개발 준비 단계로 전환

개발 준비 단계 질의 결과 (지침 2번)
- 개발 언어 : C#        → 지침 4번 조항 적용 (6번 Python 조항 미적용)
- 개발 방식 : 신규 생성

프로젝트 목적
- 폴더명 gguf_Converter 에 따라, 모델을 GGUF 형식으로 변환하는 Windows GUI 도구를 개발한다.
- llama.cpp 의 convert_hf_to_gguf.py 및 llama-quantize 를 호출하여 변환·양자화를 수행한다.

프로젝트 구조 (솔루션 루트 = 프로젝트 루트, .vs 는 루트에만 생성)
- gguf_Converter.sln
- gguf_Converter.csproj      Release 조건부 Publish 설정 + DependentUpon + SubType + ApplicationIcon
- Program.cs                 진입점, Application.SetDefaultFont
- Form1.cs                   비즈니스 로직 (입력 판별, 변환 절차, 프로세스 실행, 설정 저장)
- Form1.Designer.cs          UI 자동 생성 코드
- Form1.resx                 리소스 파일 (폼 아이콘 $this.Icon 임베드)
- GgufValidator.cs           GGUF 자체 검증기 (외부 실행 파일 의존 없음)
- app.ico                    실행 파일 아이콘 (16~256 9종 포함)
- CLAUDE.md                  본 문서
- README.md                  프로젝트 설명 문서
- CODEMAP.md                 오류 이력 및 조치 기록 (지침 7번)

실행 방법
- Visual Studio 에서 Release 구성으로 빌드하면 AutoPublish 타깃이 자동 게시한다.
- 결과물 : bin\Release\Publish\gguf_Converter.exe (Self-Contained 단일 파일, pdb 없음)

기능 요약
- 입력 형식 자동 판별 (매직 바이트 + 확장자 + 상위 폴더 config.json)
    HF 폴더 / .safetensors / .bin / .pth / .pt / GGML / .gguf
    파일을 골라도 HF 모델이면 상위 폴더로 자동 치환. 드래그 앤 드롭 지원.
- 변환 : convert_hf_to_gguf.py 또는 convert_llama_ggml_to_gguf.py
- 양자화 : llama-quantize.exe (10종)
- 검증 3단계 : GgufValidator.cs 자체 검증 (필수, llama.cpp 불필요)
- 검증 4단계 : llama-bench / llama-cli 실제 로드 (보조, 없으면 건너뜀)

검증 설계 원칙
- 변환 목적이 llama.cpp 에서의 구동이므로 헤더 확인만으로 끝내지 않는다.
- 다만 llama.cpp 실행 파일이 없는 환경에서도 검증이 가능해야 하므로,
  구조 검증은 전부 자체 파서로 수행하고 실행 파일 검증은 보조로 둔다.
- 실행 파일이 없다는 이유로 변환을 실패 처리하지 않는다.
- 자체 검증기는 합성 GGUF 9개 케이스(정상 1 / 이상 8)로 알고리즘을 확인하였다.
    정상 / 매직손상 / 버전초과 / 파일잘림 / 정렬위반 /
    architecture 누락 / 텐서수 0 / 블록배수 위반 / 토크나이저 없음(경고)

환경 설정 (지침 4번 준수 상태)
- TargetFramework            : net8.0-windows
- UI 프레임워크              : WinForms (WPF 미사용)
- 빌드 구성                  : Release 전용 (Debug 금지)
- RuntimeIdentifier          : win-x64 (Release 조건부)
- SelfContained              : true
- PublishSingleFile          : true
- EnableCompressionInSingleFile : true
- DebugType / DebugSymbols   : none / false
- PublishDir                 : bin\Release\Publish\

외부 의존 (프로그램에 포함하지 않음)
- python (PATH 등록 필요)
- llama.cpp 의 convert_hf_to_gguf.py
- llama-quantize.exe (F16 이외 양자화 시에만 필요)

Form Designer 필수 7개 — 정적 점검 결과
- 1) Form1.cs / Form1.Designer.cs / Form1.resx 3종 세트          : 충족
- 2) csproj DependentUpon 연결 (2건)                              : 충족
- 3) 기본 폰트 속성 csproj 미포함 + Program.cs 코드 설정          : 충족
- 4) Publish 설정 Release 조건부 분리 (전역 RuntimeIdentifier 없음): 충족
- 5) InitializeComponent() 직후 if (DesignMode) return;           : 충족
- 6) 폰트명 영문 / 이모지 없음 / Controls.Add Fill→Top 역순       : 충족
- 7) 서브폴더 .vs 없음                                            : 충족

※ 위는 7개 규칙의 정적 충족 여부 점검 결과이다.
  에이전트는 Visual Studio 를 실행해 디자이너를 실제로 열 수 없으므로
  "디자이너 정상 동작 확인" 으로 단정하지 않는다.
  실제 디자이너 열림은 Visual Studio 에서 사용자 확인이 필요하다.

지침 5번 워크플로우 수행 결과
- 3단계 전체 소스 점검 : 완료
    필수 7개 전수 정적 점검 통과
    이벤트 핸들러 8개 ↔ Form1.cs 구현 대조 통과
    Designer 선언 컨트롤 ↔ Form1.cs 사용 컨트롤 대조 통과
    아이콘 2종(실행 전 / 실행 후) 바이트 일치 확인
    자체 검증기 알고리즘 합성 케이스 9건 전수 통과
    문서·소스 전체 UTF-8 BOM 확인
- 4단계 Release 빌드   : 사용자가 Visual Studio 에서 수행. 2026-07-21 16:06 통과 확인.
    bin\Release\Publish\gguf_Converter.exe  71,622,246 bytes (Self-Contained 단일 파일)
    pdb 0개 — 지침 152행 충족 (전역 DebugType=none 적용 결과)
    16:07 settings.ini 생성 확인 → 프로그램 실행 및 정상 종료까지 확인됨
  ※ 에이전트 실행 환경에는 .NET SDK 가 없고 Microsoft 도메인이 차단되어 있어
    에이전트가 직접 컴파일할 수 없다. Release 빌드는 사용자 구간이다.
- 5단계 문서 갱신      : CLAUDE.md 와 README.md 동시 갱신 완료

재빌드 결과 (2026-07-21)
- 위 변경사항을 반영한 Release 재빌드 통과, 실행 정상 확인.
- 폼 아이콘 코드 로드 방식(LoadFormIcon) 도 정상 동작 확인.

16:06 빌드 이후 변경된 사항 (위 재빌드로 검증 완료)
- 폼 아이콘 구현 방식 변경
    (변경 전) Form1.resx 에 $this.Icon 바이너리 임베드 — 16:06 빌드에서 정상 동작 확인됨
    (변경 후) app.ico 를 EmbeddedResource 로 넣고 Form1.LoadFormIcon() 에서 스트림 로드
  ※ 에이전트가 "resx 타입 문자열 해석이 실패할 수 있다" 고 판단해 변경했으나,
    16:06 빌드 결과로 변경 전 방식도 정상 동작함이 확인되었다.
    확인 없이 동작하던 구현을 바꾼 판단 오류로 기록한다.
- 안전장치 추가 (아래 4건)
- 죽은 코드 FormatSize 제거, SetRunning 호출 위치 조정

안전장치 (데이터 손실 방지)
- 입력 파일과 출력 파일 경로가 같으면 변환을 시작하지 않는다.
  (같은 파일을 읽으면서 쓰면 파일이 손상된다)
- 출력 파일이 이미 있으면 덮어쓰기 전에 확인한다.
- 중간 F16 파일명이 기존 파일과 겹치면 .f16.tmpN.gguf 로 비켜 간다.
  (사용자가 갖고 있던 동명 파일을 덮어쓴 뒤 중간 파일로 오인해 삭제하는 사고 방지)
- 입력이 이미 GGUF 인 경우 그 파일은 사용자 원본이므로 절대 삭제하지 않는다.

미검증 항목 (사용자 확인 필요)
- 위 "16:06 빌드 이후 변경된 사항" 반영 재빌드
- 실제 모델 파일을 이용한 변환 성공 여부
- 실제 GGUF 파일에 대한 자체 검증기 동작
  (합성 파일 9건으로 알고리즘은 확인했으나 실물 모델로는 미확인)

작업 이력
- 2026-07-21  에이전트 문서 생성 (지침 1번, 운영체제/사용 언어 "미정")
- 2026-07-21  워크스페이스 스캔 → 운영체제 Windows 확정, 소스 없음 확인 (지침 2번)
- 2026-07-21  개발 준비 단계 질의 → 개발 언어 C# / 개발 방식 신규 생성 확정
- 2026-07-21  C# WinForms 프로젝트 신규 생성 및 GGUF 변환 기능 구현 (지침 4번)
- 2026-07-21  README.md 작성, CLAUDE.md 동시 갱신 (지침 5번)
- 2026-07-21  폼 크기 1/4 축소 (860x560 → 440x300)
- 2026-07-21  아이콘 제작 및 적용 (실행 전 ApplicationIcon / 실행 후 resx 임베드)
- 2026-07-21  입력 형식 자동 판별 추가 (다중 확장자 대응)
- 2026-07-21  Debug 산출물 pdb 생성 문제 조치 (CODEMAP.md 항목 1)
- 2026-07-21  csproj SubType=Form 명시 (CODEMAP.md 항목 2)
- 2026-07-21  GgufValidator.cs 자체 검증기 도입 — llama.cpp 없이 검증 가능

================================================================================

1. 현재 사용 중인 에이전트를 인식하여 해당 에이전트에 맞는 파일명으로 문서를 즉시 생성한다.
   ※ 이 항목은 모든 작업에서 가장 먼저 실행되어야 하며, 어떠한 경우에도 생략할 수 없다.

   생성 규칙
   - 에이전트 문서는 프로젝트 루트에 하나의 파일만 생성한다.
   - 사용자의 별도 지시를 기다리지 않는다. 작업 시작 즉시 생성한다.
   - 문서가 이미 존재하는 경우에는 5번 워크플로우의 업데이트 조건에 따라 갱신한다.
   - 다른 에이전트에서도 참조할 수 있도록 통합 문서 형태를 유지한다.

   최초 생성 시 반드시 포함해야 할 기초 항목
   - 프로젝트명
   - 운영체제      → 최초 생성 시 "미정"으로 기록, 2번 스캔 완료 즉시 갱신
   - 사용 언어     → 최초 생성 시 "미정"으로 기록, 2번 스캔 완료 즉시 갱신
   - 문서 작성일

   ※ 1번에서 기반 문서를 먼저 생성한 뒤, 2번 스캔 결과를 반영하여 문서를 업데이트한다.
      2번 스캔이 완료되기 전까지 "미정" 항목을 확정값으로 덮어쓰지 않는다.

2. 작업 시작 시 워크스페이스를 스캔하여 프로젝트 환경을 파악하고, 1번에서 생성한 문서에 기록한다.

   스캔 항목 및 순서
   ① 운영체제 확인 (Windows / Linux / Mac) → 문서에 기록, 이후 OS 관련 불필요한 확인 생략
   ② 소스파일 또는 프로젝트 파일 존재 여부 확인

   소스파일 또는 프로젝트 파일이 발견된 경우
   - 사용 언어 및 프레임워크를 자동으로 판별한다.
   - 판별 기준 예시:
       .csproj / .sln           → C#
       .py                      → Python
       package.json             → Node.js
       pom.xml / build.gradle   → Java
       .dpr / .dpk              → Delphi
   - 판별된 언어를 문서에 기록한다.
   - 복수의 언어가 혼재하는 경우 각각 기록하고 주력 언어를 명시한다.
   - 이후 아래 순서로 반드시 진행한 뒤 작업 준비를 완료한다.
       ① 에이전트 문서 / README.md 등 프로젝트 관련 문서 전체 읽기
       ② 소스 파일 전체 읽기
       ③ 프로젝트 구조 및 흐름 파악
       ④ 작업 지시 대기

   소스파일 또는 프로젝트 파일이 없는 경우
   - 개발 준비 단계로 전환하여 사용자에게 다음을 질의한다.
       1. 개발 언어 (C# / Python / Node.js / Java / Delphi / 기타)
       2. 개발 방식 (신규 생성 / 기존 구조 참고 등)
   - 답변을 받기 전까지 언어 환경 구성은 보류한다.
   - 언어가 Python으로 결정된 경우 6번으로 이동하여 개발 환경을 구성한다.

3. 모든 작업 및 표기 사항은 한국어로 기록하며,
   md 파일 또한 한국어로 작성한다.

   파일 인코딩 기준
   - 문서 파일 (.md, .txt 등)         → UTF-8 (BOM 포함) 또는 ANSI
   - 배치 파일 (.bat) [Windows 한정]  → ANSI
   - 인코딩을 지키지 않을 경우 한글 깨짐 및 실행 오류가 발생할 수 있다.

4. C#으로 개발할 경우 아래 기준을 따른다.

   빌드 설정
   - .NET 8.0 기준으로 작성한다.
   - 모든 빌드는 무조건 Release로 진행한다. Debug 빌드는 사용하지 않는다.
     코드 수정 후 재빌드(빌드 재시도 포함) 시에도 반드시 Release로만 시도한다.
   - Release 빌드 결과물은 .NET Runtime이 설치되지 않은 환경에서도 즉시 실행
     가능해야 한다. (.NET Runtime / .NET Framework 별도 설치 불필요 — Self-Contained 필수)
   - 빌드 결과물은 bin\Release\Publish 폴더에 단일 파일로 생성되어야 한다.
   - pdb 파일은 생성되지 않아야 한다.

   Publish 자동화
   - Windows 환경에서는 Visual Studio를 통해 빌드한다.
   - 프로젝트 파일(.csproj)을 수정하여 빌드 시 자동으로 게시(Publish)가 진행되도록 설정한다.

   Publish를 사용하는 이유
   - 단일 실행 파일(.exe)로 배포하여 별도의 파일 없이 실행 가능하도록 한다.
   - .NET 런타임을 실행 파일에 포함(Self-Contained)시켜 사용자가 .NET Framework 또는
     .NET Runtime을 별도로 설치하지 않아도 즉시 실행 가능한 환경을 제공한다.
   - 위 두 가지 목적을 반드시 충족하도록 .csproj 설정을 구성한다.

   .csproj 필수 설정값
   - PublishSingleFile     → true       (단일 파일로 묶기)
   - SelfContained         → true       (.NET Runtime 포함 — 별도 설치 불필요)
   - RuntimeIdentifier     → win-x64    (배포 대상 플랫폼)
   - EnableCompressionInSingleFile → true (파일 크기 최소화)
   - DebugType             → none       (pdb 미생성)
   - DebugSymbols          → false      (디버그 심볼 제외)

   ※ 위 Publish 전용 설정은 반드시 Release 조건부 PropertyGroup 안에 넣는다.
     조건 없이(전역) 두면 Visual Studio의 디자인타임 빌드(Debug / Any CPU 기반)가
     RuntimeIdentifier=win-x64 플랫폼 제약에 걸려 Form Designer가 동작하지 않는다.

     <PropertyGroup Condition="'$(Configuration)'=='Release'">
       <RuntimeIdentifier>win-x64</RuntimeIdentifier>
       <SelfContained>true</SelfContained>
       <PublishSingleFile>true</PublishSingleFile>
       <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
       <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
       <DebugType>none</DebugType>
       <DebugSymbols>false</DebugSymbols>
       <PublishDir>bin\Release\Publish\</PublishDir>
     </PropertyGroup>

   GUI 개발 (WinForms 전용)
   - C# GUI는 무조건 WinForms로 제작한다. WPF는 배제한다.
     WPF가 필요한 경우 반드시 사용자의 별도 지시를 받은 뒤에만 사용한다.
   - GUI 프로그램 제작 시 디자이너 파일(.Designer.cs)을 반드시 포함한다.
   - Visual Studio에서 폼 디자이너로 직접 열어 수정할 수 있도록 설계한다.
   - 디자이너 파일을 누락하거나 코드로만 UI를 구성하는 방식은 사용하지 않는다.
   - 아래 "Form Designer 정상 동작 규칙"을 모두 충족해야 디자이너가 정상 표기된다.
     하나라도 누락되면 디자이너가 코드 뷰로 열리거나 로드 자체가 실패한다.
     (아래 예시는 폼 이름이 Form1인 경우 기준. 실제 폼 이름에 맞춰 적용한다.)

   Form Designer 정상 동작 규칙 (모두 필수)

   ▶ 필수 7개 요약 — 하나라도 누락되면 디자이너가 실패한다.
     매 GUI 작업마다 이 7개를 먼저 전수 점검한 뒤 아래 상세 규칙으로 들어간다.
       1) Form*.cs / Form*.Designer.cs / Form*.resx 3종 세트 (resx 비어도 필수)
       2) csproj DependentUpon 연결
       3) ApplicationDefaultFont를 csproj에 두지 않음 (Program.cs에서 코드 설정)
       4) Publish 전용 설정을 Release 조건부 PropertyGroup으로 분리
       5) 생성자 InitializeComponent() 직후 if (DesignMode) return;
       6) Designer.cs 폰트명 영문 + 이모지 없음 + Controls.Add 순서(Fill→Top 역순)
       7) .vs 폴더는 .sln 옆(솔루션 루트)에만 위치

   ▶ 에이전트 검증 범위의 한계 — 반드시 인지한다.
     - 에이전트가 할 수 있는 검증은 위 7개 규칙의 "정적 충족 여부 점검"까지다.
     - 에이전트는 Visual Studio를 실행해 디자이너를 실제로 열 수 없다.
       따라서 "디자이너 정상 동작 확인" / "검증 완료"로 단정하지 않는다.
     - 정적 점검 통과 시 보고는 "규칙 7개 정적 충족 — 실제 디자이너 열림은
       Visual Studio에서 사용자 확인 필요" 형태로 한다.

   ─ 상세 규칙 ─

   ① 파일 구성 — 폼당 3개 파일을 같은 폴더에 둔다.
      Form1.cs           → 비즈니스 로직 + partial class 선언
      Form1.Designer.cs  → InitializeComponent() 자동 생성 코드
      Form1.resx         → 리소스 파일 (내용이 비어도 반드시 존재)
      ※ Form1.resx가 없으면 디자이너가 열리지 않는다.

   ② csproj 디자이너 연결 — DependentUpon으로 짝을 명시한다.
      <ItemGroup>
        <Compile Update="Form1.Designer.cs">
          <DependentUpon>Form1.cs</DependentUpon>
        </Compile>
        <EmbeddedResource Update="Form1.resx">
          <DependentUpon>Form1.cs</DependentUpon>
        </EmbeddedResource>
      </ItemGroup>
      ※ DependentUpon이 없으면 VS가 Designer.cs/resx를 Form1.cs의 짝으로 인식하지 못한다.

   ③ ApplicationDefaultFont는 csproj에 넣지 않는다.
      - csproj의 ApplicationDefaultFont 값은 디자인타임에 FontConverter로 파싱되며,
        폰트명에 공백·한글이 있으면 파싱 실패 → 디자이너 로드가 막힌다.
      - 폰트는 csproj 대신 Program.cs에서 코드로 설정한다.

        [STAThread]
        static void Main()
        {
            Application.SetDefaultFont(new System.Drawing.Font("Malgun Gothic", 9F));
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

   ④ Publish 전용 설정은 Release 조건부로 분리한다. (위 .csproj 필수 설정값 참조)
      - RuntimeIdentifier=win-x64 등을 조건 없이 두면 디자인타임 빌드가
        플랫폼 제약에 걸려 디자이너가 동작하지 않는다.

   ⑤ Form 생성자 순서를 고정한다.
      public Form1()
      {
          InitializeComponent();   // 항상 첫 번째
          if (DesignMode) return;  // 그 직후 DesignMode 체크
          InitializePaths();       // 런타임 전용 초기화 (파일 I/O / 네트워크 / 외부 프로세스)
          LoadSettings();
      }
      ※ DesignMode 체크 없이 파일 I/O·경로 계산·외부 프로세스 호출을 하면 디자이너가
        폼을 인스턴스화할 때 예외가 발생해 열리지 않는다.

   ⑥ Designer.cs 작성 제약
      - 폰트명은 반드시 영문으로 쓴다. (예: "맑은 고딕" → "Malgun Gothic")
        한글 폰트명은 디자이너에서 파싱 오류를 일으킨다.
      - 이모지를 넣지 않는다. (예: "▶ 실행 🚀" → "▶ 실행")
        이모지가 있으면 디자이너 렌더링이 실패한다. (▶ 등 일반 유니코드 기호는 허용)
      - Controls.Add 순서: Dock=Fill 컨트롤을 맨 먼저 추가하고,
        Dock=Top 컨트롤들은 아래쪽 → 위쪽 역순으로 추가한다.
          Controls.Add(pnlFill);    // Dock=Fill
          Controls.Add(pnlBottom);  // Dock=Top (아래)
          Controls.Add(pnlMiddle);  // Dock=Top (중간)
          Controls.Add(pnlTop);     // Dock=Top (맨 위)

   ⑦ .vs 폴더는 .sln과 같은 폴더(솔루션 루트)에만 둔다.
      - 프로젝트를 서브폴더로 옮길 때 .vs까지 같이 옮기면 VS가 디자이너 상태를
        찾지 못한다. 잘못된 위치의 .vs는 삭제하고 VS를 재시작하면 올바른 위치에
        자동 재생성된다.

   디자이너 생성 후 자가 점검
      - 위 "필수 7개 요약"을 전수 정적 점검한다. (5번 워크플로우 3단계에서 동일 확인)
      - 통과해도 "정상 동작 확인"으로 단정하지 않는다. (위 검증 한계 참조)

   증상별 원인·해결 (반복 발생 시 7번 코드맵에도 기록)
      - 디자이너가 코드 뷰로 열림       → DependentUpon 누락 → csproj에 추가
      - "구문 분석할 수 없습니다" 오류    → csproj ApplicationDefaultFont 파싱 실패
                                          → 제거 후 Program.cs에서 코드로 설정
      - 디자이너 로드 중 예외            → DesignMode 체크 누락 → if (DesignMode) return; 추가
      - 빈 폼 / 컨트롤 미표시            → Dock 순서 오류 → Controls.Add 순서 수정
      - 디자이너 완전히 안 보임          → RuntimeIdentifier 무조건 설정 → Release 조건 추가
      - 재시작 후에도 안 됨             → .vs 위치 오류 → 서브폴더 .vs 삭제 후 VS 재시작

   디자이너 여는 법 (사용자 안내용)
      - Solution Explorer에서 Form1.cs 우클릭 → "디자이너 보기" (Shift+F7)
      - 또는 코드 편집 창 상단 탭에서 [디자인] 탭으로 전환

5. 아래의 워크플로우를 준수한다.
   ──────────────────────────────────────────────────────────────
    1. 작업 지시 접수
    2. 소스 수정 / 삭제 / 추가
    3. 전체 소스 점검
       ※ C# GUI 프로젝트의 경우 (4번 GUI 개발 조항 준수)
          - WinForms 사용 여부 확인 (WPF 사용 시 별도 지시 유무 확인)
          - .Designer.cs 파일 포함 여부 확인
          - 4번 "필수 7개 요약" 전수 정적 점검
            (정적 충족만 확인 가능, 실제 디자이너 열림은 VS에서 사용자 확인)
    4. 언어 감지 후 분기
       ├─ [Python] → 실행
       │     ├─ [성공] → 결과 알림 → 5단계로 이동
       │     └─ [실패] → 코드맵 작성 (7번)
       │                 원인 분석 및 수정 제안
       │                 ※ 실제 수정은 개발자 구간
       │                 수정 완료 후 전체 소스 점검 → 실행 재시도
       └─ [그 외] → Release 빌드 진행 (Debug 금지)
             ├─ [성공] → 5단계로 이동
             └─ [실패] → 코드맵 작성 (7번)
                         원인 분석 및 수정 제안
                         ※ 실제 수정은 개발자 구간
                         수정 완료 후 전체 소스 점검 → Release 빌드 재시도
    5. 에이전트 문서 / README.md 업데이트 (아래 업데이트 조건 해당 시만)
    6. 작업 결과 기록
   ──────────────────────────────────────────────────────────────

   에이전트 문서 업데이트 조건 (해당 항목 변경 시에만 기록)
   ──────────────────────────────────────────────────────────
    ✓ 프로젝트 구조 변경
    ✓ 실행 방법 변경
    ✓ 환경 설정 변경
    ✓ 반복 오류 발생 → 코드맵과 함께 기록
    ✗ 단순 코드 수정 → 기록하지 않는다
   ──────────────────────────────────────────────────────────

   ※ 에이전트 문서와 README.md는 항상 함께 업데이트한다.
      에이전트 문서만 단독으로 업데이트하거나 README.md만 단독으로 업데이트하지 않는다.
      README.md는 프로젝트 설명 파일로, 에이전트 문서의 변경 내용이 반영되어야 한다.

6. Python 개발 환경은 아래 순서로 구성한다.

   ① Anaconda 설치 여부 확인

   확인 방법 (Windows)
   where conda >nul 2>&1 && echo INSTALLED || echo NOT INSTALLED

   확인 방법 (Linux / Mac)
   command -v conda &>/dev/null && echo INSTALLED || echo NOT INSTALLED

   ② 가상환경 구성

   Anaconda가 설치되어 있는 경우
   - Anaconda 기반 가상환경(conda env)을 생성하여 개발 환경을 구성한다.
   - 가상환경 이름은 프로젝트명을 기준으로 임의 지정한다.
   - venv / virtualenv 등 다른 방식은 사용하지 않는다.
   - → ③-A 파일을 생성한다.

   Anaconda가 설치되어 있지 않은 경우
   - 가상환경 없이 시스템에 설치된 Python(base)을 직접 사용한다.
   - 문서에 해당 사실을 명시한다.
   - → ③-B 파일을 생성한다.

   ③ ②의 판별 결과에 따라 아래 해당 파일을 생성한다.
   requirements.txt는 작성하지 않는다.
   패키지 설치(install_package)와 실행(run)을 반드시 분리하여 제공한다.
   모든 배치 파일(.bat)은 반드시 ANSI 인코딩으로 저장한다.

   ③-A [ Anaconda 설치된 경우 ]

   Windows

   install_package.bat — 패키지 설치 전용
   @echo off
   call conda init
   call conda create -n [프로젝트명] python=3.9 -y
   call conda activate [프로젝트명]
   pip install [필요 패키지]
   echo 설치 완료.
   pause

   run.bat — 실행 전용
   @echo off
   call conda activate [프로젝트명]
   python main.py
   pause

   Linux / Mac

   install_package.sh — 패키지 설치 전용
   source "$(conda info --base)/etc/profile.d/conda.sh"
   conda create -n [프로젝트명] python=3.9 -y
   conda activate [프로젝트명]
   pip install [필요 패키지]
   echo "설치 완료."

   run.sh — 실행 전용
   source "$(conda info --base)/etc/profile.d/conda.sh"
   conda activate [프로젝트명]
   python main.py

   ③-B [ Anaconda 미설치 — base Python 사용 ]

   Windows

   install_package.bat — 패키지 설치 전용
   @echo off
   pip install [필요 패키지]
   echo 설치 완료.
   pause

   run.bat — 실행 전용
   @echo off
   python main.py
   pause

   Linux / Mac

   install_package.sh — 패키지 설치 전용
   pip install [필요 패키지]
   echo "설치 완료."

   run.sh — 실행 전용
   python main.py

7. 빌드 또는 실행 오류 발생 시 즉시 코드맵을 작성한다. 코드맵 없이 수정을 진행하지 않는다.
   저장 위치: 프로젝트 루트 / CODEMAP.md
   - 파일이 없으면 신규 생성, 있으면 항목 추가 (이력 누적)
   - 동일 오류가 재발한 경우 코드맵에서 이전 조치 내용을 먼저 확인한다.
   - 조치 완료 후 5번 워크플로우의 빌드 재시도 루틴으로 복귀한다.

   코드맵 기록 형식
   ────────────────────────────────────────────────────────
    오류 발생일:
    발생 단계:  (아래 실행 흐름의 해당 번호 기재)
    오류 내용:
    원인 분석:
    조치 내용:
    재발 방지:
   ────────────────────────────────────────────────────────

   실행 흐름 기준 (발생 단계 특정용 — 빌드/실행 공통 파이프라인)
    1. 환경 / 경로 초기화
    2. 의존성 / 패키지 로드
    3. 컴파일                  (빌드 언어 한정)
    4. 링크 / 게시(Publish)    (빌드 언어 한정)
    5. 런타임 초기화
    6. 주요 처리 로직 (세부 내용 기술)
    7. 결과 출력
    8. 정리 / 종료

   발생 단계 기재 기준
   - 빌드 오류 → 1~4 중 해당 단계 (예: 컴파일 에러=3, win-x64 게시 실패=4)
   - 실행 오류 → 5~8 중 해당 단계 (예: 시작 직후 예외=5, 처리 중 예외=6)
   - 단계 번호와 함께 실패한 구체 동작을 적는다.
     (예: "4 / win-x64 단일파일 게시 중 RID 복원 실패")

================================================================================
변경 이력
================================================================================
날짜           변경 내용                                       구분
--------------------------------------------------------------------------------
2026-03-16     최초 문서 작성                                  추가
