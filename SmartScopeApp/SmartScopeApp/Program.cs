using System.Windows.Forms;
namespace SmartScopeApp
{
    internal static class Program
    {
        [System.STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
