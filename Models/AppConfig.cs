using System.Collections.Generic;
using System.Linq;

namespace ArknightsLauncher.Models
{
    public class AppConfig
    {
        public string RootPath { get; set; } = "";                          // 方舟根目录路径
        public string MAA_Official { get; set; } = "";                      // 联动软件（官服）路径，兼容旧 MAA 配置字段
        public string MAA_Bilibili { get; set; } = "";                      // 联动软件（B服）路径，兼容旧 MAA 配置字段
        public string LinkedSoftwareOfficialName { get; set; } = "MAA-官";  // 官服联动软件按钮名称
        public string LinkedSoftwareBilibiliName { get; set; } = "MAA-B";   // B服联动软件按钮名称
        public bool EnableLinkedSoftware { get; set; } = true;              // 是否启用联动软件，兼容旧配置字段
        public bool EnableLinkedSoftwareOfficial { get; set; } = true;      // 是否启用官服联动软件
        public bool EnableLinkedSoftwareBilibili { get; set; } = true;      // 是否启用B服联动软件
        public bool LinkedSoftwareLegacyDefaultsCleared { get; set; } = false; // 是否已清理旧 MAA 字段自动带出的默认联动项
        public List<LinkedSoftwareItem> LinkedSoftwares { get; set; }
            = new List<LinkedSoftwareItem>();                               // 可启动的联动软件列表，兼容旧配置字段
        public List<LinkedSoftwareItem> LinkedSoftwaresOfficial { get; set; }
            = new List<LinkedSoftwareItem>();                               // 官服联动软件列表
        public List<LinkedSoftwareItem> LinkedSoftwaresBilibili { get; set; }
            = new List<LinkedSoftwareItem>();                               // B服联动软件列表

        public Dictionary<string, string> Accounts { get; set; }
            = new Dictionary<string, string>();

        public string DefaultAccount { get; set; } = "";                    // 默认账号 ID（如 "A1"）
        public bool IsFirstRun { get; set; } = true;                        // 是否首次运行（用于控制首次点击官服时解锁 B 服按钮）
        public string LastNotifiedVersion { get; set; } = "";               // 上次通知的版本号
        public bool ShowTrayIcon { get; set; } = false;                     // 是否显示托盘图标
        public bool MinimizeToTray { get; set; } = false;                   // 关闭主窗口时是否最小化到托盘
        public bool AutoLaunchOfficial { get; set; } = false;               // 启动时自动打开官服
        public bool AutoLaunchBilibili { get; set; } = false;               // 启动时自动打开B服
        public bool ExitAfterLaunch { get; set; } = false;                    // 游戏启动完毕后自动关闭软件

        public string GetLinkedSoftwareOfficialName()
        {
            return string.IsNullOrWhiteSpace(LinkedSoftwareOfficialName)
                ? "MAA-官"
                : LinkedSoftwareOfficialName;
        }

        public string GetLinkedSoftwareBilibiliName()
        {
            return string.IsNullOrWhiteSpace(LinkedSoftwareBilibiliName)
                ? "MAA-B"
                : LinkedSoftwareBilibiliName;
        }

        public List<LinkedSoftwareItem> GetLinkedSoftwareItems()
        {
            return GetLinkedSoftwareItems(true)
                .Concat(GetLinkedSoftwareItems(false))
                .GroupBy(item => item.Path, System.StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        public List<LinkedSoftwareItem> GetLinkedSoftwareItems(bool isOfficial)
        {
            var items = isOfficial
                ? LinkedSoftwaresOfficial ?? new List<LinkedSoftwareItem>()
                : LinkedSoftwaresBilibili ?? new List<LinkedSoftwareItem>();
            return CleanLinkedSoftwareItems(items);
        }

        public void NormalizeLinkedSoftwares()
        {
            ClearLegacyLinkedSoftwareDefaults();
            NormalizeLinkedSoftwares(true);
            NormalizeLinkedSoftwares(false);
        }

        public void NormalizeLinkedSoftwares(bool isOfficial)
        {
            var normalizedItems = GetLinkedSoftwareItems(isOfficial)
                .GroupBy(item => item.Path, System.StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            if (isOfficial) LinkedSoftwaresOfficial = normalizedItems;
            else LinkedSoftwaresBilibili = normalizedItems;
        }

        public bool IsLinkedSoftwareEnabled(bool isOfficial)
        {
            return isOfficial ? EnableLinkedSoftwareOfficial : EnableLinkedSoftwareBilibili;
        }

        public string GetLinkedSoftwareButtonName(bool isOfficial)
        {
            var items = GetLinkedSoftwareItems(isOfficial);
            if (items.Count == 1 && !string.IsNullOrWhiteSpace(items[0].Name))
                return items[0].Name;
            return isOfficial ? "官服联动" : "B服联动";
        }

        private static List<LinkedSoftwareItem> CleanLinkedSoftwareItems(IEnumerable<LinkedSoftwareItem> items)
        {
            return items
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .ToList();
        }

        private void ClearLegacyLinkedSoftwareDefaults()
        {
            if (LinkedSoftwareLegacyDefaultsCleared)
                return;

            ClearLegacyLinkedSoftwareDefaults(true);
            ClearLegacyLinkedSoftwareDefaults(false);
            LinkedSoftwareLegacyDefaultsCleared = true;
        }

        private void ClearLegacyLinkedSoftwareDefaults(bool isOfficial)
        {
            string legacyPath = isOfficial ? MAA_Official : MAA_Bilibili;
            if (string.IsNullOrWhiteSpace(legacyPath))
                return;

            var items = isOfficial ? LinkedSoftwaresOfficial : LinkedSoftwaresBilibili;
            if (items.Count != 1)
                return;

            if (string.Equals(items[0].Path, legacyPath, System.StringComparison.OrdinalIgnoreCase))
                items.Clear();
        }

    }

    public class LinkedSoftwareItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";

        public override string ToString()
        {
            string name = string.IsNullOrWhiteSpace(Name)
                ? System.IO.Path.GetFileNameWithoutExtension(Path)
                : Name;
            return string.IsNullOrWhiteSpace(Path) ? name : $"{name} - {Path}";
        }
    }
}
