using System.IO;
using System.Windows.Forms;
using ArknightsLauncher.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher
{
    public enum ServerType
    {
        Official,       // 官服
        Bilibili,       // B服
        MAA_Official,   // MAA 官
        MAA_Bilibili,   // MAA B
        GitHub          // Github
    }

    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Directory.CreateDirectory(ConfigHelper.AccountBackupDir);
            Application.Run(new ServerSelectForm());
        }
    }
}
