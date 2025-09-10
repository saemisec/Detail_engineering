using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Detail_engineering
{
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
            var node = e.NewValue as TreeNode;
            if (node?.Doc == null)
                return;

            // کاربر روی یکی از Related Doc ها کلیک کرده
            var doc = node.Doc;
            var lastRevPath = PathHelper.BuildRelatedPath(doc);

            if (!Directory.Exists(lastRevPath))
            {
                // می‌تونی پیام ملایم نشون بدی یا هیچ کاری نکنی
                // MessageBox.Show($"Folder not found:\n{lastRevPath}", "Info");
                return;
            }

            // محتویات پوشه‌ی LastRevision را لیست کن
            foreach (var f in Directory.EnumerateFiles(lastRevPath))
            {
                Files.Add(new FileItem
                {
                    FileName = Path.GetFileName(f),
                    FileType = Path.GetExtension(f)?.TrimStart('.').ToUpperInvariant() ?? ""
                });
            }
        }
    }

    
}
