using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace Detail_engineering
{
    public partial class ThreeDView : UserControl
    {
        public ThreeDView()
        {
            InitializeComponent();
            Loaded += ThreeDView_Loaded;
        }

        private async void ThreeDView_Loaded(object sender, RoutedEventArgs e)
        {
            await Web.EnsureCoreWebView2Async();

            // فولدر مدل‌ها کنار exe
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var modelsDir = Path.Combine(baseDir, "Models");
            if (!Directory.Exists(modelsDir))
            {
                Web.CoreWebView2.NavigateToString("<html><body style='background:#101218;color:#fff;font:14px sans-serif'>Models folder not found.</body></html>");
                return;
            }

            // دامنهٔ مجازی برای دسترسی به فایل‌های محلی (از file:// راحت‌تر و پایدارتر)
            Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app", modelsDir, CoreWebView2HostResourceAccessKind.Allow);

            // اولین GLB را انتخاب کن
            var glbName = Directory.EnumerateFiles(modelsDir, "*.glb").Select(Path.GetFileName).FirstOrDefault();
            if (glbName == null)
            {
                Web.CoreWebView2.NavigateToString("<html><body style='background:#101218;color:#fff;font:14px sans-serif'>No .glb found in Models folder.</body></html>");
                return;
            }

            Web.CoreWebView2.NavigateToString(BuildHtml(glbName));
        }

        private string BuildHtml(string glbName)
        {
            // مدل را از https://app/<filename> می‌خوانیم (به لطف Map بالا)
            var safe = Uri.EscapeUriString(glbName);

            var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1'/>
  <script type='module' src='https://unpkg.com/@google/model-viewer/dist/model-viewer.min.js'></script>
  <style>
    html,body{{height:100%;margin:0;background:#101218}}
    model-viewer{{width:100%;height:100%;}}
    #msg{{position:fixed;left:8px;bottom:8px;color:#fff;font:12px/1.4 sans-serif;opacity:.8}}
  </style>
</head>
<body>
  <model-viewer id='mv'
    src='https://app/{safe}'
    camera-controls
    environment-image='neutral'
    exposure='1' shadow-intensity='0.4'
    bounds='tight' camera-orbit='0deg 65deg 140%' field-of-view='45deg'>
    <div slot='poster' style='color:#fff;display:flex;align-items:center;justify-content:center;height:100%'>Loading…</div>
  </model-viewer>
  <div id='msg'>loading…</div>
  <script>
    const mv = document.getElementById('mv'), msg = document.getElementById('msg');
    const log = t => msg.textContent = t;
    mv.addEventListener('load',  ()=>log('loaded ✓'));
    mv.addEventListener('error', e=>{{ console.error('error', e.detail); log('error: ' + (e.detail?.message||'unknown')); }});
  </script>
</body>
</html>";
            return html;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            // بارگذاری مجدد (برای وقتی مدل را عوض کردی)
            ThreeDView_Loaded(this, new RoutedEventArgs());
        }

        private void DevTools_Click(object sender, RoutedEventArgs e)
        {
            Web.CoreWebView2?.OpenDevToolsWindow();
        }
    }
}
