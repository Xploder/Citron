using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Citron;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log(e.ExceptionObject.ToString()!);
        using var single = new Mutex(true, "Local\\Citron.Desktop", out bool first);
        if (!first) return;
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ => { SynchronizationContext.SetSynchronizationContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread())); new CitronApp(); });
    }
    internal static void Log(string message)
    {
        try { string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Citron");
        Directory.CreateDirectory(path); File.AppendAllText(Path.Combine(path, "startup.log"), DateTime.Now + " " + message + Environment.NewLine); } catch { }
    }
}
internal sealed class Preferences
{
    public string Language { get; set; } = Strings.Language;
    public int Interval { get; set; } = 60;
    public int Duration { get; set; } = 0;
    public bool KeepAwake { get; set; }
    public bool SetupPrompted { get; set; }
}
public sealed partial class CitronApp : Application
{
    public CitronApp() { UnhandledException += (_, e) => Program.Log(e.Exception.ToString()); InitializeComponent(); }
    Window window = null!;
    StackPanel root = null!;
    TextBlock status = null!;
    ComboBox sessions = null!;
    ToggleSwitch enabled = null!;
    NumberBox interval = null!, duration = null!;
    CheckBox awake = null!;
    Forms.NotifyIcon tray = null!;
    DispatcherTimer timer = null!;
    Preferences prefs = new();
    string architecture = "x64";
    bool running, busy, locked, exiting, setupNeeded;
    DateTimeOffset started, next;
    string? target;
    static string PrefPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Citron", "settings.json");
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Program.Log("Launch");
        try { prefs = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(PrefPath)) ?? new(); } catch { }
        Strings.Language = prefs.Language;
        window = new Window { Title = "Citron", SystemBackdrop = new MicaBackdrop() };
        Program.Log("Window created");
        double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window)) / 96d;
        var area = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(window.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32(Math.Min((int)(660 * scale), area.Width), Math.Min((int)(860 * scale), area.Height - 32)));
        window.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Citron.ico"));
        window.ExtendsContentIntoTitleBar = true;
        window.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        window.AppWindow.Closing += (_, e) => { if (!exiting) { e.Cancel = true; window.AppWindow.Hide(); } };
        BuildUi();
        Program.Log("UI created");
        tray = new Forms.NotifyIcon { Text = "Citron", Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "CitronPaused.ico")), Visible = true };
        tray.DoubleClick += (_, _) => window.DispatcherQueue.TryEnqueue(() => { if (!running && sessions.SelectedItem == null) window.Activate(); else enabled.IsOn = !running; });
        TrayMenu();
        Program.Log("Tray created");
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += async (_, _) => await Tick();
        timer.Start();
        SystemEvents.SessionSwitch += OnSessionSwitch;
        window.Activate();
        Program.Log("Window activated");
        _ = Initialize();
    }
    async Task Initialize()
    {
        await Scan();
        if (!setupNeeded || prefs.SetupPrompted) return;
        prefs.SetupPrompted = true; Save();
        try { await Configure(false); } catch (Exception ex) { await Error(ex.Message); }
    }
    void OnSessionSwitch(object sender, SessionSwitchEventArgs e) => window.DispatcherQueue.TryEnqueue(() =>
    {
        if (e.Reason == SessionSwitchReason.SessionLock) { locked = true; SetThreadExecutionState(0x80000000); status.Text = Strings.T("locked"); }
        if (e.Reason == SessionSwitchReason.SessionUnlock) { locked = false; next = DateTimeOffset.UtcNow; }
    });
    void BuildUi()
    {
        root = new StackPanel { Spacing = 14, Margin = new Thickness(28, 16, 28, 28) };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        brand.Children.Add(new Image { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Citron.png")), Width = 44, Height = 44 });
        brand.Children.Add(new TextBlock { Text = "Citron", FontSize = 34, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }); root.Children.Add(brand);
        root.Children.Add(new TextBlock { Text = Strings.T("subtitle"), FontSize = 18 });
        status = new TextBlock { Text = Strings.T("idle"), TextWrapping = TextWrapping.Wrap, FontSize = 14 };
        root.Children.Add(new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(28, 110, 180, 95)), Child = status });
        sessions = new ComboBox { Header = Strings.T("target"), HorizontalAlignment = HorizontalAlignment.Stretch };
        root.Children.Add(sessions);
        Button refresh = Button("refresh", async () => await Scan()); root.Children.Add(refresh);
        enabled = new ToggleSwitch { Header = Strings.T("enable"), OnContent = "●", OffContent = "○" };
        enabled.Toggled += (_, _) =>
        {
            running = enabled.IsOn && sessions.SelectedItem is string;
            if (busy && enabled.IsOn) { enabled.IsOn = false; return; }
            if (enabled.IsOn && !running) { enabled.IsOn = false; status.Text = Strings.T("none"); return; }
            target = running ? (string)sessions.SelectedItem : null;
            sessions.IsEnabled = !running;
            started = next = DateTimeOffset.UtcNow;
            if (!running) SetThreadExecutionState(0x80000000);
            status.Text = Strings.T(running ? "active" : "idle");
            if (tray != null) { tray.Text = "Citron — " + (running ? "●" : "○"); var oldIcon = tray.Icon; tray.Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets", running ? "Citron.ico" : "CitronPaused.ico")); oldIcon?.Dispose(); }
        };
        root.Children.Add(enabled);
        interval = new NumberBox { Header = Strings.T("interval"), Minimum = 15, Maximum = 3600, Value = Math.Clamp(prefs.Interval, 15, 3600), SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        duration = new NumberBox { Header = Strings.T("duration"), Minimum = 0, Maximum = 1440, Value = Math.Clamp(prefs.Duration, 0, 1440), SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        interval.ValueChanged += (_, _) => Save(); duration.ValueChanged += (_, _) => Save();
        root.Children.Add(interval); root.Children.Add(duration);
        awake = new CheckBox { Content = Strings.T("sleep"), IsChecked = prefs.KeepAwake };
        awake.Checked += (_, _) => Save(); awake.Unchecked += (_, _) => { Save(); SetThreadExecutionState(0x80000000); }; root.Children.Add(awake);
        var language = new ComboBox { Header = Strings.T("language"), HorizontalAlignment = HorizontalAlignment.Stretch };
        string[] codes = ["en", "de", "es", "fr", "pt"];
        foreach (string label in new[] { "English", "Deutsch", "Español", "Français", "Português" }) language.Items.Add(label);
        language.SelectedIndex = Math.Max(0, Array.IndexOf(codes, Strings.Language));
        language.SelectionChanged += (_, _) => { enabled.IsOn = false; Strings.Language = codes[language.SelectedIndex]; Save(); BuildUi(); TrayMenu(); _ = Scan(); };
        root.Children.Add(language);
        var actions = new StackPanel { Spacing = 8 };
        actions.Children.Add(Button("setup", () => Configure(false)));
        actions.Children.Add(Button("restore", () => Configure(true))); root.Children.Add(actions);
        root.Children.Add(new TextBlock { Text = Strings.T("about"), TextWrapping = TextWrapping.Wrap, Opacity = .65, FontSize = 12 });
        root.Children.Add(Button("exit", () => { Quit(); return Task.CompletedTask; }));
        var shell = new Grid();
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var titlebar = new Grid { Margin = new Thickness(16, 0, 144, 0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        var titleBrand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        titleBrand.Children.Add(new Image { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Citron.png")), Width = 20, Height = 20 });
        titleBrand.Children.Add(new TextBlock { Text = "Citron", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Opacity = .8 });
        titlebar.Children.Add(titleBrand); shell.Children.Add(titlebar);
        var scroll = new ScrollViewer { Content = root }; Grid.SetRow(scroll, 1); shell.Children.Add(scroll);
        void UpdateCaption() { bool dark = root.ActualTheme == ElementTheme.Dark; window.AppWindow.TitleBar.ButtonForegroundColor = dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black; window.AppWindow.TitleBar.ButtonInactiveForegroundColor = dark ? Microsoft.UI.Colors.Gray : Microsoft.UI.Colors.DimGray; }
        root.ActualThemeChanged += (_, _) => UpdateCaption(); root.Loaded += (_, _) => UpdateCaption();
        window.Content = shell; window.SetTitleBar(titlebar);
    }
    Button Button(string key, Func<Task> action)
    {
        var button = new Button { Content = Strings.T(key) };
        button.Click += async (_, _) => { button.IsEnabled = false; try { await action(); } catch (Exception ex) { await Error(ex.Message); } finally { button.IsEnabled = true; } };
        return button;
    }
    void TrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Strings.T("settings"), null, (_, _) => window.DispatcherQueue.TryEnqueue(() => window.Activate()));
        menu.Items.Add(Strings.T("toggle"), null, (_, _) => window.DispatcherQueue.TryEnqueue(() => enabled.IsOn = !enabled.IsOn));
        menu.Items.Add(Strings.T("exit"), null, (_, _) => window.DispatcherQueue.TryEnqueue(Quit));
        var old = tray.ContextMenuStrip; tray.ContextMenuStrip = menu; old?.Dispose();
    }
    void Quit() { exiting = true; timer.Stop(); SetThreadExecutionState(0x80000000); SystemEvents.SessionSwitch -= OnSessionSwitch; tray.Visible = false; tray.Icon?.Dispose(); tray.Dispose(); window.Close(); Exit(); }
    void Save()
    {
        prefs.Language = Strings.Language;
        if (double.IsFinite(interval.Value)) prefs.Interval = (int)Math.Clamp(interval.Value, 15, 3600);
        if (double.IsFinite(duration.Value)) prefs.Duration = (int)Math.Clamp(duration.Value, 0, 1440);
        prefs.KeepAwake = awake?.IsChecked == true;
        try { Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!); File.WriteAllText(PrefPath, JsonSerializer.Serialize(prefs)); } catch (Exception ex) { status.Text = ex.Message; }
    }
    string Worker => Path.Combine(AppContext.BaseDirectory, "workers", architecture, "Citron.Worker.exe");
    async Task<JsonElement> Run(string command, string? session = null)
    {
        var info = new ProcessStartInfo(Worker) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(command); if (session != null) info.ArgumentList.Add(session);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Worker could not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); await process.WaitForExitAsync(); throw new TimeoutException("Citrix did not respond within 12 seconds."); }
        string result = await output; string error = await errors;
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException(error.Length > 0 ? error : "Worker returned no result.");
        var json = JsonDocument.Parse(result).RootElement.Clone();
        if (!json.GetProperty("ok").GetBoolean()) throw new InvalidOperationException(json.GetProperty("error").GetString());
        return json;
    }
    async Task<JsonElement> Detect()
    {
        foreach (var arch in new[] { "x64", "x86" })
        {
            architecture = arch;
            var check = await Run("check");
            if (check.GetProperty("component").ValueKind == JsonValueKind.String) return check;
        }
        throw new InvalidOperationException("No supported Citrix ICA COM component is registered.");
    }
    async Task Scan()
    {
        Program.Log("Scan requested");
        if (busy || running) return;
        busy = true;
        try
        {
            sessions.Items.Clear();
            var check = await Detect();
            Program.Log("Component detected");
            setupNeeded = !check.GetProperty("configured").GetBoolean();
            if (setupNeeded) { status.Text = Strings.T("setupNeeded"); return; }
            var result = await Run("scan");
            foreach (var s in result.GetProperty("sessions").EnumerateArray()) sessions.Items.Add(s.GetString()!);
            if (sessions.Items.Count > 0) { sessions.SelectedIndex = 0; status.Text = Strings.T("idle"); }
            else status.Text = Strings.T("none");
        }
        catch (Exception ex) { status.Text = Strings.T("error") + "\n" + ex.Message; }
        finally { busy = false; }
    }
    async Task Configure(bool restore)
    {
        if (busy) return;
        enabled.IsOn = false; busy = true;
        try
        {
            var check = await Detect();
            string details = Strings.T("setupText") + "\n\n" + (architecture == "x86" ? @"HKLM\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM" : @"HKLM\SOFTWARE\Citrix\ICA Client\CCM") + "\n" + check.GetProperty("values").ToString();
            var dialog = new ContentDialog { XamlRoot = root.XamlRoot, Title = Strings.T(restore ? "restore" : "setup"), Content = new TextBlock { Text = details, TextWrapping = TextWrapping.Wrap }, PrimaryButtonText = Strings.T("continue"), CloseButtonText = Strings.T("cancel"), DefaultButton = ContentDialogButton.Close };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            using var process = Process.Start(new ProcessStartInfo(Worker, restore ? "restore" : "setup") { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden })!;
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new InvalidOperationException("Setup failed or was cancelled. No successful setup was confirmed.");
            if (!restore && !(await Run("check")).GetProperty("configured").GetBoolean()) throw new InvalidOperationException("Registry verification failed.");
            status.Text = Strings.T("done");
        }
        finally { busy = false; }
    }
    async Task Tick()
    {
        if (!running || busy || locked) return;
        if (prefs.Duration > 0 && DateTimeOffset.UtcNow - started >= TimeSpan.FromMinutes(prefs.Duration)) { enabled.IsOn = false; return; }
        SetThreadExecutionState(prefs.KeepAwake ? 0x80000001 : 0x80000000);
        if (DateTimeOffset.UtcNow < next) return;
        busy = true;
        try
        {
            await Run("pulse", target);
            if (running) status.Text = Strings.T("active") + "\n" + DateTime.Now.ToString("T");
            next = DateTimeOffset.UtcNow.AddSeconds(prefs.Interval);
        }
        catch (Exception ex) { enabled.IsOn = false; status.Text = Strings.T("error") + "\n" + ex.Message; }
        finally { busy = false; }
    }
    async Task Error(string message) => await new ContentDialog { XamlRoot = root.XamlRoot, Title = Strings.T("error"), Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, CloseButtonText = Strings.T("close") }.ShowAsync();
}
