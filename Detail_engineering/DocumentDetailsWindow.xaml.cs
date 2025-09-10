using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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




    public class TreeNode
    {
        public string Title { get; set; }
        public bool IsRevision { get; set; } // اینجا استفاده‌ خاصی نداریم، نگه می‌داریم
        public DocumentRecord Doc { get; set; } // برای نودهای Related Doc پر می‌شود
        public ObservableCollection<TreeNode> Children { get; } = new();
    }

    public class FileItem
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
    }

    public partial class DocumentDetailsWindow : Window
    {
        public string HeaderTitle { get; set; } = "Document Details";
        public ObservableCollection<TreeNode> TreeRoots { get; } = new();
        public ObservableCollection<FileItem> Files { get; } = new();


        private readonly DocumentRecord _doc;

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
                    FileType = (Path.GetExtension(f) ?? "").TrimStart('.').ToUpperInvariant()
                });
            }
        }
    }
            

    
}
