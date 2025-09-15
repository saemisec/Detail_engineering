using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace Detail_engineering
{
    public class WebMsg
    {
        public string Type { get; set; }
        public WebPayload Payload { get; set; }
    }
    public class WebPayload
    {
        public string PartTitle { get; set; }
        public System.Collections.Generic.List<PayloadDoc> Documents { get; set; }
    }
    public class PayloadDoc
    {
        public string Document_name { get; set; }
        public string Document_number { get; set; }
        public string Dicipline { get; set; }
        public string Document_type { get; set; }
        public System.Collections.Generic.List<string> Revisions { get; set; }
    }


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
      Web.CoreWebView2.Settings.IsWebMessageEnabled = true;
      Web.CoreWebView2.WebMessageReceived += async (s, e) =>
      {
        try
        {
          // ✅ همیشه امن
          string rawJson = e.WebMessageAsJson;
          System.Diagnostics.Debug.WriteLine("[WPF] RAW JSON: " + rawJson);

          using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
          var root = doc.RootElement;

          // اگر پیام واقعاً رشته بود، همین‌جا خروج کن
          if (root.ValueKind == System.Text.Json.JsonValueKind.String)
          {
            var str = root.GetString();
            System.Diagnostics.Debug.WriteLine("[WPF] Got string message: " + str);
            return;
          }

          // انتظار آبجکت { type, payload }
          var type = root.GetProperty("type").GetString();
          if (!string.Equals(type, "openDocDetailsBatch", StringComparison.OrdinalIgnoreCase))
            return;

          var payload = root.GetProperty("payload");
          var partTitle = payload.GetProperty("PartTitle").GetString() ?? "(Part)";

          var docsEl = payload.GetProperty("Documents");
          var docs = new System.Collections.Generic.List<DocumentRecord>();
          foreach (var d in docsEl.EnumerateArray())
          {
            var rec = new DocumentRecord
            {
              Document_name = d.GetProperty("Document_name").GetString(),
              Document_number = d.GetProperty("Document_number").GetString(),
              Dicipline = d.GetProperty("Dicipline").GetString(),
              Document_type = d.GetProperty("Document_type").GetString(),
              Revisions = d.TryGetProperty("Revisions", out var revs)
                                  ? revs.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                                  : new System.Collections.Generic.List<string>()
            };
            docs.Add(rec);
          }

          await Dispatcher.InvokeAsync(() =>
          {
            var owner = Window.GetWindow(this);
            var win = new Detail_engineering.DocumentDetailsWindow(partTitle, docs)
            {
              Owner = owner,
              WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            win.ShowDialog();
          });
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine("[WPF] WebMessage error: " + ex);
        }
      };
      var baseDir = AppDomain.CurrentDomain.BaseDirectory;
      var modelsDir = Path.Combine(baseDir, "Models");
      if (!Directory.Exists(modelsDir))
      {
        Web.CoreWebView2.NavigateToString("<html><body style='background:#101218;color:#fff;font:14px sans-serif'>Models folder not found.</body></html>");
        return;
      }



      // Map برای WebAssets (three.js و دیکودرها)
      var webAssets = Path.Combine(baseDir, "WebAssets");
      Directory.CreateDirectory(webAssets);
      Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
          "assets", webAssets, CoreWebView2HostResourceAccessKind.Allow);

      // Map برای Models
      Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
          "app", modelsDir, CoreWebView2HostResourceAccessKind.Allow);

      // map به پوشه‌ی کنار exe که database.json اونجاست
      Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
          "data", AppDomain.CurrentDomain.BaseDirectory, CoreWebView2HostResourceAccessKind.Allow);

      // اولین GLB
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
      var safe = Uri.EscapeUriString(glbName);

      var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width,initial-scale=1'/>
  <!-- import map: ثابت می‌کنه 'three' به فایل لوکال resolve بشه -->
  <script type='importmap'>
  {{
    ""imports"": {{
      ""three"": ""https://assets/three/three.module.js""
    }}
  }}
  </script>
  <style>
    :root{{--bg:#101218;--panel:#171B34;--txt:#EAF0FF;--btn:#1F2447;--btnh:#2A3162;--accent:#5DB5FF;--on:#274a7a;--line:#2A3162;--border:#1FFFFFFF}}
    html,body{{margin:0;height:100%;background:var(--bg);color:var(--txt);font:13px system-ui,Segoe UI,Roboto,sans-serif;overflow:hidden}}
    #c{{width:100%;height:100%;display:block}}
    #toolboxes{{position:fixed;top:48px;left:8px;display:grid;grid-template-columns:repeat(5,auto);gap:6px;
      background:color-mix(in srgb,var(--panel) 92%,transparent);padding:10px;border-radius:12px;border:1px solid var(--border);backdrop-filter:blur(6px);
      box-shadow:0 10px 30px #0006;z-index:10}}
    .tb{{background:var(--btn);color:var(--txt);border:1px solid #22FFFFFF;border-radius:10px;padding:7px 10px;cursor:pointer;
      transition:transform .06s ease,background .12s ease,opacity .12s ease,outline-color .12s ease;user-select:none}}
    .tb:hover{{background:var(--btnh)}} .tb:active{{transform:scale(.97) translateY(1px)}}
    .tb.toggle.on{{background:var(--on);outline:2px solid var(--accent)}} .tb:disabled{{opacity:.45;cursor:not-allowed;filter:saturate(.6)}}
    #msg{{position:fixed;right:8px;bottom:8px;color:#fff;opacity:.75;font-size:12px}}

    #toggleToolboxBtn {{
      position: absolute; top: 4px; right: 10px;
      width: 32px; height: 32px; border-radius: 50%;
      background: rgba(0,0,0,0.35);
      border: 1px solid rgba(255,255,255,0.4);
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; transition: background .2s, border .2s; z-index: 1001;
    }}
    #toggleToolboxBtn:hover {{ background: rgba(40,59,122,.6); border-color: #fff; }}
    #eyeIcon {{ font-family: 'Segoe MDL2 Assets'; font-size: 18px; color: #fff; }}

    #tools {{
    position: absolute; top: 44px; right: 10px;
    display: flex; gap: 8px; padding: 8px;flex-direction: column;
    background: rgba(31, 36, 71, 0.95);
      border - radius: 8px; border: 1px solid rgba(255, 255, 255, 0.15);
      z - index: 1000;
    }}

    /* کلاس مخفی‌سازی با !important تا هرچی باشه override کنه */
    #toolbox.is-hidden{{
      opacity: 0;
      transform: translateY(-6px);
      pointer-events: none;
    }}

    #panel{{position:fixed;top:48px;left:260px;width:360px;background:var(--panel);border:1px solid var(--border);
      border-radius:12px;box-shadow:0 10px 30px #0006;display:none;z-index:11}}
    #panel header{{padding:10px 12px;border-bottom:1px solid var(--line);display:flex;gap:8px;align-items:center}}
    #panel header strong{{flex:1}} #panel header button{{background:var(--btn);color:var(--txt);border:1px solid #22FFFFFF;border-radius:8px;padding:6px 10px;cursor:pointer}}
    #panel .body{{padding:10px 12px}}
    table{{width:100%;border-collapse:collapse}} th,td{{border:1px solid var(--line);padding:6px 8px;text-align:left}}
    th{{background:#1F2447}} .nameCell{{max-width:240px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}}
    #pathToast {{
      position: fixed;
      z-index: 9999;
      background: #171B34;
      color: #EAF0FF;
      border: 1px solid #26305E;
      border-radius: 8px;
      padding: 6px 10px;
      box-shadow: 0 6px 20px #0006;
      font: 13px/1.5 system-ui, Segoe UI, Roboto, sans-serif;
      max-width: 420px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      display: none;
      pointer-events: auto; /* تا مزاحم کلیک نشه */
    }}
    #pathToast a{{ cursor:pointer; }}
  </style>
