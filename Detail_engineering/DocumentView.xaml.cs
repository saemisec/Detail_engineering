using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Detail_engineering
{
    public partial class DocumentView : UserControl
    {
        private ObservableCollection<DocumentRecord> _all = new();
        private ICollectionView _view;
        private int _totalCount = 0;

        public DocumentView()
        {
            InitializeComponent();
            Loaded += DocumentView_Loaded;
        }

        private void DocumentView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            _view = CollectionViewSource.GetDefaultView(_all);
            _view.Filter = FilterPredicate;
            GridDocs.ItemsSource = _view;

            UpdateResults();
        }

        private void LoadData()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "database.json");
                if (!File.Exists(path))
                {
                    MessageBox.Show($"database.json not found at:\n{path}", "Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _all = new ObservableCollection<DocumentRecord>();
                    _totalCount = 0;
                    return;
                }

                var json = File.ReadAllText(path);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                var items = JsonSerializer.Deserialize<List<DocumentRecordRaw>>(json, opts) ?? new List<DocumentRecordRaw>();

                var mapped = items.Select(DocumentRecord.FromRaw).ToList();
                _all = new ObservableCollection<DocumentRecord>(mapped);
                _totalCount = _all.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "LoadData error", MessageBoxButton.OK, MessageBoxImage.Error);
                _all = new ObservableCollection<DocumentRecord>();
                _totalCount = 0;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not DocumentRecord d) return false;
            var q = (SearchBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(q)) return true;

            var s = q.ToLowerInvariant();
            return (d.Document_name ?? "").ToLowerInvariant().Contains(s)
                || (d.Document_number ?? "").ToLowerInvariant().Contains(s);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            UpdateResults();
        }

        private void UpdateResults()
        {
            if (_view == null)
            {
                ResultsText.Text = "Results: 0 of 0";
                return;
            }
            int filtered = _view.Cast<object>().Count();
            ResultsText.Text = $"Results: {filtered} of {_totalCount}";
        }

        // Hyperlinks — فعلاً مقصد نداریم؛ فقط اطلاع می‌دهیم. بعداً وصل می‌کنیم.
        private void DocNameLink_Click(object sender, RoutedEventArgs e)
        {
            if (GetRowContext(sender) is DocumentRecord d)
            {
                MessageBox.Show($"[Document_name clicked]\n{d.Document_name}\n(مقصد لینک را بعداً تنظیم می‌کنیم)", "Link");
            }
        }

        private void LastRevisionLink_Click(object sender, RoutedEventArgs e)
        {
            if (GetRowContext(sender) is DocumentRecord d && d.HasLastRevision)
            {
                MessageBox.Show($"[lastrevision clicked]\n{d.LastRevisionName}\n(مقصد لینک را بعداً تنظیم می‌کنیم)", "Link");
            }
        }

        private static DocumentRecord GetRowContext(object sender)
        {
            // Hyperlink → TextBlock → DataContext
            if (sender is System.Windows.Documents.Hyperlink link &&
                link.Parent is TextBlock tb &&
                tb.DataContext is DocumentRecord d)
            {
                return d;
            }
            return null;
        }
    }

    // مدل خام مطابق JSON
    public class DocumentRecordRaw
    {
        public string Document_name { get; set; }
        public string Document_number { get; set; }
        public string Dicipline { get; set; }
        public string Document_type { get; set; }

        // فرض: لیست رشته‌ها (عنوان هر ریویژن). اگر ساختار دیگری داری، بگو تا تنظیم کنم.
        public List<string> Revisions { get; set; }
    }

    // مدل نمایشی با ستون‌های محاسباتی
    public class DocumentRecord : INotifyPropertyChanged
    {
        public string Document_name { get; set; }
        public string Document_number { get; set; }
        public string Dicipline { get; set; }
        public string Document_type { get; set; }
        public List<string> Revisions { get; set; } = new();

        // محاسبات
        public int RevisionsCountExclRejected => (Revisions ?? new()).Count(x => !ContainsRejected(x));
        public int RejectedCount => (Revisions ?? new()).Count(x => ContainsRejected(x));
        public string LastRevisionName => (Revisions != null && Revisions.Count > 0) ? Revisions[^1] : "";
        public bool HasLastRevision => !string.IsNullOrWhiteSpace(LastRevisionName);

        private static bool ContainsRejected(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOf("rejected", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static DocumentRecord FromRaw(DocumentRecordRaw r) => new DocumentRecord
        {
            Document_name = r.Document_name,
            Document_number = r.Document_number,
            Dicipline = r.Dicipline,
            Document_type = r.Document_type,
            Revisions = r.Revisions ?? new List<string>()
        };

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // کانورتر ساده برای Row.No (AlternationIndex + 1)
    public sealed class AlternationConverter : System.Windows.Data.IValueConverter
    {
        public static AlternationConverter Instance { get; } = new AlternationConverter();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int i) return (i + 1).ToString();
            return "1";
            }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }
}
