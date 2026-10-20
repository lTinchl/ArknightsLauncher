using ArknightsLauncher.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Views;
using System;
using System.IO;
using System.Windows.Forms;

namespace ArknightsLauncher
{
    public enum ServerType
    {
        Official,       // 官服
        Bilibili,       // B服
        LinkedSoftwareOfficial,   // 官服联动软件
        LinkedSoftwareBilibili,   // B服联动软件
        GitHub          // Github
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Directory.CreateDirectory(
                ConfigHelper.AccountBackupDir
            );

            var app = new System.Windows.Application();
            app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("Views/Themes/LauncherTheme.xaml", UriKind.Relative)
            });
            HandyControl.Themes.ThemeManager.Current.UsingWindowsAppTheme = false;
            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = HandyControl.Themes.ApplicationTheme.Dark;

            app.Run(
                new ArknightsLauncher.Views.MainWindow()
            );
        }

    }
}


