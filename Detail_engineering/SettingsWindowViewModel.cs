using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
//using System.Windows.Input;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Detail_engineering
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }

    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private string _baseFolder = Settings.Default.BaseFolder ?? "";

        public string BaseFolder
        {
            get => _baseFolder;
            set { _baseFolder = value; OnPropertyChanged(); }
        }

        public ICommand BrowseBaseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand CreateDatabaseCommand { get; }

        public event EventHandler CloseRequested;

        public SettingsWindowViewModel()
        {
            BrowseBaseCommand = new RelayCommand(_ => BrowseBase());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
            CreateDatabaseCommand = new RelayCommand(_ => CreateDatabase());
        }

        private void BrowseBase()
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) BaseFolder = dlg.SelectedPath;
        }

        private void Save()
        {
            Settings.Default.BaseFolder = (BaseFolder ?? "").Trim();
            Settings.Default.Save();

            // اعمال فوری روی برنامه (مثلاً PathHelper)
            PathHelper.BaseDir = Settings.Default.BaseFolder ?? "";

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }


        private void CreateDatabase()
        {
            // 1) مسیر ریشه‌ی داکیومنت‌ها: پیش‌فرض BaseFolder؛ اگر خالی بود از کاربر بگیر
            string root = (BaseFolder ?? "").Trim();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK) root = dlg.SelectedPath;
            }
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                System.Windows.MessageBox.Show("Base folder معتبر نیست.", "Create Database", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 2) خروجی: کنار exe با نام database.json
            string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");

            var records = new List<DbRecord>();

            try
            {
                // ساختار: base\Discipline\Document_type\(Document_number + space + Document_name)\Revisions...
                foreach (var discDir in SafeEnumerateDirectories(root))
                {
                    string discipline = Path.GetFileName(discDir);

                    foreach (var dtypeDir in SafeEnumerateDirectories(discDir))
                    {
                        string docType = Path.GetFileName(dtypeDir);

                        foreach (var comboDir in SafeEnumerateDirectories(dtypeDir))
                        {
                            string comboName = Path.GetFileName(comboDir);
                            //if (!comboName.ToLower().Contains("1389-de"))
                            if (comboName.ToLower().Contains("1389-ar"))
                            {
                                var (docNumber, docName) = SplitNumberAndName(comboName);
                                var revisions = SafeEnumerateDirectories(comboDir)
                                                .Select(Path.GetFileName)
                                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                                .ToList();
                                records.Add(new DbRecord
                                {
                                    Document_name = docName,
                                    Document_number = docNumber,
                                    Dicipline = discipline,
                                    Document_type = docType,
                                    Revisions = revisions
                                });
                            }
                        }
                    }
                }

                // 3) نوشتن JSON خوش‌فرم
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outPath, JsonSerializer.Serialize(records, opts));
                System.Windows.MessageBox.Show($"Database created successfully", "Create Database",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطا در ساخت دیتابیس:\n" + ex.Message, "Create Database",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // مدل خروجی JSON
        private class DbRecord
        {
            public string Document_name { get; set; }
            public string Document_number { get; set; }
            public string Dicipline { get; set; }
            public string Document_type { get; set; }
            public List<string> Revisions { get; set; }
        }

        // کمک‌تابع: ایمن در برابر خطا
        private static IEnumerable<string> SafeEnumerateDirectories(string path)
        {
            try { return Directory.EnumerateDirectories(path); }
            catch { return Enumerable.Empty<string>(); }
        }

        // جدا کردن Document_number و Document_name بر اساس الگوی "-000-"
        private static (string number, string name) SplitNumberAndName(string combo)
        {
            // نمونه: "PL-000- Pump Layout - Area A"
            //      → number = "PL-000-"
            //      → name   = "Pump Layout - Area A"
            var m = Regex.Match(combo ?? "", @"^(.*?-\d{3}-)(.*)$", RegexOptions.Singleline);
            if (m.Success)
            {
                var num = m.Groups[1].Value.Trim();
                var name = m.Groups[2].Value.Trim().Trim('-', '_').Trim();
                if (num.EndsWith("-") ) { num = num.Substring(0, num.Length - 1);}
                return (num, name);
            }

            // فالبک: اگر الگو پیدا نشد، قبل از اولین فاصله تقسیم کن
            var idx = (combo ?? "").IndexOf(' ');
            if (idx > 0)
            {
                var n1 = combo.Substring(0, idx).Trim();
                var n2 = combo.Substring(idx + 1).Trim();
                return (n1, n2);
            }

            // فالبک نهایی: کل نام را Document_name قرار بده
            return ("", (combo ?? "").Trim());
        }




        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