</head>
<body>
  <button id='toggleToolboxBtn' title='Show / Hide toolbox'>
    <span id='eyeIcon'>&#xE717;</span>
  </button>
  <div id='tools' >
    <button class='tb' id='home'>Home</button>
    <button class='tb' id='fit'>Fit</button>
    <button class='tb toggle' id='autorot'>Auto-Rotate</button>
    <button class='tb toggle' id='walk'>Walk</button>

    <button class='tb' id='zin'>Zoom +</button>
    <button class='tb' id='zout'>Zoom −</button>
    <button class='tb' id='fovN'>FOV −</button>
    <button class='tb' id='fovP'>FOV +</button>

    <button class='tb' id='rotL'>⟲</button>
    <button class='tb' id='rotR'>⟳</button>
    <button class='tb' id='tiltU'>Tilt ↑</button>
    <button class='tb' id='tiltD'>Tilt ↓</button>

    <button class='tb' id='panL'>Pan ←</button>
    <button class='tb' id='panR'>Pan →</button>
    <button class='tb' id='panU'>Pan ↑</button>
    <button class='tb' id='panD'>Pan ↓</button>
    <button class='tb toggle' id='pathpeek'>Part-Details</button>

  </div>

  <div id='panel'>
    <header><strong>Selected Part</strong><button id='close'>✕</button></header>
    <div class='body'>
      <table>
        <thead><tr><th>Name</th><th>Related Document</th></tr></thead>
        <tbody>
          <tr><td class='nameCell' id='nm0'>—</td><td>&nbsp;</td></tr>
          <tr><td class='nameCell' id='nm1'>—</td><td>&nbsp;</td></tr>
          <tr><td class='nameCell' id='nm2'>—</td><td>&nbsp;</td></tr>
          <tr><td class='nameCell' id='nm3'>—</td><td>&nbsp;</td></tr>
          <tr><td class='nameCell' id='nm4'>—</td><td>&nbsp;</td></tr>
        </tbody>
      </table>
    </div>
  </div>

  <canvas id='c'></canvas>
  <div id='pathToast' style='display:none'></div>

  <div id='msg'>loading…</div>

  <script type='module'>
    import * as THREE from 'https://assets/three/three.module.js';
    import {{ OrbitControls }} from 'https://assets/three/examples/jsm/controls/OrbitControls.js';
    import {{ GLTFLoader }}  from 'https://assets/three/examples/jsm/loaders/GLTFLoader.js';
    import {{ DRACOLoader }} from 'https://assets/three/examples/jsm/loaders/DRACOLoader.js';
    //import {{ KTX2Loader }}  from 'https://assets/three/examples/jsm/loaders/KTX2Loader.js';

    const msg = document.getElementById('msg'); const log = t => msg.textContent = t;


    // حالت سوییچ
    let pathPeek = false;
    const toast = document.getElementById('pathToast');
    let toastTimer = null;
    let BASE_DIR = '';

    const togglebtn = document.getElementById('toggleToolboxBtn');
    const tools = document.getElementById('tools');
    const eyeIcon = document.getElementById('eyeIcon');

    togglebtn.addEventListener('click', (e) => {{
      e.preventDefault();
      e.stopPropagation();

      const nowHidden = tools.classList.toggle('is-hidden');
      if (nowHidden) {{
        tools.style.display = 'none';
        eyeIcon.textContent = '\uE8F4'; // hide
      }} else {{
        tools.style.display = 'flex'; // مطابق استایل خودت
        eyeIcon.textContent = '\uE717'; // show
      }}
    }});


    function setBaseDir(input){{
      let s = String(input || '').trim();
      if (/^(\/\/\/\/|\\\\)/.test(s)) {{
        s = s.replace(/^\/\/\/\/|^\\\\/, ''); 
        s = s.replace(/[\/]+/g, '\\');
        BASE_DIR = '\\\\' + s.replace(/^\\+/, ''); 
        return;
      }}

      if (/^\/\//.test(s)) {{
        s = s.replace(/^\/\//, '');
        s = s.replace(/[\/]+/g, '\\');
        BASE_DIR = '\\\\' + s.replace(/^\\+/, '');
        return;
      }}
      s = s.replace(/[\/]+/g, '\\');
      BASE_DIR = s;
    }}
    setBaseDir('\\\\192.168.94.4\\Ardestan Dehshir\\1-DCC\\4.Detail Engineering');

    function safeSegment(s) {{
      return String(s ?? '')
        .trim()
        .replace(/[<>:'/\\|?*\u0000-\u001F]/g, ' ')
        .replace(/\s+/g, ' ')     
        .replace(/\.$/, ''); 
    }}

    

    function winJoin() {{
      const parts = [];
      for (const a of arguments) {{
        if (!a) continue;
        let p = String(a).replace(/[\/]+/g, '\\'); 
        p = p.replace(/^[\\]+|[\\]+$/g, '');
        if (!p) continue;
        parts.push(p);
      }}
      return parts.length ? parts[0] + (parts.length>1 ? '\\' + parts.slice(1).join('\\') : '') : '';
    }}

    function getLastRevision(doc) {{
      const revs = Array.isArray(doc?.Revisions) ? doc.Revisions : [];
      if (!revs.length) return '';
      return String(revs[revs.length - 1] ?? '').trim();
    }}


    function buildRelatedPath(doc) {{
      const disc  = safeSegment(doc?.Dicipline);
      const dtype = safeSegment(doc?.Document_type);
      const dnum  = safeSegment(doc?.Document_number);
      const dname = safeSegment(doc?.Document_name);
      const last  = safeSegment(getLastRevision(doc));
      let combo = '';
      if (dnum && dname) combo = `${{dnum}}-${{dname}}`;
      else combo = dnum || dname || '';
      combo = safeSegment(combo);
      //const full = winJoin(BASE_DIR, disc, dtype, combo, last);
      const full = winJoin(disc, dtype, combo, last);
      console.log(full);
      return full;
    }}


    
    
    function normalizeForMatch(s){{
      return String(s||'')
        .toLowerCase()
        .replace(/[_\-\/\\\.]+/g, ' ')
        .replace(/[^a-z0-9\u0600-\u06FF ]+/gi, ' ')
        .split(/\s+/).filter(t => t.length > 1);
    }}



    function setPathPeek(on){{
      pathPeek = !!on;
      document.getElementById('pathpeek').classList.toggle('on', pathPeek);
      // وقتی روشنه، مطمئن شو چیز دیگه‌ای مزاحم نیست
      if (pathPeek) enableAutoRotate(false), enableWalk(false);
    }}


    function tokenOverlapScore(aTokens, bTokens){{
      if (!aTokens.length || !bTokens.length) return 0;
      const setA = new Set(aTokens), setB = new Set(bTokens);
      let inter = 0; for (const t of setA) if (setB.has(t)) inter++;
      // Overlap Coefficient: |A∩B| / min(|A|, |B|)
      return inter / Math.min(setA.size, setB.size);
    }}

    function containsBoost(a, b){{
      // اگر رشته خام شامل هم بود، کمی بونس بده
      const al = String(a||'').toLowerCase();
      const bl = String(b||'').toLowerCase();
      return (al.includes(bl) || bl.includes(al)) ? 0.2 : 0;
    }}

    // ساخت مسیر اجدادی از ریشه تا نود
    function ancestryArray(node){{
      const chain = [];
      let cur = node;
      while(cur){{
        const nm = (cur.name && cur.name.trim()) ? cur.name.trim() : '';
        chain.push(nm || '(no name)');
        cur = cur.parent;
      }}
      return chain.reverse(); // [root,...,leaf]
    }}

    function customFormat(arr) {{
    if (!Array.isArray(arr) || arr.length < 2) {{
        return undefined; // اگر آرایه معتبر نبود یا طول کافی نداشت
    }}

    const secondItem = arr[1]; // عنصر شماره یک (دومین عنصر)
    const firstItem = arr[0]==='RESTURANT' ? 'RESTAURANT' : arr[0];  // عنصر شماره صفر (اولین عنصر)
    const thirdItem = arr[2];

    // بررسی اینکه اولین کاراکتر عدد هست یا نه
    if (/^\d/.test(secondItem)) {{
        const indexOfUnderscore = thirdItem.indexOf('_');
        const indexOfdash = secondItem.indexOf('-');
      const part = indexOfUnderscore !== -1 ? thirdItem.slice(0, indexOfUnderscore) : thirdItem;
      console.log(part);
      const main = indexOfdash !== -1 ? secondItem.slice(0, indexOfdash) : secondItem;
      return `${{ main}} ${{ part}} `;
    }} else {{
        return firstItem;
      }}
    }}

    // گرفتن بخش‌های 3..5 (۱-مبنایی)
    function takeSegments3to5(arr){{
      const segs = arr.slice(4, 8); // ایندکس‌های 3 و 4 (۰-مبنایی) = عناصر 4 و 5
      //if (segs.length === 2 && /equipment|structure/i.test(segs[1])) {{
      //  segs.pop(); // حذف عنصر 5
      //}}
      let fin_segs = segs.splice(2,1);
      for (let i = 0; i < segs.length; i++) {{
        segs[i] = segs[i].replace(/\b(AR|FINISH)\b/gi, '').trim();
      }}
      //console.log(customFormat(segs));
      return customFormat(segs);
    }}

    // نمایش toast به‌مدت ۵ ثانیه
    function showPathToast(text, x, y) {{
      if (toastTimer) {{ clearTimeout(toastTimer); toastTimer = null; }}
      toast.textContent = text && text.length ? text : '—';
      toast.style.display = 'block';
      positionTooltip(toast, x, y);
      toastTimer = setTimeout(() => {{ toast.style.display = 'none'; }}, 5000);
    }}


    let DOCS = null; // [{{ Document_name, Document_number, ... }}]
    async function ensureDocsLoaded(){{
      if (DOCS) return;
      try{{
        const res = await fetch('https://data/database.json', {{ cache: 'no-store' }});
        if (!res.ok) throw new Error(`HTTP ${{res.status}}`);
        DOCS = await res.json();
      }}catch(err){{
        console.warn('Load database.json failed:', err);
        DOCS = []; // بدون خطا پیش بریم
      }}
    }}


    let lastXY = {{ x: null, y: null }};
    window.addEventListener('mousemove', e => {{ lastXY = {{ x: e.clientX, y: e.clientY }}; }}, {{ passive: true }});
    toast.addEventListener('mousedown', e => {{ e.stopPropagation(); }}, {{ passive:true }});

    window.addEventListener('touchmove', e => {{
      if (e.touches && e.touches[0]) lastXY = {{ x: e.touches[0].clientX, y: e.touches[0].clientY }};
    }}, {{ passive: true }});

    

    function getEventXY(ev){{
      if (ev && typeof ev.clientX === 'number' && typeof ev.clientY === 'number')
        return {{ x: ev.clientX, y: ev.clientY }};
      if (ev && ev.touches && ev.touches[0])
        return {{ x: ev.touches[0].clientX, y: ev.touches[0].clientY }};
      // فالبک به آخرین مختصات ثبت‌شده
      return lastXY;
    }}

    function findRelatedDocs(queryTxt, maxResults=10000){{
      if (!DOCS || DOCS.length===0) return [];
      const qTokens = normalizeForMatch(queryTxt);
      
      
      

      // هر سند را امتیاز بده
      const scored = DOCS.map((d, idx) => {{
        const dn = d.Document_name || '';
        const num = d.Document_number || '';
        const s1 = tokenOverlapScore(qTokens, normalizeForMatch(dn)) + containsBoost(dn, queryTxt);
        //const s2 = tokenOverlapScore(qTokens, normalizeForMatch(num)) + containsBoost(num, queryTxt);
        const s2 = 0;
        const score = Math.max(s1, s2);
        return {{ idx, doc: d, score }};
      }});
      const TH = 0.7; // اگر می‌خوای سخت‌گیرتر باشی 0.6 کن
      const hits = scored.filter(x => x.score >= TH)
                        .sort((a,b)=> b.score - a.score)
                        .slice(0, maxResults);
      return hits;
    }}

    function positionTooltip(el, x, y) {{
      const margin = 10; // فاصله از کرسر و لبه‌ها
      el.style.visibility = 'hidden';
      el.style.display = 'block';
      el.style.left = '0px';
      el.style.top  = '0px';

      const vw = window.innerWidth;
      const vh = window.innerHeight;
      const rect = el.getBoundingClientRect();
      const w = rect.width;
      const h = rect.height;

      // فضای اطراف کرسر
      const spaceRight  = vw - x - margin;
      const spaceLeft   = x - margin;
      const spaceBelow  = vh - y - margin;
      const spaceAbove  = y - margin;

      // افقی: اگر راست جا شد، راست؛ وگرنه چپ
      let left = (spaceRight >= w) ? (x + margin) : (x - w - margin);
      // عمودی: اگر پایین جا شد، پایین؛ وگرنه بالا
      let top  = (spaceBelow >= h) ? (y + margin) : (y - h - margin);

      // در نهایت clamp به داخل صفحه
      left = Math.max(margin, Math.min(left, vw - w - margin));
      top  = Math.max(margin, Math.min(top,  vh - h - margin));
      el.style.left = left + 'px';
      el.style.top  = top + 'px';
      el.style.visibility = 'visible';
    }}


    function showPathTable(partText, matches, x, y){{
      if (toastTimer){{ clearTimeout(toastTimer); toastTimer = null; }}

      let html = `<table style='border-collapse:collapse;min-width:260px;max-width:420px'>
        <tr><th style='text-align:left;border-bottom:1px solid #26305E;padding:4px 6px'>Part</th></tr>
        <tr><td style='padding:4px 6px;color:#EAF0FF'>${{partText}}</td></tr>`;

      if (matches.length){{
        html += `<tr><th style='text-align:left;border-bottom:1px solid #26305E;padding:6px 6px 4px'>Related documents</th></tr>`;
        for (let i=0;i<matches.length;i++){{
          const d = matches[i].doc;
          const label = d.Document_name || d.Document_number || '—';
          html += `<tr><td style='padding:3px 6px'>
            <a href='#' class='relDoc' data-idx='${{matches[i].idx}}' style='color:#5DB5FF;text-decoration:none'>
              ${{label}}
            </a>
          </td></tr>`;
        }}
      }}
      html += `</table>`;

      toast.innerHTML = html;
      toast.style.display = 'block';
      positionTooltip(toast, x, y);

      for (const a of toast.querySelectorAll('a.relDoc')) {{
      a.addEventListener('click', (ev) => {{
        ev.preventDefault(); ev.stopPropagation(); ev.stopImmediatePropagation();
        const docs = matches.map(m => ({{
          Document_name:   m.doc?.Document_name || '',
          Document_number: m.doc?.Document_number || '',
          Dicipline:       m.doc?.Dicipline || '',
          Document_type:   m.doc?.Document_type || '',
          Revisions:       Array.isArray(m.doc?.Revisions) ? m.doc.Revisions : []
        }}));

        const msg = {{
          type: 'openDocDetailsBatch',
          payload: {{ PartTitle: partText, Documents: docs }}
        }};

        //console.log('[JS] posting message →', msg);
        if (!window.chrome || !window.chrome.webview) {{
          console.warn('[JS] window.chrome.webview NOT available!');
          return;
        }}
        window.chrome.webview.postMessage(msg);
      }}, {{ passive: false }});
    }}

      toastTimer = setTimeout(()=>{{ toast.style.display = 'none'; }}, 5000);
    }}
    const canvas   = document.getElementById('c');
    const renderer = new THREE.WebGLRenderer({{ canvas, antialias:true, alpha:false }});
    renderer.setPixelRatio(window.devicePixelRatio||1);
    renderer.setSize(window.innerWidth, window.innerHeight);

    const scene = new THREE.Scene(); scene.background = new THREE.Color(0x101218);
    const camera = new THREE.PerspectiveCamera(45, window.innerWidth/window.innerHeight, 0.1, 1e6);
    camera.position.set(0,0,10);

    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;

    scene.add(new THREE.HemisphereLight(0xffffff, 0x222233, 0.7));
    const dir = new THREE.DirectionalLight(0xffffff, 0.8); dir.position.set(5,10,7); scene.add(dir);

    const loader = new GLTFLoader();
    const draco = new DRACOLoader(); draco.setDecoderPath('https://assets/decoders/draco/'); loader.setDRACOLoader(draco);
    //const ktx2  = new KTX2Loader();  ktx2.setTranscoderPath('https://assets/decoders/ktx2/').detectSupport(renderer); loader.setKTX2Loader(ktx2);
    
    const url = 'https://app/{safe}';
    loader.load(url, (gltf) => {{
      const root = gltf.scene || gltf.scenes[0]; scene.add(root);

      // Fit
      const box = new THREE.Box3().setFromObject(root);
      const sizeV = box.getSize(new THREE.Vector3());
      const size = sizeV.length();
      const center = box.getCenter(new THREE.Vector3());

      const fitOffset = 0.2;
      const fov = camera.fov * (Math.PI/180);
      const dist = Math.abs(size / Math.tan(fov/2)) * fitOffset;
      camera.position.copy(center.clone().add(new THREE.Vector3(0,0,1).multiplyScalar(dist)));
      camera.near = Math.max(0.1, size/1000);
      camera.far  = Math.max(1000, size*1000);
      camera.updateProjectionMatrix();
      controls.target.copy(center);
      controls.update();
      log('loaded ✓');
      document.getElementById('tools').style.display='grid';
    }}, undefined, (err) => {{ console.error(err); log('load error'); }});

    // انتخاب Part با Raycaster
    const raycaster = new THREE.Raycaster();
    const mouse = new THREE.Vector2();
    async function pick(e) {{
      if (!pathPeek) return;

      // ⬅️ قبل از هر کاری مختصات را بگیر
      const {{ x, y }} = getEventXY(event);
      if (x == null || y == null) return;  // اگر چیزی نداشتیم، هیچی نشون نده

      const rect = renderer.domElement.getBoundingClientRect();
      const nx = ((x - rect.left) / rect.width) * 2 - 1;
      const ny = -((y - rect.top) / rect.height) * 2 + 1;
      mouse.set(nx, ny);

      raycaster.setFromCamera(mouse, camera);
      const hits = raycaster.intersectObjects(scene.children, true);
      if (!hits.length) return;

      const obj = hits[0].object;
      const arr = ancestryArray(obj);
      const segs = takeSegments3to5(arr);


      //let chain = []; 
      //let cur = obj;
      //while (cur) {{
        //chain.push(cur.name || '(no name)');
        //cur = cur.parent;
      //}}
      //console.log('RAW name:', obj.name);
      //console.log('ANCESTRY path:', chain.reverse().join(' / '));
      //console.log('segs',segs)

      // مونتاژ txt و پاک‌سازی‌های قبلی
      //let txt = segs.join(' / ').trim();
      let txt = segs.replace(/^-+\s*/, '').replace(/\s*-+$/, '').trim();
      if (!txt || txt === '-' || txt === '—') return;

      // ⬅️ بعد از await از x,y ذخیره‌شده استفاده کن، نه event.clientX
      await ensureDocsLoaded();
      const related = findRelatedDocs(segs, 10000);
      if (related.length > 0){{
        showPathTable(txt, related, x, y);
      }} else {{
        showPathToast(txt, x, y);
      }}
    }}
    renderer.domElement.addEventListener('click', pick);

    const recentParts = [];
    function addRecentPart(name){{
      const n = normalizeName(name);
      if(!n) return;
      const idx = recentParts.findIndex(x => x.toLowerCase() === n.toLowerCase());
      if(idx >= 0) recentParts.splice(idx, 1); // اگر بود، حذف کن تا به آخر منتقل شود
      recentParts.push(n);
      // فقط 5 آیتم آخر را نگه دار
      while(recentParts.length > 5) recentParts.shift();
    }}

    // آپدیت جدول از روی recentParts
    function renderRecentTable(){{
      for (let i=0; i<5; i++){{
        const cell = document.getElementById('nm'+i);
        const val = recentParts[recentParts.length-1-i]; // از آخر به اول
        if(cell) cell.textContent = val ? val : '—';
      }}
    }}

    // پنل جدول
    const panel = document.getElementById('panel');
    document.getElementById('close').onclick = ()=> panel.style.display='none';
    function showPanel(partName) {{
      addRecentPart(partName);
      renderRecentTable();
      document.getElementById('panel').style.display = 'block';
    }}

    // Toolbox (بدون Parts) + تضاد Walk/AutoRotate
    const btn = id => document.getElementById(id);
    const toggleOn = (id,on)=> btn(id).classList.toggle('on', !!on);
    const setDisabled = (id,dis)=> btn(id).disabled = !!dis;

    function fitView() {{
      const box = new THREE.Box3().setFromObject(scene);
      const size = box.getSize(new THREE.Vector3()).length();
      const center = box.getCenter(new THREE.Vector3());
      const fitOffset = 1.6;
      const fov = camera.fov * (Math.PI/180);
      const dist = Math.abs(size / Math.tan(fov/2)) * fitOffset;
      camera.position.copy(center.clone().add(new THREE.Vector3(0,0,1).multiplyScalar(dist)));
      camera.near = Math.max(0.1, size/1000);
      camera.far  = Math.max(1000, size*1000);
      camera.updateProjectionMatrix();
      controls.target.copy(center); controls.update();
      enableAutoRotate(false);
    }}
    function homeView() {{ controls.reset(); enableAutoRotate(false); }}

    function normalizeName(s){{
      if(!s) return '';
      const parts = s.split(/[\\/]/).filter(Boolean);
      //if (parts.length>1) return parts[1];
      return parts.length ? parts[parts.length-1] : s;
      return s;
    }}


    function enableAutoRotate(on) {{
      controls.autoRotate = !!on; toggleOn('autorot', on);
      if (on) {{ enableWalk(false); setDisabled('walk', true); }} else {{ setDisabled('walk', false); }}
    }}

    let walk=false, walkSpeed=1.0;
    function enableWalk(on) {{
      walk = on; toggleOn('walk', on);
      if (on) {{ enableAutoRotate(false); setDisabled('autorot', true); }} else {{ setDisabled('autorot', false); }}
      renderer.domElement.style.cursor = on ? 'crosshair' : 'default';
    }}
    window.addEventListener('keydown', (e) => {{
      if (!walk) return;
      const k = e.key.toLowerCase();
      if (['w','a','s','d',' ','c'].includes(k)) e.preventDefault();
      const forward = new THREE.Vector3(); camera.getWorldDirection(forward).normalize();
      const right   = new THREE.Vector3().crossVectors(forward, camera.up).normalize();
      const up      = new THREE.Vector3(0,1,0);
      const step = walkSpeed; const delta = new THREE.Vector3();
      switch(k){{case 'w': delta.add(forward.multiplyScalar(step)); break;
                 case 's': delta.add(forward.multiplyScalar(-step)); break;
                 case 'a': delta.add(right.multiplyScalar(-step)); break;
                 case 'd': delta.add(right.multiplyScalar(+step)); break;
                 case ' ': delta.add(up.multiplyScalar(+step)); break;
                 case 'c': delta.add(up.multiplyScalar(-step)); break;}}
      camera.position.add(delta); controls.target.add(delta); controls.update();
    }});

    function nudgeOrbitYaw(deg) {{
      const vec = camera.position.clone().sub(controls.target);
      const r = vec.length(); const rot = new THREE.Matrix4().makeRotationY(deg*Math.PI/180);
      vec.applyMatrix4(rot); camera.position.copy(controls.target.clone().add(vec.setLength(r))); controls.update();
    }}
    function nudgeOrbitPitch(deg) {{
      const rad = deg*Math.PI/180; const vec = camera.position.clone().sub(controls.target);
      const right = new THREE.Vector3().crossVectors(vec, camera.up).normalize();
      vec.applyMatrix4(new THREE.Matrix4().makeRotationAxis(right, -rad));
      camera.position.copy(controls.target.clone().add(vec)); controls.update();
    }}
    
    function zoom(delta) {{
      const dir = camera.getWorldDirection(new THREE.Vector3());
      camera.position.addScaledVector(dir, -delta); controls.update();
    }}
    
    function pan(dx,dy) {{
      const m = new THREE.Matrix4().extractRotation(camera.matrix);
      const xAxis = new THREE.Vector3(1,0,0).applyMatrix4(m);
      const yAxis = new THREE.Vector3(0,1,0).applyMatrix4(m);
      const pan = new THREE.Vector3().addScaledVector(xAxis, dx).addScaledVector(yAxis, dy);
      camera.position.add(pan); controls.target.add(pan); controls.update();
    }}
    function fov(delta) {{ camera.fov = Math.max(10, Math.min(90, camera.fov + delta)); camera.updateProjectionMatrix(); }}

    function getUniqueNamesForNode(root){{
      const raw = [];
      root.traverse(n => {{
        if (n.name && n.name.trim()) raw.push(n.name.trim());
      }});
      const seen = new Set();
      const uniq = [];
      for (const nm of raw.map(normalizeName).filter(Boolean)) {{
        const key = nm.toLowerCase();
        if (!seen.has(key)) {{ seen.add(key); uniq.push(nm); }}
      }}
      if (uniq.length === 0) {{
        const self = normalizeName(root.name || '');
        if (self) uniq.push(self);
      }}
      return uniq;
    }}

    function renderTableFor(names){{
      for (let i=0; i<5; i++){{
        const cell = document.getElementById('nm'+i);
        const val = names[i] || '—';
        if (cell) cell.textContent = val;
      }}
      document.getElementById('panel').style.display = 'block';
    }}

    //const tools = document.getElementById('tools');
    const bind = (id,fn)=> document.getElementById(id).onclick = fn;
    bind('home', ()=> homeView()); bind('fit', ()=> fitView());
    bind('autorot', ()=> enableAutoRotate(!controls.autoRotate)); bind('walk', ()=> enableWalk(!walk));
    bind('zin', ()=> zoom(-10)); bind('zout', ()=> zoom(+10));
    bind('fovN', ()=> fov(-5)); bind('fovP', ()=> fov(+5));
    bind('rotL', ()=> nudgeOrbitYaw(-10)); bind('rotR', ()=> nudgeOrbitYaw(+10));
    bind('tiltU', ()=> nudgeOrbitPitch(+6)); bind('tiltD', ()=> nudgeOrbitPitch(-6));
    bind('panL', ()=> pan(-2,0)); bind('panR', ()=> pan(+2,0)); bind('panU', ()=> pan(0,+2)); bind('panD', ()=> pan(0,-2));
    bind('pathpeek', ()=> setPathPeek(!pathPeek));
    tools.style.display='grid';

    window.addEventListener('resize', ()=> {{
      camera.aspect = window.innerWidth/window.innerHeight; camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    }});

    (function loop(){{
      requestAnimationFrame(loop);
      controls.update();
      renderer.render(scene, camera);
    }})();
  </script>
</body>
</html>";
      return html;
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
      ThreeDView_Loaded(this, new RoutedEventArgs());
    }

    private void DevTools_Click(object sender, RoutedEventArgs e)
    {
      Web.CoreWebView2?.OpenDevToolsWindow();
    }
  }
}
