namespace gguf_Converter
{
    internal static class Program
    {
        /// <summary>
        /// 애플리케이션 진입점.
        /// 지침 4번 규칙 3 : 기본 폰트는 csproj 가 아닌 여기서 코드로 설정한다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetDefaultFont(new System.Drawing.Font("Malgun Gothic", 9F));
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
