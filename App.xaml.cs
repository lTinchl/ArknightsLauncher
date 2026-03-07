using ArknightsLauncher.Services;
using Microsoft.UI.Xaml;

namespace ArknightsLauncher
{
    public partial class App : Application
    {
        private Window? _window;

        // 暴露给 SettingsDialog 获取窗口句柄用
        public Window? MainWindowInstance => _window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var cfg = ConfigService.Load();

            if (!cfg.SetupCompleted)
            {
                _window = new SetupWizardWindow();
            }
            else
            {
                _window = new MainWindow();
            }

            _window.Activate();
        }
    }
}