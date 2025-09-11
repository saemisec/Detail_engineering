using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace Detail_engineering
{

    public class FileTypeLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (value as string ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "DOC" or "DOCX" => "Document",
                "XLS" or "XLSX" or "CSV" => "Worksheet",
                "PDF" => "PDF",
                "DWG" => "Drawing",
                "DGN" => "Drawing",
                "ZIP" or "RAR" or "7Z" => "ZIP",
                "PNG" or "JPG" or "JPEG" or "WEBP" => "Image",
                //default => string.IsNullOrEmpty(t) ? "File" : t
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class FileTypeBgConverter : IValueConverter
    {
        SolidColorBrush Brush(string hex) => (SolidColorBrush)(new BrushConverter().ConvertFromString(hex));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (value as string ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "DOC" or "DOCX" => Brush("#3156D8"),
                "XLS" or "XLSX" or "CSV" => Brush("#2E8B57"),
                "PDF" => Brush("#C0392B"),
                "DWG" or "DGN" => Brush("#8E44AD"),
                "ZIP" or "RAR" or "7Z" => Brush("#F39C12"),
                "PNG" or "JPG" or "JPEG" or "WEBP" => Brush("#3B4A86"),
                _ => Brush("#4B557A")
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class FileTypeAccentConverter : IValueConverter
    {
        SolidColorBrush Brush(string hex) => (SolidColorBrush)(new BrushConverter().ConvertFromString(hex));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (value as string ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "DOC" or "DOCX" => Brush("#9BB5FF"),
                "XLS" or "XLSX" or "CSV" => Brush("#A8E6C3"),
                "PDF" => Brush("#F5B7B1"),
                "DWG" or "DGN" => Brush("#D2B4DE"),
                "ZIP" or "RAR" or "7Z" => Brush("#FDE3A7"),
                "PNG" or "JPG" or "JPEG" or "WEBP" => Brush("#9AB0FF"),
                _ => Brush("#C9D0EA")
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }


    public class TreeNodeIconConverter : IValueConverter
    {
        // ورودی TreeNode است: برای ریشه 📦 و برای فرزند بر اساس Doc.Document_type
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var node = value as TreeNode;
            if (node == null || node.Doc == null)
                return "📦"; // ریشه (Part)

            var t = (node.Doc.Document_name ?? "").Trim().ToUpperInvariant();
            if (t.Contains("P & ID") || t.Contains("P&ID") || t.Contains("P-ID")) return "🛠";
            else if (t.Contains("DATASHEET")) return "📄";
            else if (t.Contains("CALCULATION")) return "🧮";
            else if (t.Contains("SPEC")) return "📘";
            else if (t.Contains("LAYOUT")) return "🗺";
            else if (t.Contains("DRAWING") || t.Contains("DWG")) return "📐";
            return "📎";
            
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class DocTypeBadgeBgConverter : IValueConverter
    {
        SolidColorBrush B(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = (value as string ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "P & ID" or "P&ID" or "P-ID" => B("#244E7A"),
                "DATASHEET" => B("#3156D8"),
                "CALCULATION" => B("#2E8B57"),
                "SPEC" or "SPECIFICATION" => B("#5D3FD3"),
                "LAYOUT" => B("#8E44AD"),
                "DRAWING" or "DWG" => B("#3B4A86"),
                _ => B("#4B557A")
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }







    public class TreeNode
    {
        public string Title { get; set; }
        public bool IsRevision { get; set; } // اینجا استفاده‌ خاصی نداریم، نگه می‌داریم
        public DocumentRecord Doc { get; set; } // برای نودهای Related Doc پر می‌شود
        public ObservableCollection<TreeNode> Children { get; } = new();
    }

    public class ItemRow
    {
        public string Name { get; init; } = string.Empty;
        public string FullPath { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
    }

    public class FileItem
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FullPath { get; set; }
    }

    public partial class DocumentDetailsWindow : Window
    {
        public string HeaderTitle { get; set; } = "Document Details";
        public ObservableCollection<TreeNode> TreeRoots { get; } = new();
        public ObservableCollection<FileItem> Files { get; } = new();


        private readonly DocumentRecord _doc;

        private bool _pdfInit = false;


        public DocumentDetailsWindow(DocumentRecord doc)
        {
            InitializeComponent();
            _doc = doc;

            HeaderTitle = $"{doc.Document_name}  ({doc.Document_number})";
            DataContext = this;

            // Build tree: root (document) -> children (revisions)
            var root = new TreeNode
            {
                Title = doc.Document_name,
                IsRevision = false
            };
            foreach (var r in (doc.Revisions ?? new()).DefaultIfEmpty())
            {
                if (string.IsNullOrWhiteSpace(r)) continue;
                root.Children.Add(new TreeNode
                {
                    Title = r,
                    IsRevision = true
                });
            }
            TreeRoots.Add(root);

            // bind files grid (empty for now)
            GridFiles.ItemsSource = Files;
        }

        public DocumentDetailsWindow(string partTitle, System.Collections.Generic.IEnumerable<DocumentRecord> relatedDocs)
        {
            InitializeComponent();
            HeaderTitle = partTitle;
            DataContext = this;
            this.Loaded += (s, e) => EnsurePdfWebViewReady();


            // root: عنوان Part
            var root = new TreeNode { Title = partTitle, IsRevision = false };
            foreach (var d in relatedDocs ?? Enumerable.Empty<DocumentRecord>())
            {
                //MessageBox.Show($"database.json not found at:\n{d}", "Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                var title = BuildDocNodeTitle(d); // مثلا "PL-001-A - Pump Layout - Area A"
                root.Children.Add(new TreeNode { Title = title, IsRevision = false, Doc = d });
            }
            TreeRoots.Add(root);

            GridFiles.ItemsSource = Files;
        }

        private static string BuildDocNodeTitle(DocumentRecord d)
        {
            var num = d?.Document_number;
            var name = d?.Document_name;
            if (!string.IsNullOrWhiteSpace(num) && !string.IsNullOrWhiteSpace(name))
                return $"{num} - {name}";
            return num ?? name ?? "(document)";
        }


        private FrameworkElement GetItemsHost(TreeViewItem tvi)
        {
            if (tvi == null) return null;

            // مطمئن شو تمپلیت اعمال شده و فرزندان ساخته شدن
            tvi.ApplyTemplate();
            tvi.UpdateLayout();

            // اول دنبال ItemsPresenter
            var ip = FindVisualChild<ItemsPresenter>(tvi);
            if (ip != null) return ip;

            // اگه نبود، VirtualizingStackPanel
            var vsp = FindVisualChild<VirtualizingStackPanel>(tvi);
            if (vsp != null) return vsp;

            // fallback: StackPanel ساده
            return FindVisualChild<StackPanel>(tvi);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }



        private void TreeItem_Expanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem ?? sender as TreeViewItem;
            if (tvi == null) return;

            // مطمئن شو تمپلیت اعمال شده و بچه‌ها ساخته شده‌اند
            tvi.ApplyTemplate();
            tvi.UpdateLayout();

            FrameworkElement itemsHost = GetItemsHost(tvi);
            if (itemsHost == null) return;

            itemsHost.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
            {
                EasingFunction = new QuadraticEase()
            };
            itemsHost.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            e.Handled = false;
        }

        private void TreeItem_Collapsed(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem ?? sender as TreeViewItem;
            if (tvi == null) return;

            tvi.ApplyTemplate();
            tvi.UpdateLayout();

            FrameworkElement itemsHost = GetItemsHost(tvi);
            if (itemsHost == null) return;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new QuadraticEase()
            };
            itemsHost.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            e.Handled = false;
        }





        private static ItemsPresenter FindItemsPresenter(DependencyObject d)
        {
            // جستجو در ویژوال‌تری برای ItemsPresenter زیر TreeViewItem
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                if (child is ItemsPresenter ip) return ip;
                var res = FindItemsPresenter(child);
                if (res != null) return res;
            }
            return null;
        }

        private async void EnsurePdfWebViewReady()
        {
            if (_pdfInit) return;
            try
            {
                await PdfViewer.EnsureCoreWebView2Async();
                _pdfInit = true;
                // تم تاریک صفحهٔ پس‌زمینه
                PdfViewer.DefaultBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#0F1631");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PDF] Init error: " + ex);
            }
        }

        private async void GridFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GridFiles.SelectedItem as FileItem;
            if (item == null)
            {
                PdfHint.Visibility = Visibility.Visible;
                return;
            }

            // فقط PDF را پیش‌نمایش بده
            if (!string.Equals(item.FileType, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                PdfHint.Text = "Only PDF preview is supported here.";
                PdfHint.Visibility = Visibility.Visible;
                PdfViewer.Visibility = Visibility.Collapsed;
                return;
            }

            // WebView2 آماده؟
            await Dispatcher.InvokeAsync(EnsurePdfWebViewReady);

            if (!_pdfInit || PdfViewer.CoreWebView2 == null)
            {
                PdfHint.Text = "PDF viewer is not available.";
                PdfHint.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // مسیر فایل → file:/// URI (UNC هم پشتیبانی می‌شود)
                var uri = new Uri(item.FullPath);
                var nav = uri.AbsoluteUri; // مثال: file:///C:/... یا file://server/share/...

                PdfViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                PdfViewer.Source = new Uri(nav);
                PdfHint.Visibility = Visibility.Collapsed;
                PdfViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PDF] Navigate error: " + ex);
                PdfHint.Text = "Failed to open PDF.";
                PdfHint.Visibility = Visibility.Visible;
            }
        }



        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // When a tree node is selected

        private void Tree_SelectedItemChanged_old(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }


        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Files.Clear();
            if (e.NewValue is not TreeNode node || node.Doc == null) return;

            // ⬅️ مسیر نهایی پوشه‌ی آخرین ریویژن
            var lastRevPath = PathHelper.BuildRelatedPath(node.Doc);


            if (!Directory.Exists(lastRevPath))
                return;

            foreach (var f in Directory.EnumerateFiles(lastRevPath))
            {
                Files.Add(new FileItem
                {
                    FileName = Path.GetFileName(f),
                    FileType = (Path.GetExtension(f) ?? "").TrimStart('.').ToUpperInvariant(),
                    FullPath = f
                });
            }
        }
    }



}
