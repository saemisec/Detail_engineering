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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Detail_engineering
{
    public partial class DocumentView : UserControl
    {
        private ObservableCollection<DocumentRecord> _all = new();
        private ICollectionView _view;
        private int _totalCount = 0;

        public ObservableCollection<string> AllDisciplines { get; } = new();
        public ObservableCollection<string> AllDocTypes { get; } = new();

        private readonly HashSet<string> _selDisc = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selType = new(StringComparer.OrdinalIgnoreCase);

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
                //var path = AppDomain.CurrentDomain.JsonPath;
                var path = Settings.Default.JsonPath;  
                //Path.Combine(baseDir, "database.json");
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
                AllDisciplines.Clear();
                foreach (var v in _all.Select(d => d.Dicipline).Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                    AllDisciplines.Add(v);

                AllDocTypes.Clear();
                foreach (var v in _all.Select(d => d.Document_type).Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
                    AllDocTypes.Add(v);

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

            // فیلتر متن (نام/شماره)
            var q = (SearchBox.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(q))
            {
                var s = q.ToLowerInvariant();
                if (!((d.Document_name ?? "").ToLowerInvariant().Contains(s) ||
                      (d.Document_number ?? "").ToLowerInvariant().Contains(s)))
                    return false;
            }

            // فیلتر Dicipline
            if (_selDisc.Count > 0 && (d.Dicipline == null || !_selDisc.Contains(d.Dicipline)))
                return false;

            // فیلتر Document_type
            if (_selType.Count > 0 && (d.Document_type == null || !_selType.Contains(d.Document_type)))
                return false;

            return true;
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

        private void ShowFilterMenu(Button btn, bool isDiscipline)
        {
            var cm = new ContextMenu
            {
                PlacementTarget = btn,
                Placement = PlacementMode.Bottom
                //Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#171B34"),
                //BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#26305E")
            };
            cm.Resources[SystemColors.MenuTextBrushKey] = Brushes.White;

            var title = new MenuItem { Header = isDiscipline ? "Filter by Dicipline" : "Filter by Document_type", IsEnabled = false };
            cm.Items.Add(title);
            cm.Items.Add(new Separator());

            var items = isDiscipline ? AllDisciplines : AllDocTypes;
            foreach (var val in items)
            {
                var mi = new MenuItem { Header = val, IsCheckable = true, StaysOpenOnClick = true };
                mi.IsChecked = isDiscipline ? _selDisc.Contains(val) : _selType.Contains(val);
                mi.Click += (s, args) =>
                {
                    if (isDiscipline)
                    {
                        if (mi.IsChecked) _selDisc.Add(val); else _selDisc.Remove(val);
                    }
                    else
                    {
                        if (mi.IsChecked) _selType.Add(val); else _selType.Remove(val);
                    }
                    _view?.Refresh();
                    UpdateResults();
                };
                cm.Items.Add(mi);
            }

            /*
            cm.Items.Add(new Separator());

            var clear = new MenuItem { Header = "Clear" };
            clear.Click += (s, args) =>
            {
                if (isDiscipline) _selDisc.Clear(); else _selType.Clear();
                foreach (var it in cm.Items.OfType<MenuItem>()) if (it.IsCheckable) it.IsChecked = false;
                _view?.Refresh();
                UpdateResults();
            };
            cm.Items.Add(clear);

            var close = new MenuItem { Header = "Close" };
            close.Click += (s, args) => cm.IsOpen = false;
            cm.Items.Add(close);
            */
            btn.ContextMenu = cm;
            cm.IsOpen = true;
            
        }

        private void DiscBtn_Click(object sender, RoutedEventArgs e) => ShowFilterMenu((Button)sender, isDiscipline: true);
        private void TypeBtn_Click(object sender, RoutedEventArgs e) => ShowFilterMenu((Button)sender, isDiscipline: false);


        // تیک/برداشت تیک — بلافاصله فیلتر بزن
        private void DiscCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Content is string val)
            {
                if (cb.IsChecked == true) _selDisc.Add(val);
                else _selDisc.Remove(val);
                _view?.Refresh();
                UpdateResults();
            }
        }
        private void TypeCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Content is string val)
            {
                if (cb.IsChecked == true) _selType.Add(val);
                else _selType.Remove(val);
                _view?.Refresh();
                UpdateResults();
            }
        }

        // Clear دکمه
        private void DiscClear_Click(object sender, RoutedEventArgs e)
        {
            _selDisc.Clear();
            UncheckAllInPopup(sender);
            _view?.Refresh();
            UpdateResults();
        }
        private void TypeClear_Click(object sender, RoutedEventArgs e)
        {
            _selType.Clear();
            UncheckAllInPopup(sender);
            _view?.Refresh();
            UpdateResults();
        }

        // بستن Popup
        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            var p = FindAncestor<System.Windows.Controls.Primitives.Popup>(sender as DependencyObject);
            if (p != null) p.IsOpen = false;
        }

        // کمک‌متدها: پیدا کردن Popup و Uncheck همه‌ی چک‌باکس‌ها داخلش
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void UncheckAllInPopup(object sender)
        {
            var p = FindAncestor<System.Windows.Controls.Primitives.Popup>((sender as DependencyObject));
            if (p == null) return;

            void Walk(DependencyObject d)
            {
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(d, i);
                    if (child is CheckBox cb) cb.IsChecked = false;
                    Walk(child);
                }
            }
            if (p.Child != null) Walk(p.Child);
        }

        // Hyperlinks — فعلاً مقصد نداریم؛ فقط اطلاع می‌دهیم. بعداً وصل می‌کنیم.
        private void DocNameLink_Click(object sender, RoutedEventArgs e)
        {
            if (GetRowContext(sender) is DocumentRecord d)
            {
                var owner = Window.GetWindow(this);
                var win = new DocumentDetailsWindow(d)  // ⬅️ پنجره جدید با دیتا
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                win.ShowDialog();
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
