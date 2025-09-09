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
    .toolbox{{position:fixed;top:48px;left:8px;display:grid;grid-template-columns:repeat(5,auto);gap:6px;
      background:color-mix(in srgb,var(--panel) 92%,transparent);padding:10px;border-radius:12px;border:1px solid var(--border);backdrop-filter:blur(6px);
      box-shadow:0 10px 30px #0006;z-index:10}}
    .tb{{background:var(--btn);color:var(--txt);border:1px solid #22FFFFFF;border-radius:10px;padding:7px 10px;cursor:pointer;
      transition:transform .06s ease,background .12s ease,opacity .12s ease,outline-color .12s ease;user-select:none}}
    .tb:hover{{background:var(--btnh)}} .tb:active{{transform:scale(.97) translateY(1px)}}
    .tb.toggle.on{{background:var(--on);outline:2px solid var(--accent)}} .tb:disabled{{opacity:.45;cursor:not-allowed;filter:saturate(.6)}}
    #msg{{position:fixed;right:8px;bottom:8px;color:#fff;opacity:.75;font-size:12px}}

    #panel{{position:fixed;top:48px;left:260px;width:360px;background:var(--panel);border:1px solid var(--border);
      border-radius:12px;box-shadow:0 10px 30px #0006;display:none;z-index:11}}
    #panel header{{padding:10px 12px;border-bottom:1px solid var(--line);display:flex;gap:8px;align-items:center}}
    #panel header strong{{flex:1}} #panel header button{{background:var(--btn);color:var(--txt);border:1px solid #22FFFFFF;border-radius:8px;padding:6px 10px;cursor:pointer}}
    #panel .body{{padding:10px 12px}}
    table{{width:100%;border-collapse:collapse}} th,td{{border:1px solid var(--line);padding:6px 8px;text-align:left}}
    th{{background:#1F2447}} .nameCell{{max-width:240px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}}
    #pathToast {{
      position: fixed;
      z-index: 12;
      background: #171B34;
      color: #EAF0FF;
      border: 1px solid #26305E;
      border-radius: 8px;
      padding: 6px 10px;
      box-shadow: 0 6px 20px #0006;
      font: 13px/1.5 system-ui, Segoe UI, Roboto, sans-serif;
      max-width: 320px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      display: none;
      pointer-events: none; /* تا مزاحم کلیک نشه */
    }}
  </style>
</head>
<body>
  <div class='toolbox' id='tools' style='display:none'>
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
    <button class='tb toggle' id='pathpeek'>Path Peek</button>

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

    // گرفتن بخش‌های 3..5 (۱-مبنایی)
    function takeSegments3to5(arr){{
      console.log(arr);
      const segs = arr.slice(4, 6); // ایندکس‌های 3 و 4 (۰-مبنایی) = عناصر 4 و 5
      if (segs.length === 2 && /equipment|structure/i.test(segs[1])) {{
        segs.pop(); // حذف عنصر 5
      }}
      for (let i = 0; i < segs.length; i++) {{
        segs[i] = segs[i].replace(/\b(AR|FINISH)\b/gi, '').trim();
      }}
      return segs;
    }}

    // نمایش toast به‌مدت ۵ ثانیه
    function showPathToast(text, x, y) {{
      if (toastTimer) {{
        clearTimeout(toastTimer);
        toastTimer = null;
      }}
      toast.textContent = text && text.length ? text : '—';
      // موقعیت: کمی بالاتر از محل کلیک
      toast.style.left = (x + 10) + 'px';
      toast.style.top  = (y - 30) + 'px';
      toast.style.display = 'block';
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

    function findRelatedDocs(queryTxt, maxResults=5){{
      if (!DOCS || DOCS.length===0) return [];
      const qTokens = normalizeForMatch(queryTxt);

      // هر سند را امتیاز بده
      const scored = DOCS.map((d, idx) => {{
        const dn = d.Document_name || '';
        const num = d.Document_number || '';
        const s1 = tokenOverlapScore(qTokens, normalizeForMatch(dn)) + containsBoost(dn, queryTxt);
        const s2 = tokenOverlapScore(qTokens, normalizeForMatch(num)) + containsBoost(num, queryTxt);
        const score = Math.max(s1, s2);
        return {{ idx, doc: d, score }};
      }});
      const TH = 0.5; // اگر می‌خوای سخت‌گیرتر باشی 0.6 کن
      const hits = scored.filter(x => x.score >= TH)
                        .sort((a,b)=> b.score - a.score)
                        .slice(0, maxResults);
      return hits;
    }}

    function showPathTable(partText, matches, x, y){{
      if (toastTimer){{ clearTimeout(toastTimer); toastTimer = null; }}

      // سطر اول: نام پارت
      let html = `<table style='border-collapse:collapse;min-width:260px'>
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
      toast.style.left = (x + 10) + 'px';
      toast.style.top  = (y - 30) + 'px';
      toast.style.display = 'block';

      // هندل کلیک روی لینک‌ها (فعلاً مقصد نداریم)
      for (const a of toast.querySelectorAll('a.relDoc')){{
        a.addEventListener('click', (ev)=>{{
          ev.preventDefault();
          const idx = Number(a.getAttribute('data-idx'));
          const d = DOCS[idx];
          // TODO: مقصد نهایی را بعداً جایگزین کن
          alert(`[Document clicked]\n${{d.Document_name || d.Document_number}}`);
        }});
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

      const fitOffset = 1.6;
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

      // مونتاژ txt و پاک‌سازی‌های قبلی
      let txt = segs.join(' / ').trim();
      txt = txt.replace(/^-+\s*/, '').replace(/\s*-+$/, '').trim();
      if (!txt || txt === '-' || txt === '—') return;

      // ⬅️ بعد از await از x,y ذخیره‌شده استفاده کن، نه event.clientX
      await ensureDocsLoaded();
      const related = findRelatedDocs(txt, 5);
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

    const tools = document.getElementById('tools');
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
