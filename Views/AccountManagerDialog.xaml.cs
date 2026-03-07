using System;
using System.Collections.ObjectModel;
using System.IO;
using ArknightsLauncher.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ArknightsLauncher
{

    public sealed partial class AccountManagerDialog : ContentDialog
    {
        private readonly ObservableCollection<AccountItem> _items = new();

        public AccountManagerDialog()
        {
            this.InitializeComponent();
            AccountList.ItemsSource = _items;
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            _items.Clear();
            var cfg = ConfigService.Load();

            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
                _items.Add(new AccountItem { Id = cfg.DefaultAccount, Remark = cfg.Accounts[cfg.DefaultAccount] + " ⭐" });

            foreach (var kv in cfg.Accounts)
            {
                if (kv.Key == cfg.DefaultAccount) continue;
                _items.Add(new AccountItem { Id = kv.Key, Remark = kv.Value });
            }
        }

        private AccountItem? GetSelected() => AccountList.SelectedItem as AccountItem;

        // ──────────────────────────────────────────────
        //  新增
        // ──────────────────────────────────────────────
        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextBox { PlaceholderText = "请输入账号备注", MaxLength = 32 };
            var dlg = new ContentDialog
            {
                Title = "新增账号",
                Content = input,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            string remark = input.Text.Trim();
            if (string.IsNullOrEmpty(remark)) return;

            var cfg = ConfigService.Load();
            int idx = 1;
            while (cfg.Accounts.ContainsKey("A" + idx)) idx++;
            string newId = "A" + idx;

            cfg.Accounts[newId] = remark;
            ConfigService.Save(cfg);
            Directory.CreateDirectory(Path.Combine(ConfigService.AccountBackupDir, newId));
            LoadAccounts();
        }

        // ──────────────────────────────────────────────
        //  重命名（按钮 & 双击）
        // ──────────────────────────────────────────────
        private async void BtnRename_Click(object sender, RoutedEventArgs e) => await RenameSelected();
        private async void AccountList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => await RenameSelected();

        private async System.Threading.Tasks.Task RenameSelected()
        {
            var sel = GetSelected();
            if (sel == null) return;

            string cleanRemark = sel.Remark.Replace(" ⭐", "");
            var input = new TextBox { Text = cleanRemark, MaxLength = 32 };
            var dlg = new ContentDialog
            {
                Title = $"重命名 {cleanRemark}",
                Content = input,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            string newRemark = input.Text.Trim();
            if (string.IsNullOrEmpty(newRemark)) return;

            var cfg = ConfigService.Load();
            cfg.Accounts[sel.Id] = newRemark;
            ConfigService.Save(cfg);
            LoadAccounts();
        }

        // ──────────────────────────────────────────────
        //  设为默认
        // ──────────────────────────────────────────────
        private void BtnSetDefault_Click(object sender, RoutedEventArgs e)
        {
            var sel = GetSelected();
            if (sel == null) return;

            var cfg = ConfigService.Load();
            cfg.DefaultAccount = sel.Id;
            ConfigService.Save(cfg);
            LoadAccounts();
        }

        // ──────────────────────────────────────────────
        //  备份
        // ──────────────────────────────────────────────
        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var sel = GetSelected();
            if (sel == null) return;

            try
            {
                GameService.BackupAccount(sel.Id);
                await ShowInfo("备份成功", $"账号 {sel.Remark.Replace(" ⭐", "")} 的数据已备份。");
            }
            catch (Exception ex)
            {
                await ShowInfo("备份失败", ex.Message);
            }
        }

        // ──────────────────────────────────────────────
        //  删除
        // ──────────────────────────────────────────────
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var sel = GetSelected();
            if (sel == null) return;

            var cfg = ConfigService.Load();
            if (cfg.Accounts.Count <= 1)
            {
                await ShowInfo("提示", "至少需要保留一个账号。");
                return;
            }

            var confirm = new ContentDialog
            {
                Title = "确认删除",
                Content = $"确定要删除账号「{sel.Remark.Replace(" ⭐", "")}」吗？此操作不可撤销。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            cfg.Accounts.Remove(sel.Id);
            if (cfg.DefaultAccount == sel.Id) cfg.DefaultAccount = "";
            ConfigService.Save(cfg);

            string backupDir = Path.Combine(ConfigService.AccountBackupDir, sel.Id);
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);

            LoadAccounts();
        }

        // ──────────────────────────────────────────────
        //  辅助
        // ──────────────────────────────────────────────
        private async System.Threading.Tasks.Task ShowInfo(string title, string msg)
        {
            var dlg = new ContentDialog { Title = title, Content = msg, CloseButtonText = "确定", XamlRoot = this.XamlRoot };
            await dlg.ShowAsync();
        }
    }
}
