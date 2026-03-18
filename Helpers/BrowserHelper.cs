using System.Diagnostics;
using System.Windows.Forms;

namespace ArknightsLauncher.Helpers
{
    public static class BrowserHelper
    {
        public static void Open(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
            }
        }

        public static void OpenGitHub()        => Open(AppInfo.GitHubUrl);
        public static void OpenQuarkPan()      => Open(AppInfo.QuarkPanUrl);
        public static void OpenYituliu()       => Open(AppInfo.ArknightsYituliuUrl);
        public static void OpenToolbox()       => Open(AppInfo.ArknightsToolboxUrl);
        public static void OpenPrtsWiki()      => Open(AppInfo.PrtsWikiUrl);
    }
}
