using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Detail_engineering
{
    public partial class DocumentDetailsWindow : Window
    {
        public string HeaderTitle { get; set; } = "Document Details";

        // Tree data
        public ObservableCollection<TreeNode> TreeRoots { get; } = new();

        // Right-grid data
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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // When a tree node is selected
        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var node = e.NewValue as TreeNode;
            Files.Clear();

            // Only populate when a "revision" node is selected; for root keep it empty
            if (node != null && node.IsRevision)
            {
                // TODO: اینجا بعداً دیتاهای واقعی این Revision رو می‌ریزیم
                // فعلاً خالی می‌مونه، یا اگر خواستی نمونهٔ تستی:
                // Files.Add(new FileItem { FileName = "sample.dwg", FileType = "DWG" });
            }
        }
    }

    public class TreeNode
    {
        public string Title { get; set; }
        public bool IsRevision { get; set; }
        public ObservableCollection<TreeNode> Children { get; } = new();
    }

    public class FileItem
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
    }
}
