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

            // فایل‌های محلی را روی یک هاست مجازی می‌گذاریم
            Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app", modelsDir, CoreWebView2HostResourceAccessKind.Allow);

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

            // Toolbox: top-left چسبیده، toggle UI، افکت کلیک، و ناسازگاری Walk/AutoRotate
            var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1'/>
  <script type='module' src='https://unpkg.com/@google/model-viewer/dist/model-viewer.min.js'></script>
  <style>
    :root {{
      --bg:#101218; --panel:#171B34; --txt:#EAF0FF; --btn:#1F2447; --btnh:#2A3162; --accent:#5DB5FF; --on:#274a7a;
    }}
    html,body{{height:100%;margin:0;background:var(--bg);color:var(--txt);font:13px system-ui,Segoe UI,Roboto,sans-serif}}
    model-viewer{{width:100%;height:100%}}

    .toolbox{{
      position:fixed; top:48px; left:8px;  /* بالای فرم، کنار چپ؛ 48px تا با هدر 40px تداخل نداشته باشه */
      display:grid; grid-template-columns:repeat(4,auto); gap:6px;
      background:color-mix(in srgb, var(--panel) 92%, transparent);
      padding:10px; border-radius:12px; border:1px solid #1FFFFFFF; backdrop-filter:blur(6px);
      box-shadow:0 10px 30px #0006; z-index:10;
    }}
    .toolbox h4{{grid-column:1/-1;margin:0 0 6px;font-weight:600;font-size:12px;opacity:.85}}

    .tb{{ background:var(--btn); color:var(--txt); border:1px solid #22FFFFFF; border-radius:10px; padding:7px 10px;
         cursor:pointer; transition:transform .06s ease, background .12s ease, opacity .12s ease, outline-color .12s ease; user-select:none; }}
    .tb:hover{{ background:var(--btnh); }}
    .tb:active{{ transform:scale(0.97) translateY(1px); }}

    /* حالت toggle فعال */
    .tb.toggle.on{{ background:var(--on); outline:2px solid var(--accent); }}

    /* حالت غیرفعال (grayed out) */
    .tb:disabled{{ opacity:.45; cursor:not-allowed; filter:saturate(.6); }}

    #msg{{ position:fixed; right:8px; bottom:8px; color:#fff; opacity:.75; font-size:12px }}
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

  <!-- Toolbox (بالا-چپ) -->
  <div class='toolbox' id='tools' style='display:none'>
    <h4>TOOLS</h4>

    <!-- Home / Fit / AutoRotate / Walk -->
    <button class='tb'          id='home'   title='Reset view'>Home</button>
    <button class='tb'          id='fit'    title='Fit to view'>Fit</button>
    <button class='tb toggle'   id='autorot' title='Toggle auto-rotate'>Auto-Rotate</button>
    <button class='tb toggle'   id='walk'    title='Walk mode (WASD + Mouse)'>Walk</button>

    <!-- Zoom / FOV -->
    <button class='tb' id='zin'  title='Zoom In'>Zoom +</button>
    <button class='tb' id='zout' title='Zoom Out'>Zoom −</button>
    <button class='tb' id='fovN' title='Narrow FOV'>FOV −</button>
    <button class='tb' id='fovP' title='Widen FOV'>FOV +</button>

    <!-- Rotate / Tilt -->
    <button class='tb' id='rotL' title='Rotate left'>⟲</button>
    <button class='tb' id='rotR' title='Rotate right'>⟳</button>
    <button class='tb' id='tiltU' title='Tilt up'>Tilt ↑</button>
    <button class='tb' id='tiltD' title='Tilt down'>Tilt ↓</button>

    <!-- Pan -->
    <button class='tb' id='panL' title='Pan left'>Pan ←</button>
    <button class='tb' id='panR' title='Pan right'>Pan →</button>
    <button class='tb' id='panU' title='Pan up'>Pan ↑</button>
    <button class='tb' id='panD' title='Pan down'>Pan ↓</button>
  </div>

  <div id='msg'>loading…</div>

  <script>
    const mv  = document.getElementById('mv');
    const box = document.getElementById('tools');
    const msg = document.getElementById('msg');
    const log = t => msg.textContent = t;

    // Helpers
    const deg = v => v.toFixed(1)+'deg';
    const pct = v => v.toFixed(1)+'%';
    const clamp = (x,a,b)=>Math.max(a,Math.min(b,x));

    function parseOrbit(s) {{
      const [a,e,r] = s.split(' ');
      return {{ az: parseFloat(a), el: parseFloat(e), r: parseFloat(r) }};
    }}
    function setOrbit(az, el, r) {{
      mv.cameraOrbit = `${{deg(az)}} ${{deg(el)}} ${{pct(r)}}`;
      mv.jumpCameraToGoal();
    }}
    function nudgeOrbit(daz, del, drPct=0) {{
      const o = parseOrbit(mv.cameraOrbit);
      setOrbit(o.az + daz, clamp(o.el + del, -89, 89), clamp(o.r + drPct, 5, 500));
    }}
    function nudgeFOV(df) {{
      const f = parseFloat(mv.fieldOfView);
      mv.fieldOfView = deg(clamp(f + df, 10, 90));
      mv.jumpCameraToGoal();
    }}
    function nudgeTarget(dx, dy, dz) {{
      const [x,y,z] = mv.cameraTarget.toString().split(' ').map(parseFloat);
      mv.cameraTarget = `${{(x+dx).toFixed(3)}} ${{(y+dy).toFixed(3)}} ${{(z+dz).toFixed(3)}}`;
      mv.jumpCameraToGoal();
    }}
    function fitView() {{
      mv.cameraOrbit='0deg 65deg 140%'; mv.fieldOfView='45deg'; mv.cameraTarget='0 0 0'; mv.autoRotate=false; mv.jumpCameraToGoal();
      setToggle('autorot', false); // sync UI
    }}
    function homeView() {{
      mv.cameraOrbit='0deg 0deg 200%'; mv.fieldOfView='45deg'; mv.cameraTarget='0 0 0'; mv.autoRotate=false; mv.jumpCameraToGoal();
      setToggle('autorot', false);
    }}

    // حالت Walk
    let walk = false, yaw = 0, pitch = -10, radius = 40, speed = 1.0;
    let lastX=0, lastY=0, dragging=false;

    function setToggle(id, on) {{
      const el = document.getElementById(id);
      el.classList.toggle('on', !!on);
      if (id==='autorot') mv.autoRotate = !!on;
      if (id==='walk')    enableWalk(!!on, false); // false: از این‌جا تریگر UI نکن
    }}

    function setDisabled(id, dis) {{
      const el = document.getElementById(id);
      el.disabled = !!dis;
      if (dis) el.classList.remove('on');
    }}

    function enableWalk(on, fromBtn=true) {{
      walk = on;
      const btn = document.getElementById('walk');
      btn.classList.toggle('on', on);
      mv.style.cursor = on ? 'crosshair' : 'default';

      if (on) {{
        // آماده‌سازی اوربیت
        const o = parseOrbit(mv.cameraOrbit);
        yaw = o.az; pitch = clamp(o.el, -75, 75); radius = clamp(o.r, 10, 200);
        // ناسازگاری: Walk با AutoRotate
        setToggle('autorot', false);
        setDisabled('autorot', true);
      }} else {{
        setDisabled('autorot', false);
      }}
      if (fromBtn) {{ /* کلیک روی خود Walk بود */ }}
    }}

    function updateWalkOrbit() {{ setOrbit(yaw, pitch, radius); }}

    // Keyboard برای Walk
    window.addEventListener('keydown', e => {{
      if (!walk) return;
      const key = e.key.toLowerCase();
      if (['w','a','s','d',' ','c','q','e'].includes(key)) e.preventDefault();

      const rad = d=>d*Math.PI/180;
      const fx = Math.cos(rad(yaw)) * Math.cos(rad(pitch));
      const fy = Math.sin(rad(pitch));
      const fz = Math.sin(rad(yaw)) * Math.cos(rad(pitch));
      const rx = Math.cos(rad(yaw+90));
      const rz = Math.sin(rad(yaw+90));

      let [x,y,z] = mv.cameraTarget.toString().split(' ').map(parseFloat);
      switch(key) {{
        case 'w': x+=fx*speed; y+=fy*speed; z+=fz*speed; break;
        case 's': x-=fx*speed; y-=fy*speed; z-=fz*speed; break;
        case 'a': x-=rx*speed; z-=rz*speed; break;
        case 'd': x+=rx*speed; z+=rz*speed; break;
        case ' ': y+=speed; break;
        case 'c': y-=speed; break;
        case 'q': radius = clamp(radius+5, 5, 500); updateWalkOrbit(); break;
        case 'e': radius = clamp(radius-5, 5, 500); updateWalkOrbit(); break;
      }}
      mv.cameraTarget = `${{x.toFixed(3)}} ${{y.toFixed(3)}} ${{z.toFixed(3)}}`;
      mv.jumpCameraToGoal();
    }});

    // Mouse look برای Walk
    mv.addEventListener('mousedown', e => {{ if (!walk) return; dragging=true; lastX=e.clientX; lastY=e.clientY; }});
    mv.addEventListener('mouseup',   e => {{ dragging=false; }});
    mv.addEventListener('mouseleave',e => {{ dragging=false; }});
    mv.addEventListener('mousemove', e => {{
      if (!walk || !dragging) return;
      const dx = e.clientX-lastX, dy = e.clientY-lastY; lastX=e.clientX; lastY=e.clientY;
      yaw=(yaw+dx*0.3); pitch=clamp(pitch-dy*0.25, -85, 85); updateWalkOrbit();
    }});

    // اتصال دکمه‌ها
    function bind(id, fn) {{ document.getElementById(id).onclick = fn; }}
    function toggleBind(id, getter, setter) {{
      const el = document.getElementById(id);
      const sync = ()=> el.classList.toggle('on', !!getter());
      el.onclick = ()=>{{ setter(!getter()); sync(); }};
      sync();
    }}

    // ناسازگاری‌ها: AutoRotate ↔ Walk (یکی روشن شد، دیگری خاموش و disabled)
    function enableAutoRotate(on) {{
      mv.autoRotate = !!on;
      const arBtn = document.getElementById('autorot');
      arBtn.classList.toggle('on', !!on);
      if (on) {{
        // AutoRotate روشن → Walk خاموش و غیرفعال
        enableWalk(false);
        setDisabled('walk', true);
      }} else {{
        // AutoRotate خاموش → Walk دوباره قابل استفاده
        setDisabled('walk', false);
      }}
    }}

    // پس از لود مدل
    mv.addEventListener('load', () => {{
      log('loaded ✓');
      box.style.display = 'grid';

      // دکمه‌های ساده
      bind('home', ()=> homeView());
      bind('fit',  ()=> fitView());

      bind('zin',  ()=> nudgeOrbit(0,0,-10));
      bind('zout', ()=> nudgeOrbit(0,0,+10));
      bind('fovN', ()=> nudgeFOV(-5));
      bind('fovP', ()=> nudgeFOV(+5));

      bind('rotL', ()=> nudgeOrbit(-10,0,0));
      bind('rotR', ()=> nudgeOrbit(+10,0,0));
      bind('tiltU',()=> nudgeOrbit(0,-6,0));
      bind('tiltD',()=> nudgeOrbit(0,+6,0));

      bind('panL', ()=> nudgeTarget(-2,0,0));
      bind('panR', ()=> nudgeTarget(+2,0,0));
      bind('panU', ()=> nudgeTarget(0,+2,0));
      bind('panD', ()=> nudgeTarget(0,-2,0));

      // Toggleها
      document.getElementById('walk').onclick = ()=> enableWalk(!document.getElementById('walk').classList.contains('on'));
      document.getElementById('autorot').onclick = ()=> enableAutoRotate(!mv.autoRotate);

      // حالت اولیه
      enableAutoRotate(false);
      enableWalk(false);
    }});

    mv.addEventListener('error', e => {{
      console.error('model-viewer error', e.detail);
      log('error: ' + (e.detail?.message||'unknown'));
    }});
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
