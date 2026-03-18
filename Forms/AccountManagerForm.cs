using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;

namespace ArknightsLauncher.Forms
{
    public class AccountManagerForm : Form
    {
        private ListBox listBox;
        private Button btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename;
        private Label hintLabel;

        public AccountManagerForm()
        {
            Text = "账号管理";
            Size = new Size(350, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            hintLabel = new Label
            {
                Text = "双击列表项可重命名",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            Controls.Add(hintLabel);

            listBox = new ListBox { Dock = DockStyle.Top, Height = 250 };
            listBox.DoubleClick += RenameAccount;
            Controls.Add(listBox);

            btnAdd        = new Button { Text = "新增",   Width = 70 };
            btnDelete     = new Button { Text = "删除",   Width = 70 };
            btnBackup     = new Button { Text = "备份当前", Width = 90 };
            btnSetDefault = new Button { Text = "设为默认", Width = 90 };
            btnRename     = new Button { Text = "重命名",  Width = 90 };

            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60 };
            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename });

            Controls.Add(listBox);
            Controls.Add(panel);

            btnAdd.Click        += AddAccount;
            btnDelete.Click     += DeleteAccount;
            btnBackup.Click     += BackupCurrent;
            btnSetDefault.Click += SetDefault;
            btnRename.Click     += RenameAccount;

            LoadAccounts();
        }

        private void LoadAccounts()
        {
            listBox.Items.Clear();
            var cfg = ConfigHelper.Load();

            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
            {
                listBox.Items.Add(new AccountItem
                {
                    Id = cfg.DefaultAccount,
                    Remark = cfg.Accounts[cfg.DefaultAccount] + " ⭐"
                });
            }

            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue;
                listBox.Items.Add(new AccountItem { Id = acc.Key, Remark = acc.Value });
            }
        }

        private void AddAccount(object sender, EventArgs e)
        {
            string remark = Microsoft.VisualBasic.Interaction.InputBox("请输入账号备注", "新增账号");
            if (string.IsNullOrWhiteSpace(remark)) return;

            var cfg = ConfigHelper.Load();
            int index = 1;
            while (cfg.Accounts.ContainsKey("A" + index)) index++;
            string id = "A" + index;

            cfg.Accounts[id] = remark;
            ConfigHelper.Save(cfg);

            Directory.CreateDirectory(Path.Combine(ConfigHelper.AccountBackupDir, id));
            LoadAccounts();
        }

        private void RenameAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            string current = selectedItem.Remark.Replace(" ⭐", "");
            string newRemark = Microsoft.VisualBasic.Interaction.InputBox($"修改 {current}", "重命名", current);
            if (string.IsNullOrWhiteSpace(newRemark)) return;

            var cfg = ConfigHelper.Load();
            cfg.Accounts[selectedItem.Id] = newRemark;
            ConfigHelper.Save(cfg);
            LoadAccounts();
        }

        private void DeleteAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            var cfg = ConfigHelper.Load();
            if (cfg.Accounts.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个账号", "提示");
                return;
            }

            if (MessageBox.Show($"确认删除 {selectedItem.Remark.Replace(" ⭐", "")}？", "确认",
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            cfg.Accounts.Remove(selectedItem.Id);
            if (cfg.DefaultAccount == selectedItem.Id) cfg.DefaultAccount = "";
            ConfigHelper.Save(cfg);

            string backupDir = Path.Combine(ConfigHelper.AccountBackupDir, selectedItem.Id);
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);

            LoadAccounts();
        }

        private void SetDefault(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            var cfg = ConfigHelper.Load();
            cfg.DefaultAccount = selectedItem.Id;
            ConfigHelper.Save(cfg);
            LoadAccounts();
        }

        private async void BackupCurrent(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            await GameLauncher.BackupAccount(selectedItem.Id);
            MessageBox.Show("备份完成", "成功");
        }
    }
}
