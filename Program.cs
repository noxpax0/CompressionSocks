using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CompressionGarmentOrder;

internal static class Program
{
    private const string InstanceMutexName = "Local\\Mercury.CompressionGarmentOrder.SingleInstance";

    [STAThread]
    private static void Main()
    {
        if (UpdateManager.HandleUpdaterMode(Environment.GetCommandLineArgs())) return;
        UpdateManager.ScheduleCleanupAfterUpdate(Environment.GetCommandLineArgs());
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => DiagnosticsLog.Write("ui-thread", args.Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            DiagnosticsLog.Write("background-task", args.Exception);
            args.SetObserved();
        };
        using var mutex = new Mutex(true, InstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Mercury is already open.", "Mercury", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private readonly WebView2 _browser = new() { Dock = DockStyle.Fill };
    private readonly OrderStore _store = new();
    private SoundPlayer? _welcomeSound;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    public MainForm()
    {
        var initialTheme = ReadThemePreference();
        var initialBackground = GetThemeSplashColor(initialTheme);
        Text = "Mercury — Compression Garment Order";
        Icon = LoadWindowIcon();
        BackColor = initialBackground;
        _browser.DefaultBackgroundColor = initialBackground;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(680, 500);
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(1280, 860);
        RestoreWindowSettings();
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(_browser);
        ApplyWindowTheme(initialTheme);
        HandleCreated += (_, _) => ApplyWindowTheme(ReadThemePreference());
        Shown += async (_, _) => await InitialiseBrowserAsync();
        FormClosing += (_, _) => SaveWindowSettings();
    }

    private static Icon LoadWindowIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppAssets.mercury.ico")
            ?? throw new InvalidOperationException("Mercury application icon was not found.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }

    private static Color GetThemeSplashColor(string theme) => theme switch
    {
        "ocean" => Color.FromArgb(120, 147, 156),
        "indigo" => Color.FromArgb(133, 137, 165),
        "terracotta" => Color.FromArgb(169, 137, 125),
        "plum" => Color.FromArgb(147, 131, 148),
        _ => Color.FromArgb(118, 148, 135)
    };

    private async Task InitialiseBrowserAsync()
    {
        try
        {
            DiagnosticsLog.Info("startup", "Beginning WebView2 initialisation.");
            // WebView2's profile is intentionally kept outside the deployment folder.
            var profile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CompressionGarmentOrder", "WebView2Profile");
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: profile,
                options: new CoreWebView2EnvironmentOptions());
            await _browser.EnsureCoreWebView2Async(environment);
            DiagnosticsLog.Info("startup", "WebView2 initialised.");

            var core = _browser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsBuiltInErrorPageEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.DownloadStarting += (_, eventArgs) => eventArgs.Cancel = true;
            core.PermissionRequested += (_, eventArgs) => eventArgs.State = CoreWebView2PermissionState.Deny;
            core.NewWindowRequested += OpenExternalLink;
            core.NavigationStarting += OpenExternalNavigation;
            core.WebMessageReceived += HandleWebMessage;
            string document = BuildEmbeddedDocument(includeSplash: false);
            DiagnosticsLog.Info("startup", $"Embedded document built ({document.Length} characters).");
            core.NavigateToString(document);
            DiagnosticsLog.Info("startup", "Embedded document navigation requested.");
            _ = UpdateManager.CheckAndApplyAsync(this);
        }
        catch (Exception exception)
        {
            DiagnosticsLog.Write("startup", exception);
            MessageBox.Show(
                "Microsoft Edge WebView2 Runtime is required to open this application.\n\n" + exception.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private string BuildEmbeddedDocument(bool includeSplash)
    {
        var html = ReadTextResource("Web.index.html");
        var css = ReadTextResource("Web.styles.css");
        var script = ReadTextResource("Web.script.js");
        var mercuryLogo = ToDataUrl("AppAssets.mercury-logo-cropped.png", "image/png");
        var initialTheme = ReadThemePreference();
        var latestOrder = _store.GetDraftJson();
        var savedOrders = _store.GetOrdersJson();
        html = html.Replace("<html lang=\"en\">", "<html lang=\"en\" data-theme=\"" + initialTheme + "\">");
        if (includeSplash)
        {
            var splashMarkup = "<div class=\"mercury-splash\" aria-label=\"Mercury is loading\"><div class=\"mercury-splash-content\"><span class=\"mercury-splash-logo\" style=\"--mercury-logo:url(" + mercuryLogo + ")\"></span><h2>Hi there!</h2><p>Mercury · Compression Garment Order</p><span class=\"mercury-splash-progress\" aria-hidden=\"true\"></span></div></div>";
            html = html.Replace("<body>", "<body>" + splashMarkup);
        }

        // Compression is unavailable until a garment is selected, so do not report it
        // as missing before the garment-style validation has passed.
        script = script.Replace(
            "if (!compressionLevels[state.compressionId])",
            "if (state.garmentId && !compressionLevels[state.compressionId])");
        // Selecting Open/Closed toe is a garment-only change. Do not nudge or redraw
        // the already available compression step.
        script = Regex.Replace(script,
            @"\s*if \(select\.value\) flashNextStep\(\$\(\""#compressionSection\""\)\);",
            "");
        // Saved orders are opened deliberately from Order History; never interrupt
        // startup or refresh with either of the legacy automatic restore dialogs.
        script = script.Replace("  offerDraftRestore();", "");

        // Prefer the installed WhatsApp desktop application. The host handles the URI and
        // falls back to WhatsApp Web only when Windows has no WhatsApp protocol handler.
        script = Regex.Replace(script,
            @"^\s*const whatsappWindow = window\.open\(\""about:blank\"", \""_blank\""\);\s*\r?\n",
            "", RegexOptions.Multiline);
        script = script.Replace("https://wa.me/?text=", "whatsapp://send?text=");
        script = Regex.Replace(script,
            @"\s*if \(whatsappWindow\) whatsappWindow\.location\.replace\(whatsappUrl\);\s*\r?\n\s*else window\.location\.href = whatsappUrl;",
            "\n    window.location.href = whatsappUrl;");

        foreach (var name in new[]
        {
            "arm.jpg", "breastGarment.jpg", "farrow wrap.jpg", "gauntlet.jpg", "glove.jpg",
            "Knee.jpg", "KneeUlcer.jpg", "Thigh.jpg", "Waist.jpg"
        })
        {
            script = script.Replace("assets/styles/" + name, ToDataUrl("Web.assets.styles\\" + name, "image/jpeg"));
        }

        html = html.Replace("assets/measurement-tape-transparent.png", ToDataUrl("Web.assets.measurement-tape-transparent.png", "image/png"));
        html = html.Replace("<span class=\"brand-mark\" aria-hidden=\"true\">CO</span>", "<span class=\"brand-mark\"><span class=\"brand-logo-shape\" role=\"img\" aria-label=\"Mercury winged compression stocking logo\" style=\"--mercury-logo:url(" + mercuryLogo + ")\"></span></span>");
        html = html.Replace("<h1>Compression Garment Order</h1>", "<div class=\"brand-title\"><h1>Mercury</h1><span>Compression Garment Order</span></div>");
        css += "\n.preferences-panel .notes-column { display: flex; flex-direction: column; }\n.preferences-panel .notes-column textarea { flex: 1 1 auto; min-height: 132px; height: 132px; }\n";
        css += """

            /* Five curated page themes: balanced hue relationships with accessible contrast. */
            :root[data-theme="sage"] { --brand-primary:#3f725f; --brand-primary-dark:#315849; --brand-surface:#f1f5f2; --splash-accent:#aec9bc; --text-primary:#263b34; --text-secondary:#52635a; --border:#d9e1dc; --input-line:#8d9d94; --focus:0 0 0 3px #a7cbb9; }
            :root[data-theme="ocean"] { --brand-primary:#3d7180; --brand-primary-dark:#315a66; --brand-surface:#f0f5f6; --splash-accent:#abc9d0; --text-primary:#263d45; --text-secondary:#50656d; --border:#d7e1e3; --input-line:#899da3; --focus:0 0 0 3px #a5c7cf; }
            :root[data-theme="indigo"] { --brand-primary:#62688f; --brand-primary-dark:#4d5272; --brand-surface:#f3f3f7; --splash-accent:#bdc0d6; --text-primary:#34364b; --text-secondary:#5b5d73; --border:#dedee7; --input-line:#9495a8; --focus:0 0 0 3px #bec1d9; }
            :root[data-theme="terracotta"] { --brand-primary:#936858; --brand-primary-dark:#745146; --brand-surface:#f7f2ef; --splash-accent:#d2b5aa; --text-primary:#493832; --text-secondary:#65534d; --border:#e5dcd7; --input-line:#a28e86; --focus:0 0 0 3px #d5bdb3; }
            :root[data-theme="plum"] { --brand-primary:#796579; --brand-primary-dark:#5f4f60; --brand-surface:#f6f2f6; --splash-accent:#cbb7cb; --text-primary:#423742; --text-secondary:#615362; --border:#e4dde4; --input-line:#9f8f9f; --focus:0 0 0 3px #d0c1d0; }
            body { min-height:100vh; background:linear-gradient(135deg, var(--brand-surface), #f5f4f0 48%, var(--brand-surface)); }
            html { scrollbar-color:var(--brand-primary) transparent; scrollbar-width:thin; }
            html::-webkit-scrollbar { width:9px; }
            html::-webkit-scrollbar-thumb { border:2px solid transparent; border-radius:99px; background:var(--brand-primary); background-clip:padding-box; }
            .site-header { position:sticky; top:0; z-index:65; box-shadow:0 5px 18px #183d2d24; }
            .brand-mark { width:54px; height:54px; padding:0; overflow:visible; background:transparent; box-shadow:none; }
            .brand-logo-shape { display:block; width:100%; height:100%; background:linear-gradient(145deg,#fff,var(--brand-surface)); -webkit-mask-image:var(--mercury-logo); -webkit-mask-position:center; -webkit-mask-repeat:no-repeat; -webkit-mask-size:contain; mask-image:var(--mercury-logo); mask-position:center; mask-repeat:no-repeat; mask-size:contain; filter:drop-shadow(0 3px 4px #00000028); }
            .mercury-splash { position:fixed; z-index:999; inset:0; display:grid; place-items:center; overflow:hidden; background:radial-gradient(circle at 28% 18%, color-mix(in srgb, var(--splash-accent) 25%, transparent), transparent 31%), radial-gradient(circle at 78% 82%, color-mix(in srgb, var(--brand-surface) 30%, transparent), transparent 38%), linear-gradient(145deg, color-mix(in srgb,var(--brand-primary) 66%,var(--brand-surface)), color-mix(in srgb,var(--brand-primary-dark) 62%,var(--brand-surface))); color:var(--splash-accent); transition:opacity 820ms ease, visibility 820ms ease; }
            .mercury-splash::before, .mercury-splash::after { content:''; position:absolute; width:360px; height:360px; border:1px solid color-mix(in srgb, var(--splash-accent) 18%, transparent); border-radius:50%; opacity:.65; }
            .mercury-splash::before { top:-210px; right:-115px; }
            .mercury-splash::after { bottom:-250px; left:-145px; width:440px; height:440px; }
            .mercury-splash.is-leaving { opacity:0; visibility:hidden; }
            .mercury-splash-content { position:relative; display:grid; justify-items:center; gap:13px; min-width:min(420px,calc(100vw - 48px)); padding:38px 48px 31px; overflow:hidden; border:1px solid color-mix(in srgb, var(--splash-accent) 24%, transparent); border-radius:30px; background:linear-gradient(145deg, color-mix(in srgb, var(--brand-primary-dark) 54%, transparent), color-mix(in srgb, var(--brand-primary) 36%, transparent)); box-shadow:0 28px 70px #00000035, inset 0 1px 0 color-mix(in srgb, var(--splash-accent) 16%, transparent); -webkit-backdrop-filter:blur(18px); backdrop-filter:blur(18px); opacity:.8; transition:opacity 820ms ease; animation:mercury-splash-enter 520ms cubic-bezier(.16,.84,.3,1) both paused; }
            .mercury-splash.is-ready .mercury-splash-content { animation-play-state:running; }
            .mercury-splash.is-leaving .mercury-splash-content { opacity:0; }
            .mercury-splash-logo { width:116px; height:116px; margin-bottom:3px; background:linear-gradient(145deg,var(--splash-accent),color-mix(in srgb,var(--splash-accent) 58%,var(--brand-surface))); -webkit-mask-image:var(--mercury-logo); -webkit-mask-position:center; -webkit-mask-repeat:no-repeat; -webkit-mask-size:contain; mask-image:var(--mercury-logo); mask-position:center; mask-repeat:no-repeat; mask-size:contain; filter:drop-shadow(0 12px 20px #0005); }
            .mercury-splash h2 { margin:0; font-size:clamp(2rem,4vw,2.7rem); font-weight:760; letter-spacing:-.045em; }
            .mercury-splash p { margin:0; color:color-mix(in srgb, var(--splash-accent) 78%, var(--brand-surface)); font-size:.78rem; font-weight:700; letter-spacing:.13em; text-transform:uppercase; }
            .mercury-splash-progress { width:92px; height:3px; margin-top:12px; overflow:hidden; border-radius:999px; background:color-mix(in srgb,var(--splash-accent) 18%,transparent); }
            .mercury-splash-progress::after { content:''; display:block; width:100%; height:100%; border-radius:inherit; background:var(--splash-accent); transform-origin:left; animation:mercury-splash-progress 1.4s ease-out both paused; }
            .mercury-splash.is-ready .mercury-splash-progress::after { animation-play-state:running; }
            @keyframes mercury-splash-enter { from { opacity:0; transform:translateY(12px) scale(.95); } to { opacity:1; transform:none; } }
            @keyframes mercury-splash-progress { from { transform:scaleX(0); } to { transform:scaleX(1); } }
            .brand-title { display:grid; gap:0; }
            .brand-title h1 { margin:0; font-size:1.52rem; letter-spacing:-.02em; }
            .brand-title span { color:#ffffffc7; font-size:.73rem; font-weight:650; letter-spacing:.035em; text-transform:uppercase; }
            .panel { box-shadow:0 10px 28px color-mix(in srgb, var(--brand-primary) 7%, transparent); }
            .theme-wheel { width:310px; height:72px; margin-left:auto; overflow:hidden; touch-action:pan-y; user-select:none; }
            .theme-wheel-track { position:relative; height:72px; cursor:grab; }
            .theme-wheel-track:active { cursor:grabbing; }
            .theme-wheel-item { position:absolute; top:7px; left:50%; width:72px; min-height:44px; margin:0 0 0 -36px; padding:0; border:1px solid #ffffff80; border-radius:12px; color:transparent; opacity:.15; cursor:pointer; transform:translateX(0) translateY(0) rotate(0) scale(.8); transition:transform 300ms cubic-bezier(.2,.8,.2,1), opacity 220ms ease, box-shadow 220ms ease; }
            .theme-wheel-item[data-palette="sage"] { background:linear-gradient(135deg,#315849 0 34%,#3f725f 35% 66%,#f1f5f2 67%); }
            .theme-wheel-item[data-palette="ocean"] { background:linear-gradient(135deg,#315a66 0 34%,#3d7180 35% 66%,#f0f5f6 67%); }
            .theme-wheel-item[data-palette="indigo"] { background:linear-gradient(135deg,#4d5272 0 34%,#62688f 35% 66%,#f3f3f7 67%); }
            .theme-wheel-item[data-palette="terracotta"] { background:linear-gradient(135deg,#745146 0 34%,#936858 35% 66%,#f7f2ef 67%); }
            .theme-wheel-item[data-palette="plum"] { background:linear-gradient(135deg,#5f4f60 0 34%,#796579 35% 66%,#f6f2f6 67%); }
            .theme-wheel-item[aria-pressed="true"] { border-color:#fff; box-shadow:0 5px 14px #0004; }
            .theme-wheel-item:focus-visible { outline:2px solid #fff; outline-offset:2px; }
            @media (max-width:600px) { .header-inner { flex-wrap:wrap; } .theme-wheel { width:100%; margin-left:0; } }

            #review, #whatsapp, #print, #copy, #save, #clear { display:none !important; }
            .quick-actions { position:fixed; z-index:70; top:50%; right:18px; display:grid; width:116px; gap:9px; transform:translateY(-50%); isolation:isolate; }
            .quick-actions::before { content:'ACTIONS'; position:absolute; z-index:0; top:50%; left:50%; width:420px; color:var(--brand-primary); font:800 1.35rem/1 system-ui; letter-spacing:.18em; opacity:.08; pointer-events:none; text-align:center; white-space:nowrap; transform:translate(-50%, -50%) rotate(-90deg); }
            .quick-action { position:relative; z-index:1; justify-self:end; display:flex; align-items:center; justify-content:flex-start; width:46px; min-height:46px; padding:0; overflow:hidden; border:2px solid #fff; border-radius:999px; background:#fff; color:var(--brand-primary-dark); box-shadow:0 8px 22px #183d2d38, 0 0 0 1px color-mix(in srgb, var(--brand-primary) 32%, transparent); white-space:nowrap; transition:width 190ms ease, background-color 160ms ease, box-shadow 160ms ease; }
            .quick-action, .quick-action:hover, .quick-action:focus-visible { width:116px; background:var(--brand-surface); box-shadow:0 8px 20px #183d2d24; }
            .quick-action-icon { display:grid; flex:0 0 44px; place-items:center; font-size:1.2rem; }
            .quick-action-label { width:auto; overflow:hidden; font-size:.88rem; font-weight:800; letter-spacing:.01em; line-height:1.15; opacity:1; }
            .quick-action:hover .quick-action-label, .quick-action:focus-visible .quick-action-label { width:auto; opacity:1; }
            .quick-action[data-kind="send"] { border-color:#a8cdb8; color:#087a43; }
            .quick-action[data-kind="clear"] { border-color:#e5b6b0; color:var(--danger); }
            .section-nav { position:fixed; z-index:70; top:94px; right:18px; display:grid; gap:7px; isolation:isolate; }
            .section-nav button { position:relative; z-index:1; display:grid; place-items:center; width:38px; min-height:38px; padding:0; border:1px solid #d5e3dd; border-radius:50%; background:#ffffffeb; color:var(--brand-primary-dark); box-shadow:0 6px 17px #183d2d20; font-size:1rem; }
            .section-nav button:hover, .section-nav button:focus-visible { background:var(--brand-surface); outline:2px solid var(--brand-primary); outline-offset:2px; }
            .section-nav::before { content:''; position:absolute; z-index:0; left:-20px; right:-20px; height:64px; border-radius:50%; background:radial-gradient(ellipse, color-mix(in srgb, var(--brand-primary) 34%, transparent) 0%, color-mix(in srgb, var(--brand-primary) 16%, transparent) 38%, transparent 74%); filter:blur(9px); opacity:0; pointer-events:none; }
            .section-nav.is-scroll-up::before { top:-14px; animation:navigation-nudge 520ms cubic-bezier(.16,.84,.3,1) both; }
            .section-nav.is-scroll-down::before { bottom:-14px; animation:navigation-nudge 520ms cubic-bezier(.16,.84,.3,1) both; }
            @keyframes navigation-nudge { 0% { opacity:0; transform:scale(.72); } 18% { opacity:1; transform:scale(1.04); } 100% { opacity:0; transform:scale(1.45); } }
            @media print {
              @page { size:auto; margin:4mm; }
              #printArea { width:100%; max-width:none; }
              .print-grid { grid-template-columns:repeat(auto-fit,minmax(190px,1fr)); gap:4px 12px; }
              .print-section { margin:7px 0; }
              .print-notes { min-height:36px; }
            }
            @media print and (max-width:480px) {
              #printArea { font-size:8.5pt; }
              .print-header { margin-bottom:7px; padding-bottom:5px; border-bottom-width:2px; }
              .print-header h1 { font-size:14pt; }
              .print-grid { grid-template-columns:1fr; gap:3px; }
              .print-section h2 { margin-bottom:3px; font-size:10pt; }
              .print-garment img { width:42px; height:42px; }
            }
            #errorSummary.is-visible { display:block; }
            .error-tasks { position:fixed; z-index:70; top:50%; left:18px; width:238px; padding:12px; border:1px solid #e7c98d; border-radius:14px; background:#fff8dc; box-shadow:0 10px 26px #5c4b1f2b; color:#5e4a18; transform:translateY(-50%); }
            .error-tasks[hidden] { display:none; }
            .error-tasks h2 { margin:0 0 8px; font-size:.86rem; }
            .error-task-list { display:grid; gap:6px; margin:0; padding:0; list-style:none; }
            .error-task { display:grid; grid-template-columns:20px minmax(0, 1fr); align-items:center; gap:7px; width:100%; min-height:34px; padding:5px 6px; border:0; border-radius:8px; background:transparent; color:inherit; font:600 .72rem/1.2 system-ui; text-align:left; cursor:pointer; }
            .error-task:hover, .error-task:focus-visible { background:#fff0bd; outline:2px solid #d6ae59; outline-offset:1px; }
            .error-task-state { display:grid; place-items:center; width:18px; height:18px; border:1px solid #c69c47; border-radius:50%; color:transparent; font-size:.72rem; }
            .error-task.is-complete { color:#798064; text-decoration:line-through; }
            .error-task.is-complete .error-task-state { border-color:#6d9162; background:#6d9162; color:#fff; }
            .history-calendar { display:grid; gap:10px; }
            .history-calendar-nav { display:flex; align-items:center; justify-content:space-between; gap:8px; }
            .history-calendar-nav strong { font-size:.95rem; }
            .history-calendar-nav button { min-height:32px; padding:4px 10px; border:1px solid var(--border); border-radius:8px; background:#fff; color:var(--brand-primary-dark); }
            .history-weekdays, .history-days { display:grid; grid-template-columns:repeat(7,1fr); gap:4px; }
            .history-weekdays span { color:var(--muted); font-size:.66rem; font-weight:800; text-align:center; text-transform:uppercase; }
            .history-day { position:relative; display:grid; place-items:center; min-height:34px; padding:0; border:1px solid transparent; border-radius:8px; background:transparent; color:var(--ink); font-size:.76rem; }
            .history-day:hover, .history-day.is-selected { border-color:var(--brand-primary); background:var(--brand-surface); }
            .history-day.has-orders::after { content:''; position:absolute; bottom:4px; width:5px; height:5px; border-radius:50%; background:var(--brand-primary); }
            .history-orders { display:grid; gap:6px; max-height:210px; overflow:auto; }
            .history-tools { display:grid; grid-template-columns:1fr auto; gap:8px; }
            .history-tools input, .history-tools select { min-height:38px; border:1px solid var(--input-line); border-radius:8px; background:#fff; color:var(--ink); padding:6px 9px; }
            .history-order { display:grid; gap:2px; width:100%; min-height:46px; padding:8px 10px; border:1px solid var(--border); border-radius:9px; background:#fff; color:var(--ink); text-align:left; }
            .history-order:hover { border-color:var(--brand-primary); background:var(--brand-surface); }
            .history-order small { color:var(--muted); }
            @media (max-width:900px) { .error-tasks { top:auto; bottom:14px; left:12px; transform:none; max-width:calc(100vw - 154px); } }
            @media (max-width:600px) { .quick-actions { top:auto; right:12px; bottom:14px; transform:none; } }
            @media (forced-colors:active) {
              .theme-wheel-item, .quick-action, .section-nav button, .error-task, .panel { border:1px solid CanvasText; }
              .quick-actions::before, .section-nav::before, .section-nav::after { display:none; }
              :focus-visible { outline:3px solid Highlight !important; outline-offset:3px; }
            }
            """;
        html = html.Replace("<link rel=\"stylesheet\" href=\"styles.css\">", "<style>" + css + "</style>");
        const string offlineStorage = "<script>(() => { const values = new Map(); Object.defineProperty(window, 'localStorage', { configurable: true, value: { getItem: key => values.get(String(key)) ?? null, setItem: (key, value) => values.set(String(key), String(value)), removeItem: key => values.delete(String(key)), clear: () => values.clear() } }); const panel = document.querySelector('#preferencesSection'); const colours = panel?.querySelector('.colour-column'); const notes = panel?.querySelector('.notes-column'); if (panel && colours && notes) panel.insertBefore(colours, notes); document.querySelector('#preferencesStepStatus')?.remove(); document.querySelector('.colour-column .optional')?.remove(); })();</script>";
        const string quickActions = "<script>(() => { const actions = [['#review','🔎','Review','review'],['#print','🖨️','Print','print'],['#copy','📋','Copy','copy'],['#save','💾','Save','save'],['#history','🗓️','History','history'],['#whatsapp','💬','Send','send'],['#clear','🗑️','Clear','clear']]; const rail = document.createElement('nav'); rail.className = 'quick-actions'; rail.setAttribute('aria-label', 'Order actions'); actions.forEach(([target, icon, label, kind]) => { const button = document.createElement('button'); button.type = 'button'; button.className = 'quick-action'; button.dataset.kind = kind; button.setAttribute('aria-label', label + ' order'); const symbol = document.createElement('span'); symbol.className = 'quick-action-icon'; symbol.textContent = icon; symbol.setAttribute('aria-hidden', 'true'); const text = document.createElement('span'); text.className = 'quick-action-label'; text.textContent = label; button.append(symbol, text); button.addEventListener('click', () => document.querySelector(target)?.click()); rail.append(button); }); document.body.append(rail); })();</script>";
        const string errorBubble = "<script>(() => { const bubble = document.createElement('aside'); bubble.className = 'error-tasks'; bubble.hidden = true; bubble.setAttribute('aria-live', 'polite'); const heading = document.createElement('h2'); heading.textContent = 'Fix these'; const list = document.createElement('ul'); list.className = 'error-task-list'; bubble.append(heading, list); document.body.append(bubble); let tasks = []; const draw = () => { list.replaceChildren(); bubble.hidden = tasks.length === 0; tasks.forEach(task => { const error = document.getElementById(task.key + '-error'); const complete = !error || !error.textContent.trim(); const item = document.createElement('li'); const button = document.createElement('button'); button.type = 'button'; button.className = 'error-task' + (complete ? ' is-complete' : ''); const state = document.createElement('span'); state.className = 'error-task-state'; state.textContent = complete ? '✓' : ''; state.setAttribute('aria-hidden', 'true'); const text = document.createElement('span'); text.textContent = task.message; button.append(state, text); button.addEventListener('click', () => { const target = document.getElementById(task.target); if (!target) return; target.scrollIntoView({ behavior:'smooth', block:'center' }); const control = target.matches('input, select, textarea, button') ? target : target.querySelector('input, select, textarea, button'); setTimeout(() => control?.focus(), 350); }); item.append(button); list.append(item); }); }; const originalRender = window.renderErrors; window.renderErrors = errors => { originalRender(errors); tasks = errors.map(({ key, target, message }) => ({ key, target, message })); queueMicrotask(draw); }; const originalClearError = window.clearError; window.clearError = key => { originalClearError(key); queueMicrotask(draw); }; })();</script>";
        const string sectionNavigator = "<script>(() => { const sections = () => Array.from(document.querySelectorAll('.workflow-section')); const move = direction => { const headerHeight = document.querySelector('.site-header')?.offsetHeight || 0; const marker = headerHeight + 14; const items = sections(); const candidates = items.filter(section => direction > 0 ? section.getBoundingClientRect().top > marker : section.getBoundingClientRect().top < marker); const target = direction > 0 ? candidates[0] : candidates[candidates.length - 1]; if (!target) return; window.scrollTo({ top: Math.max(0, target.getBoundingClientRect().top + window.scrollY - headerHeight - 12), behavior:'smooth' }); }; const nav = document.createElement('nav'); nav.className = 'section-nav'; nav.setAttribute('aria-label', 'Section navigation'); const up = document.createElement('button'); up.type = 'button'; up.textContent = '↑'; up.setAttribute('aria-label', 'Previous section (Shift+Space)'); up.addEventListener('click', () => move(-1)); const down = document.createElement('button'); down.type = 'button'; down.textContent = '↓'; down.setAttribute('aria-label', 'Next section (Ctrl+Space)'); down.addEventListener('click', () => move(1)); nav.append(up, down); document.body.append(nav); window.addEventListener('keydown', event => { if (event.code !== 'Space' || event.target.closest('input, textarea, select, [contenteditable=true]')) return; if (event.shiftKey && !event.ctrlKey && !event.altKey) { event.preventDefault(); move(-1); } if (event.ctrlKey && !event.shiftKey && !event.altKey) { event.preventDefault(); move(1); } }); })();</script>";
        const string scrollCue = "<script>(() => { const arrows = () => document.querySelectorAll('.section-nav button'); let previousY = window.scrollY, timer; const nudge = direction => { const button = arrows()[direction > 0 ? 1 : 0]; if (!button) return; button.classList.remove('is-scroll-nudge'); void button.offsetWidth; button.classList.add('is-scroll-nudge'); clearTimeout(timer); timer = setTimeout(() => button.classList.remove('is-scroll-nudge'), 680); }; window.addEventListener('scroll', () => { const currentY = window.scrollY, delta = currentY - previousY; if (Math.abs(delta) > 3) nudge(delta); previousY = currentY; }, { passive:true }); })();</script>";
        const string scrollCueBackdrop = "<script>(() => { const nav = document.querySelector('.section-nav'); if (!nav) return; let previousY = window.scrollY, timer; const nudge = direction => { const className = direction > 0 ? 'is-scroll-down' : 'is-scroll-up'; nav.classList.remove('is-scroll-up','is-scroll-down'); void nav.offsetWidth; nav.classList.add(className); clearTimeout(timer); timer = setTimeout(() => nav.classList.remove(className), 540); }; window.addEventListener('scroll', () => { const currentY = window.scrollY, delta = currentY - previousY; if (Math.abs(delta) > 4) nudge(delta); previousY = currentY; }, { passive:true }); })();</script>";
        const string nativePrint = "<script>(() => { const invokeNativePrint = () => { const result=validateForm(); if(!result.valid) return; preparePrintView(result.state); window.chrome?.webview?.postMessage({ kind:'print' }); }; const oldPrint = document.querySelector('#print'); if (oldPrint) { const printButton = oldPrint.cloneNode(true); oldPrint.replaceWith(printButton); printButton.addEventListener('click', invokeNativePrint); } window.printResults = invokeNativePrint; })();</script>";
        const string orderDatabase = "<script>(() => { const decode = value => new TextDecoder().decode(Uint8Array.from(atob(value), char => char.charCodeAt(0))); let saved = null; try { if (window.__mercuryLatestOrder) saved = JSON.parse(decode(window.__mercuryLatestOrder)); } catch { saved = null; } const oldSave = document.querySelector('#save'); if (oldSave) { const save = oldSave.cloneNode(true); oldSave.replaceWith(save); save.addEventListener('click', () => { const state = readFormState(); state.savedAt = new Date().toISOString(); window.chrome?.webview?.postMessage({ kind:'save-order', value:state }); window.__mercuryPendingOrders = [...(window.__mercuryPendingOrders || []), { savedAt:state.savedAt, data:state }]; showToast('Order saved on this device.'); }); } const copy = document.createElement('p'); if (saved) { const person = saved.patient?.name ? ' for ' + saved.patient.name : ''; copy.textContent = 'A saved order' + person + ' is available on this device.'; openModal('Continue previous order?', copy, [{ label:'Start New', className:'secondary', action:() => { clearForm(); closeModal(); } }, { label:'Resume', action:() => { applyDraft(saved); closeModal(); showToast('Saved order resumed.'); } }]); } else { copy.textContent = 'Start a new compression garment order.'; openModal('Welcome to Mercury', copy, [{ label:'Start New', action:closeModal }]); } })();</script>";
        var effectiveOrderDatabase = orderDatabase
            .Replace("if (saved) { const person", "if (saved) { clearForm(); const person")
            .Replace("action:() => { clearForm(); closeModal(); }", "action:() => { window.chrome?.webview?.postMessage({ kind:'discard-resume' }); clearForm(); closeModal(); }");
        const string orderPersistence = "<script>(() => { const oldSave=document.querySelector('#save'); if (!oldSave) return; const save=oldSave.cloneNode(true); oldSave.replaceWith(save); save.addEventListener('click', () => { const state=readFormState(); state.savedAt=new Date().toISOString(); window.chrome?.webview?.postMessage({ kind:'save-order', value:state }); showToast('Draft saved securely on this device.'); }); })();</script>";
        const string draftRestoreOffer = "<script>(() => { if (!window.__mercuryLatestOrder) return; let draft; try { const bytes=Uint8Array.from(atob(window.__mercuryLatestOrder), char => char.charCodeAt(0)); draft=JSON.parse(new TextDecoder().decode(bytes)); } catch { return; } setTimeout(() => { const message=document.createElement('p'); message.textContent=draft.savedAt ? 'A draft saved on ' + new Date(draft.savedAt).toLocaleString() + ' is available. Starting new will discard it.' : 'A saved draft is available. Starting new will discard it.'; openModal('Restore Draft?', message, [{ label:'Start New', className:'neutral', action:() => { window.chrome?.webview?.postMessage({ kind:'discard-resume' }); closeModal(); } }, { label:'Restore Draft', action:() => { applyDraft(draft); closeModal(); showToast('Draft restored.'); } }]); }, 250); })();</script>";
        const string controlledRefresh = "<script>(() => { window.addEventListener('keydown', event => { if (event.key === 'F5') { event.preventDefault(); event.stopImmediatePropagation(); window.chrome?.webview?.postMessage({ kind:'refresh-order' }); } }, true); })();</script>";
        const string orderHistory = "<script>(() => { const decode = value => new TextDecoder().decode(Uint8Array.from(atob(value), char => char.charCodeAt(0))); let orders = []; try { orders = window.__mercuryOrders ? JSON.parse(decode(window.__mercuryOrders)) : []; } catch { orders = []; } const history = document.createElement('button'); history.type = 'button'; history.id = 'history'; history.hidden = true; document.body.append(history); const key = date => [date.getFullYear(), String(date.getMonth()+1).padStart(2,'0'), String(date.getDate()).padStart(2,'0')].join('-'); const show = () => { if (window.__mercuryPendingOrders?.length) { orders.push(...window.__mercuryPendingOrders); window.__mercuryPendingOrders = []; } const content = document.createElement('div'); content.className = 'history-calendar'; const nav = document.createElement('div'); nav.className = 'history-calendar-nav'; const previous = document.createElement('button'); previous.type = 'button'; previous.textContent = '‹'; const title = document.createElement('strong'); const next = document.createElement('button'); next.type = 'button'; next.textContent = '›'; nav.append(previous,title,next); const weekdays = document.createElement('div'); weekdays.className = 'history-weekdays'; ['Su','Mo','Tu','We','Th','Fr','Sa'].forEach(day => { const label = document.createElement('span'); label.textContent = day; weekdays.append(label); }); const days = document.createElement('div'); days.className = 'history-days'; const list = document.createElement('div'); list.className = 'history-orders'; content.append(nav,weekdays,days,list); let month = new Date(); month = new Date(month.getFullYear(),month.getMonth(),1); let selectedDay = key(new Date()); const render = () => { title.textContent = month.toLocaleString(undefined,{month:'long',year:'numeric'}); days.replaceChildren(); const first = new Date(month.getFullYear(),month.getMonth(),1).getDay(); const total = new Date(month.getFullYear(),month.getMonth()+1,0).getDate(); for(let empty=0; empty<first; empty++) days.append(document.createElement('span')); for(let day=1; day<=total; day++) { const value = key(new Date(month.getFullYear(),month.getMonth(),day)); const matches = orders.filter(order => key(new Date(order.savedAt)) === value); const button = document.createElement('button'); button.type = 'button'; button.className = 'history-day' + (matches.length ? ' has-orders' : '') + (selectedDay === value ? ' is-selected' : ''); button.textContent = day; button.setAttribute('aria-label', matches.length ? value + ', ' + matches.length + ' saved order' + (matches.length>1?'s':'') : value); button.addEventListener('click', () => { selectedDay = value; render(); }); days.append(button); } list.replaceChildren(); const matches = orders.filter(order => key(new Date(order.savedAt)) === selectedDay).reverse(); if(!matches.length) { const empty = document.createElement('small'); empty.textContent = 'No saved orders for this day.'; list.append(empty); } matches.forEach(order => { const button = document.createElement('button'); button.type = 'button'; button.className = 'history-order'; const name = document.createElement('strong'); name.textContent = order.data?.patient?.name || 'Unnamed order'; const detail = document.createElement('small'); detail.textContent = new Date(order.savedAt).toLocaleString(); button.append(name,detail); button.addEventListener('click', () => { applyDraft(order.data); closeModal(); showToast('Saved order resumed.'); }); list.append(button); }); }; previous.addEventListener('click', () => { month = new Date(month.getFullYear(),month.getMonth()-1,1); render(); }); next.addEventListener('click', () => { month = new Date(month.getFullYear(),month.getMonth()+1,1); render(); }); render(); openModal('Order history',content,[{label:'Close',action:closeModal}]); }; history.addEventListener('click',show); })();</script>";
        var enhancedOrderHistory = orderHistory
            .Replace("const list = document.createElement('div'); list.className = 'history-orders'; content.append(nav,weekdays,days,list);", "const tools=document.createElement('div'); tools.className='history-tools'; const search=document.createElement('input'); search.type='search'; search.placeholder='Search name or order ID'; search.setAttribute('aria-label','Search order history'); const status=document.createElement('select'); status.setAttribute('aria-label','Filter order status'); status.innerHTML='<option value=completed>Active</option><option value=archived>Archived</option><option value=all>All</option>'; tools.append(search,status); const list = document.createElement('div'); list.className = 'history-orders'; content.append(tools,nav,weekdays,days,list);")
            .Replace("const matches = orders.filter(order => key(new Date(order.savedAt)) === selectedDay).reverse();", "const query=search.value.trim().toLocaleLowerCase(); const matches = orders.filter(order => key(new Date(order.savedAt)) === selectedDay && (status.value==='all' || (order.status || 'completed')===status.value) && (!query || (order.data?.patient?.name || '').toLocaleLowerCase().includes(query) || (order.id || '').toLocaleLowerCase().includes(query))).reverse();")
            .Replace("button.append(name,detail); button.addEventListener('click', () => { applyDraft(order.data); closeModal(); showToast('Saved order resumed.'); }); list.append(button);", "button.append(name,detail); button.addEventListener('click', () => { applyDraft(order.data); closeModal(); showToast('Saved order loaded.'); }); const change=document.createElement('button'); change.type='button'; change.className='neutral'; change.textContent=(order.status==='archived'?'Restore':'Archive'); change.addEventListener('click', event => { event.stopPropagation(); order.status=order.status==='archived'?'completed':'archived'; window.chrome?.webview?.postMessage({kind:'set-order-status',id:order.id,status:order.status}); render(); showToast(order.status==='archived'?'Order archived.':'Order restored.'); }); const row=document.createElement('div'); row.append(button,change); list.append(row);")
            .Replace("previous.addEventListener('click'", "search.addEventListener('input',render); status.addEventListener('change',render); previous.addEventListener('click'");
        const string themePicker = "<script>(() => { const themes = [['sage','Sage clinic'],['ocean','Ocean blue'],['indigo','Indigo calm'],['terracotta','Warm terracotta'],['plum','Plum balance']]; const root = document.documentElement; const header = document.querySelector('.header-inner'); if (!header) return; const wheel = document.createElement('div'); wheel.className = 'theme-wheel'; wheel.setAttribute('role', 'group'); wheel.setAttribute('aria-label', 'Page colour theme selector'); const track = document.createElement('div'); track.className = 'theme-wheel-track'; wheel.append(track); let selected = Math.max(0, themes.findIndex(theme => theme[0] === window.__mercuryInitialTheme)), startX = 0, startSelected = 0, dragged = false; const normalise = value => { const half = 2; return ((value + half) % 5 + 5) % 5 - half; }; const choose = value => { selected = (value + 5) % 5; root.setAttribute('data-theme', themes[selected][0]); window.chrome?.webview?.postMessage({ kind:'theme', value:themes[selected][0] }); Array.from(track.children).forEach((button, itemIndex) => { const delta = normalise(itemIndex - selected), distance = Math.abs(delta); button.style.transform = 'translateX(' + (delta * 78) + 'px) translateY(' + (distance * distance * 5) + 'px) rotate(' + (delta * 12) + 'deg) scale(' + (1 - distance * .08) + ')'; button.style.opacity = String(1 - distance * .3); button.style.zIndex = String(10 - distance); button.setAttribute('aria-pressed', String(itemIndex === selected)); }); }; themes.forEach((theme, itemIndex) => { const button = document.createElement('button'); button.type = 'button'; button.className = 'theme-wheel-item'; button.dataset.palette = theme[0]; button.dataset.index = String(itemIndex); button.setAttribute('aria-label', 'Use ' + theme[1] + ' theme'); button.addEventListener('click', () => choose(itemIndex)); track.append(button); }); wheel.addEventListener('wheel', event => { event.preventDefault(); choose(selected + (event.deltaY > 0 ? 1 : -1)); }, { passive:false }); wheel.addEventListener('pointerdown', event => { const card = event.target.closest('.theme-wheel-item'); if (card) { choose(Number(card.dataset.index)); return; } startX = event.clientX; startSelected = selected; dragged = false; track.setPointerCapture(event.pointerId); }); wheel.addEventListener('pointermove', event => { if (!track.hasPointerCapture(event.pointerId)) return; const movement = event.clientX - startX; if (Math.abs(movement) > 5) dragged = true; choose(startSelected + Math.round(-movement / 78)); }); wheel.addEventListener('pointerup', event => { if (track.hasPointerCapture(event.pointerId)) track.releasePointerCapture(event.pointerId); setTimeout(() => { dragged = false; }, 0); }); wheel.addEventListener('keydown', event => { if (event.key === 'ArrowRight' || event.key === 'ArrowDown') { event.preventDefault(); choose(selected + 1); } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') { event.preventDefault(); choose(selected - 1); } }); header.append(wheel); choose(selected); })();</script>";
        var themeBootstrap = "<script>window.__mercuryInitialTheme='" + initialTheme + "';</script>";
        var orderBootstrap = "<script>window.__mercuryLatestOrder='" + Convert.ToBase64String(Encoding.UTF8.GetBytes(latestOrder ?? string.Empty)) + "';</script>";
        var ordersBootstrap = "<script>window.__mercuryOrders='" + Convert.ToBase64String(Encoding.UTF8.GetBytes(savedOrders)) + "';</script>";
        const string splashDismiss = "<script>(() => { const splash=document.querySelector('.mercury-splash'); if (!splash) return; requestAnimationFrame(() => requestAnimationFrame(() => { splash.classList.add('is-ready'); setTimeout(() => { window.chrome?.webview?.postMessage({ kind:'splash-sound' }); splash.classList.add('is-leaving'); }, 1400); setTimeout(() => splash.remove(), 2100); })); })();</script>";
        return html.Replace("<script src=\"script.js\"></script>", offlineStorage + themeBootstrap + orderBootstrap + ordersBootstrap + themePicker + quickActions + "<script>" + script + "</script>" + errorBubble + sectionNavigator + scrollCue + scrollCueBackdrop + nativePrint + orderPersistence + controlledRefresh + enhancedOrderHistory + splashDismiss + draftRestoreOffer);
    }

    private static string ReadTextResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Embedded resource was not found: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ToDataUrl(string name, string mediaType)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Embedded resource was not found: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return "data:" + mediaType + ";base64," + Convert.ToBase64String(buffer.ToArray());
    }

    private void OpenExternalLink(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        if (!TryGetApprovedExternalUri(args.Uri, out Uri uri)) return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { /* The page remains usable if Windows has no handler for the link. */ }
    }

    private void OpenExternalNavigation(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (Uri.TryCreate(args.Uri, UriKind.Absolute, out Uri? internalUri) && internalUri.Scheme is "about" or "data") return;
        args.Cancel = true;
        if (!TryGetApprovedExternalUri(args.Uri, out Uri uri)) return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch when (uri.Scheme == "whatsapp")
        {
            // WhatsApp Desktop is not installed: preserve the original browser behaviour.
            try { Process.Start(new ProcessStartInfo("https://wa.me/?" + uri.Query.TrimStart('?')) { UseShellExecute = true }); }
            catch { /* The page remains usable if Windows has no handler for the link. */ }
        }
        catch { /* The page remains usable if Windows has no handler for the link. */ }
    }

    private static bool TryGetApprovedExternalUri(string value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? parsed)) return false;
        bool approved = parsed.Scheme == "whatsapp" ||
            (parsed.Scheme == Uri.UriSchemeHttps && parsed.Host is "wa.me" or "web.whatsapp.com" or "github.com");
        if (!approved) return false;
        uri = parsed;
        return true;
    }

    private static string ReadThemePreference()
    {
        try
        {
            var value = File.ReadAllText(GetThemePreferencePath()).Trim();
            return new[] { "sage", "ocean", "indigo", "terracotta", "plum" }.Contains(value) ? value : "sage";
        }
        catch { return "sage"; }
    }

    private void HandleWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            if (args.WebMessageAsJson.Length > 300_000) return;
            using var message = JsonDocument.Parse(args.WebMessageAsJson);
            if (!message.RootElement.TryGetProperty("kind", out var kind)) return;
            if (kind.GetString() == "print")
            {
                _browser.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                return;
            }
            if (kind.GetString() == "save-order" && message.RootElement.TryGetProperty("value", out var order))
            {
                _store.SaveDraft(order.GetRawText());
                return;
            }
            if (kind.GetString() == "complete-order" && message.RootElement.TryGetProperty("value", out order))
            {
                _store.CompleteOrder(order.GetRawText());
                return;
            }
            if (kind.GetString() == "discard-resume")
            {
                _store.DiscardDraft();
                return;
            }
            if (kind.GetString() == "set-order-status" &&
                message.RootElement.TryGetProperty("id", out JsonElement idElement) &&
                message.RootElement.TryGetProperty("status", out JsonElement statusElement))
            {
                _store.SetOrderStatus(idElement.GetString() ?? string.Empty, statusElement.GetString() ?? string.Empty);
                return;
            }
            if (kind.GetString() == "refresh-order")
            {
                _ = _browser.CoreWebView2.ExecuteScriptAsync("showToast('Mercury refreshes automatically when needed.');");
                return;
            }
            if (kind.GetString() == "splash-sound")
            {
                PlayWelcomeSound();
                return;
            }
            if (kind.GetString() != "theme" || !message.RootElement.TryGetProperty("value", out var value)) return;
            var theme = value.GetString();
            if (theme is not ("sage" or "ocean" or "indigo" or "terracotta" or "plum")) return;
            var path = GetThemePreferencePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, theme);
            ApplyWindowTheme(theme);
        }
        catch (Exception exception)
        {
            DiagnosticsLog.Write("web-message", exception);
            _ = _browser.CoreWebView2.ExecuteScriptAsync("showToast('That action could not be completed. Your form is still open.');");
        }
    }

    private void PlayWelcomeSound()
    {
        try
        {
            _welcomeSound ??= new SoundPlayer(CreateWelcomeChime());
            _welcomeSound.Play();
        }
        catch { /* Audio is a cosmetic welcome cue; never interrupt the order form. */ }
    }

    private static Stream CreateWelcomeChime()
    {
        const int sampleRate = 44100;
        const double duration = 0.62;
        var sampleCount = (int)(sampleRate * duration);
        var stream = new MemoryStream(44 + sampleCount * 2);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);

            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (double)sampleRate;
                var attack = Math.Min(1.0, time / 0.045);
                var release = Math.Clamp((duration - time) / 0.42, 0.0, 1.0);
                release = release * release * (3.0 - 2.0 * release);
                var secondTone = Math.Clamp((time - 0.075) / 0.07, 0.0, 1.0);
                var tone = Math.Sin(2 * Math.PI * 523.25 * time) * 0.58
                         + Math.Sin(2 * Math.PI * 659.25 * time) * 0.29 * secondTone
                         + Math.Sin(2 * Math.PI * 783.99 * time) * 0.13 * secondTone;
                var sample = (short)(short.MaxValue * 0.105 * attack * release * tone);
                writer.Write(sample);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private void ApplyWindowTheme(string theme)
    {
        Color chrome = theme switch
        {
            "ocean" => Color.FromArgb(49, 90, 102),
            "indigo" => Color.FromArgb(77, 82, 114),
            "terracotta" => Color.FromArgb(116, 81, 70),
            "plum" => Color.FromArgb(95, 79, 96),
            _ => Color.FromArgb(49, 88, 73)
        };
        if (IsHandleCreated)
        {
            int captionColor = ColorTranslator.ToWin32(chrome);
            int textColor = ColorTranslator.ToWin32(Color.White);
            _ = DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
            _ = DwmSetWindowAttribute(Handle, DwmwaTextColor, ref textColor, sizeof(int));
        }
    }

    private void RestoreWindowSettings()
    {
        WindowSettings? settings = WindowSettings.Load();
        if (settings is null || !settings.IsVisibleOnAnyScreen())
        {
            Bounds = Screen.PrimaryScreen!.WorkingArea;
            WindowState = FormWindowState.Maximized;
            return;
        }
        Bounds = new Rectangle(settings.X, settings.Y, settings.Width, settings.Height);
        WindowState = settings.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
    }

    private void SaveWindowSettings()
    {
        try
        {
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            new WindowSettings(bounds.X, bounds.Y, bounds.Width, bounds.Height,
                WindowState == FormWindowState.Maximized).Save();
        }
        catch (Exception exception) { DiagnosticsLog.Write("window-settings", exception); }
    }

    private static string GetThemePreferencePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mercury", "theme.txt");

}
