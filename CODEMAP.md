코드맵 (CODEMAP.md)

지침 7번에 따라 빌드 또는 실행 오류 발생 시 기록한다.
동일 오류 재발 시 아래 이전 조치 내용을 먼저 확인한다.

================================================================================
항목 1
================================================================================
오류 발생일: 2026-07-21

발생 단계: 3 / Debug 구성으로 컴파일이 수행되어 pdb 가 생성됨

오류 내용:
- 지침 4번은 아래 두 가지를 요구한다.
    147행 "모든 빌드는 무조건 Release로 진행한다. Debug 빌드는 사용하지 않는다."
    152행 "pdb 파일은 생성되지 않아야 한다."
  그러나 실제로는 Debug 빌드가 수행되어 아래 파일이 생성되었다.
    bin\Debug\net8.0-windows\gguf_Converter.pdb
    obj\Debug\net8.0-windows\gguf_Converter.pdb
- 지침 151행이 요구하는 산출물 bin\Release\Publish 는 생성되지 않았다.

원인 분석:
- 지침 176~185행의 예시는 DebugType / DebugSymbols 를 Release 조건부
  PropertyGroup 안에 두도록 되어 있고, csproj 는 그 예시를 그대로 따랐다.
- Visual Studio 의 기본 구성은 Debug 이므로 사용자가 구성을 바꾸지 않고 빌드하면
  Release 조건부 설정이 적용되지 않아 pdb 가 생성된다.
- 즉 csproj 가 지침 예시를 어긴 것이 아니라, 지침 예시만으로는
  152행("pdb 파일은 생성되지 않아야 한다")을 구성과 무관하게 보장하지 못한다.

조치 내용:
- DebugType=none / DebugSymbols=false 를 전역 PropertyGroup 에 추가하여
  구성과 무관하게 pdb 가 생성되지 않도록 했다.
- 지침 176~185행의 Release 조건부 PropertyGroup 은 예시 그대로 유지했다.
  (RuntimeIdentifier / SelfContained / PublishSingleFile 등 Publish 전용 설정)
- 이미 생성된 Debug 산출물(bin, obj)을 정리했다.
- 솔루션의 Debug 구성 자체는 제거하지 않았다.
  제거하면 디자인타임 빌드가 Release 기준이 되어 RuntimeIdentifier=win-x64
  플랫폼 제약에 걸리고, 지침 172~174행이 경고하는 디자이너 미동작 상황이 발생한다.

재발 방지:
- 빌드 후 점검 항목에 아래 2가지를 추가한다.
    1. *.pdb 파일이 존재하지 않는가
    2. bin\Release\Publish\gguf_Converter.exe 가 생성되었는가
- 빌드는 반드시 Visual Studio 상단 구성을 Release 로 변경한 뒤 수행한다.

주의 : DebugType 전역 설정은 플랫폼 제약과 무관하므로 디자이너에 영향이 없다.
       RuntimeIdentifier 를 전역으로 옮기는 것은 금지된다. (지침 172~174행)

================================================================================
항목 2
================================================================================
오류 발생일: 2026-07-21

발생 단계: 1 / Solution Explorer 에서 디자이너 파일이 보이지 않는다는 보고

오류 내용:
- 사용자가 "디자이너 파일도 없는데?" 라고 지적.

원인 분석:
- Form1.Designer.cs 는 디스크에 정상 존재한다. (13,729 bytes)
- 지침 ②(224~233행)의 DependentUpon 이 정상 동작하여 Solution Explorer 에서
  Form1.cs 하위로 접혀 들어간 상태이다. Form1.cs 를 펼쳐야 보인다.
  즉 지침 ②가 의도한 결과이며 결함이 아니다.
- 보강 : Visual Studio 는 프로젝트를 열면서 gguf_Converter.csproj.user 를 만들고
  그 안에 <SubType>Form</SubType> 을 기록했다. 이는 SDK 스타일 WinForms 에서
  흔한 정상 동작이나, .user 파일은 사용자별 로컬 파일이라 형상관리 대상이 아니다.
  삭제되거나 다른 PC 로 옮기면 정보가 사라진다.

조치 내용:
- csproj 의 ItemGroup 에 Form1.cs 에 대한 <SubType>Form</SubType> 을 명시하여
  .user 파일에 의존하지 않도록 했다. (지침에 금지 조항 없음, 지침 ② 보강)
- Form1.resx 에 <SubType>Designer</SubType> 을 추가했다.
- DependentUpon 은 지침 ② 예시대로 유지했다.

재발 방지:
- 에이전트 판정 오류 기록 : 최초에 "SubType 누락이 디자이너 미표시의 원인" 이라고
  확인 없이 단정하여 코드맵에 사실처럼 기재했다가 정정했다.
  원인을 확정하기 전에는 "확인 필요" 로 기재하고 단정하지 않는다.
- 필수 7개 규칙 2 정적 점검 시, DependentUpon 문자열 존재 여부만으로
  "충족" 을 보고하지 않는다. 중첩 표시는 VS 에서 사용자 확인이 필요하다.

================================================================================
