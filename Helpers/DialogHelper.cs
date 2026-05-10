using System.IO;
using System.Windows.Forms;

namespace ArknightsLauncher.Helpers
{
    public static class DialogHelper
    {
        public static string SelectFolder()
        {
            while (true)
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "请选择 Arknights 根目录",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return "";
                string selectedPath = dialog.SelectedPath;
                if (File.Exists(Path.Combine(selectedPath, "Arknights.exe")))
                    return selectedPath;
                MessageBox.Show("未找到 'Arknights.exe'，请重新选择游戏根目录", "错误");
            }
        }

        public static string SelectExe(string title, string filterName)
        {
            string displayFilterName = string.IsNullOrWhiteSpace(filterName) ? "可执行文件" : filterName;
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = $"{displayFilterName} (*.exe)|*.exe|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : "";
        }
    }
}
