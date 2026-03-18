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
                string folderName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar));

                if (folderName.Equals("Arknights Game", System.StringComparison.OrdinalIgnoreCase))
                    return selectedPath;

                MessageBox.Show("请选择名为 'Arknights Game' 的文件夹作为根目录", "错误");
            }
        }

        public static string SelectExe(string title, string filterName)
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = "MAA.exe|MAA.exe",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : "";
        }
    }
}
