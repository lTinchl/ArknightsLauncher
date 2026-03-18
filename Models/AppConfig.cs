using System.Collections.Generic;

namespace ArknightsLauncher.Models
{
    public class AppConfig
    {
        public string RootPath { get; set; } = "";                          // 方舟根目录路径
        public string MAA_Official { get; set; } = "";                      // MAA 官服路径
        public string MAA_Bilibili { get; set; } = "";                      // MAA B服路径

        public Dictionary<string, string> Accounts { get; set; }
            = new Dictionary<string, string>();

        public string DefaultAccount { get; set; } = "";                    // 默认账号 ID（如 "A1"）
        public bool IsFirstRun { get; set; } = true;                        // 是否首次运行（用于控制首次点击官服时解锁 B 服按钮）
        public string LastNotifiedVersion { get; set; } = "";               // 上次通知的版本号
        public bool ShowTrayIcon { get; set; } = false;                     // 是否显示托盘图标
        public bool MinimizeToTray { get; set; } = false;                   // 关闭主窗口时是否最小化到托盘
        public bool AutoLaunchOfficial { get; set; } = false;               // 启动时自动打开官服
        public bool AutoLaunchBilibili { get; set; } = false;               // 启动时自动打开B服


    }
}
