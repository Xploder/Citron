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

namespace Citron;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log(e.ExceptionObject.ToString()!);
        using var single = new Mutex(true, "Local\\Citron.Desktop", out bool first);
        if (!first) { NativeTray.ShowExisting(); return; }
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ => { SynchronizationContext.SetSynchronizationContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread())); new CitronApp(); });
    }
    internal static void Log(string message)
    {
        try { string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Citron");
        Directory.CreateDirectory(path); File.AppendAllText(Path.Combine(path, "startup.log"), DateTime.Now + " " + message + Environment.NewLine); } catch { }
    }
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
    CheckBox awake = null!, displayOn = null!, randomizeInterval = null!, autoDiscover = null!;
    NativeTray tray = null!;
    DispatcherTimer timer = null!;
    Preferences prefs = new();
    readonly AutoSessionDiscovery discovery = new();
    bool backendReady;
    string architecture = "x64";
    bool running, busy, locked, exiting, setupNeeded;
    DateTimeOffset started, next, nextSessionCheck;
    string? target;
    static string PrefPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Citron", "settings.json");
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Program.Log("Launch");
        try { prefs = JsonSerializer.Deserialize(File.ReadAllText(PrefPath), JsonContext.Default.Preferences) ?? new(); } catch { }
        Strings.Language = prefs.Language;
        window = new Window { Title = "Citron", SystemBackdrop = new MicaBackdrop() };
        Program.Log("Window created");
        double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window)) / 96d;
        var area = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(window.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32(Math.Min((int)(600 * scale), area.Width), Math.Min((int)(740 * scale), area.Height - 32)));
        window.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Citron.ico"));
        window.ExtendsContentIntoTitleBar = true;
        window.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        window.AppWindow.Closing += (_, e) => { if (!exiting) { e.Cancel = true; window.AppWindow.Hide(); } };
        BuildUi();
        Program.Log("UI created");
        tray = new NativeTray(WinRT.Interop.WindowNative.GetWindowHandle(window), () => { tray?.Show(); window.Activate(); }, () => enabled.IsOn = !running, OnSessionLock, Quit);
        Program.Log("Tray created");
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += async (_, _) => await Tick();
        timer.Start();
        if (!Environment.GetCommandLineArgs().Contains("--tray")) window.Activate();
        Program.Log("Window activated");
        _ = Initialize();
    }
    async Task Initialize()
    {
        await Scan();
        if (!setupNeeded || prefs.SetupPrompted || Environment.GetCommandLineArgs().Contains("--tray")) return;
        prefs.SetupPrompted = true; Save();
        try { await Configure(false); } catch (Exception ex) { await Error(ex.Message); }
    }
    void OnSessionLock(bool isLocked)
    {
        locked = isLocked;
        if (locked) { SetThreadExecutionState(0x80000000); status.Text = Strings.T("locked"); }
        else { next = DateTimeOffset.UtcNow; discovery.RequestSearch(); }
    }
    void BuildUi()
    {
        root = new StackPanel { Spacing = 10, Margin = new Thickness(20, 8, 20, 20) };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        brand.Children.Add(new Image { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Citron.png")), Width = 32, Height = 32 });
        brand.Children.Add(new TextBlock { Text = "Citron", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }); root.Children.Add(brand);
        status = new TextBlock { Text = Strings.T("idle"), TextWrapping = TextWrapping.Wrap, FontSize = 14 };
        root.Children.Add(new Border { Padding = new Thickness(16), CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(28, 110, 180, 95)), Child = status });
        sessions = new ComboBox { Header = Strings.T("target"), HorizontalAlignment = HorizontalAlignment.Stretch };
        var sessionRow = new Grid { ColumnSpacing = 8 };
        sessionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sessionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sessionRow.Children.Add(sessions);
        Button refresh = Button("refresh", async () => await Scan()); refresh.VerticalAlignment = VerticalAlignment.Bottom; Grid.SetColumn(refresh, 1); sessionRow.Children.Add(refresh); root.Children.Add(sessionRow);
        enabled = new ToggleSwitch { Header = Strings.T("enable"), OnContent = "●", OffContent = "○" };
        enabled.Toggled += (_, _) =>
        {
            if (!enabled.IsOn) { discovery.Suppress(target); discovery.Schedule(DateTimeOffset.UtcNow); }
            running = enabled.IsOn && sessions.SelectedItem is string;
            if (busy && enabled.IsOn) { enabled.IsOn = false; return; }
            if (enabled.IsOn && !running) { enabled.IsOn = false; status.Text = Strings.T("none"); return; }
            target = running ? (string)sessions.SelectedItem : null;
            sessions.IsEnabled = !running;
            started = next = DateTimeOffset.UtcNow;
            nextSessionCheck = DateTimeOffset.UtcNow.Add(SessionPolicy.CheckInterval);
            ApplyWakeRequest();
            status.Text = Strings.T(running ? "active" : "idle");
            tray?.SetActive(running);
        };
        root.Children.Add(enabled);
        autoDiscover = new SettingsCheckBox { Content = Strings.T("autoDiscover"), IsChecked = prefs.AutoDiscoverSessions };
        void ChangeDiscovery() { Save(); discovery.Reset(); }
        autoDiscover.Checked += (_, _) => ChangeDiscovery(); autoDiscover.Unchecked += (_, _) => ChangeDiscovery();
        ToolTipService.SetToolTip(autoDiscover, Strings.T("autoDiscoverHint")); root.Children.Add(autoDiscover);
        interval = new NumberBox { Header = Strings.T("interval"), Minimum = 15, Maximum = 3600, Value = Math.Clamp(prefs.Interval, 15, 3600), SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        duration = new NumberBox { Header = Strings.T("duration"), Minimum = 0, Maximum = 1440, Value = Math.Clamp(prefs.Duration, 0, 1440), SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
        interval.ValueChanged += (_, _) => { Save(); Reschedule(); }; duration.ValueChanged += (_, _) => Save();
        var timing = new StackPanel { Spacing = 8 };
        var numbers = new Grid { ColumnSpacing = 12 };
        numbers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        numbers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        numbers.Children.Add(interval); Grid.SetColumn(duration, 1); numbers.Children.Add(duration); timing.Children.Add(numbers);
        randomizeInterval = new SettingsCheckBox { Content = Strings.T("randomize"), IsChecked = prefs.RandomizeInterval };
        randomizeInterval.Checked += (_, _) => { Save(); Reschedule(); };
        randomizeInterval.Unchecked += (_, _) => { Save(); Reschedule(); };
        timing.Children.Add(randomizeInterval);
        ToolTipService.SetToolTip(randomizeInterval, Strings.T("randomizeHint"));
        root.Children.Add(Section("timingSection", timing, true));
        var power = new StackPanel { Spacing = 8 };
        awake = new SettingsCheckBox { Content = Strings.T("sleep"), IsChecked = prefs.KeepAwake };
        displayOn = new SettingsCheckBox { Content = Strings.T("displayOn"), IsChecked = prefs.KeepDisplayOn, IsEnabled = prefs.KeepAwake };
        awake.Checked += (_, _) => { Save(); displayOn.IsEnabled = true; ApplyWakeRequest(); };
        awake.Unchecked += (_, _) => { Save(); displayOn.IsEnabled = false; ApplyWakeRequest(); }; power.Children.Add(awake);
        displayOn.Checked += (_, _) => { Save(); ApplyWakeRequest(); };
        displayOn.Unchecked += (_, _) => { Save(); ApplyWakeRequest(); };
        power.Children.Add(displayOn);
        power.Children.Add(new TextBlock { Text = Strings.T("wakeHint"), TextWrapping = TextWrapping.Wrap, Opacity = .65, FontSize = 12 });
        root.Children.Add(Section("powerSection", power));
        var settings = new StackPanel { Spacing = 10 };
        var language = new ComboBox { Header = Strings.T("language"), HorizontalAlignment = HorizontalAlignment.Stretch };
        string[] codes = ["en", "de", "es", "fr", "pt"];
        foreach (string label in new[] { "English", "Deutsch", "Español", "Français", "Português" }) language.Items.Add(label);
        language.SelectedIndex = Math.Max(0, Array.IndexOf(codes, Strings.Language));
        language.SelectionChanged += (_, _) => { enabled.IsOn = false; Strings.Language = codes[language.SelectedIndex]; Save(); BuildUi(); _ = Scan(); };
        settings.Children.Add(language);
        var startup = new SettingsCheckBox { Content = Strings.T("startup") };
        bool changingStartup = false;
        try { startup.IsChecked = StartupRegistration.IsEnabled(); } catch (Exception ex) { Program.Log(ex.Message); }
        void ChangeStartup()
        {
            if (changingStartup) return;
            bool desired = startup.IsChecked == true;
            try { StartupRegistration.SetEnabled(desired); }
            catch (Exception ex) { changingStartup = true; startup.IsChecked = !desired; changingStartup = false; status.Text = Strings.T("startupFailed") + "\n" + ex.Message; }
        }
        startup.Checked += (_, _) => ChangeStartup(); startup.Unchecked += (_, _) => ChangeStartup();
        ToolTipService.SetToolTip(startup, Strings.T("startupHint")); settings.Children.Add(startup);
        var actions = new StackPanel { Spacing = 8 };
        actions.Children.Add(Button("setup", () => Configure(false)));
        actions.Children.Add(Button("restore", () => Configure(true))); settings.Children.Add(actions);
        root.Children.Add(Section("settingsSection", settings));
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
        var button = new Button { Content = new TextBlock { Text = Strings.T(key), TextWrapping = TextWrapping.Wrap } };
        button.Click += async (_, _) => { button.IsEnabled = false; try { await action(); } catch (Exception ex) { await Error(ex.Message); } finally { button.IsEnabled = true; } };
        return button;
    }
    static Expander Section(string key, UIElement content, bool expanded = false) => new() { Header = Strings.T(key), Content = content, IsExpanded = expanded, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
    void Quit() { exiting = true; timer.Stop(); SetThreadExecutionState(0x80000000); tray.Dispose(); window.Close(); Exit(); }
    void Save()
    {
        prefs.Language = Strings.Language;
        if (double.IsFinite(interval.Value)) prefs.Interval = (int)Math.Clamp(interval.Value, 15, 3600);
        if (double.IsFinite(duration.Value)) prefs.Duration = (int)Math.Clamp(duration.Value, 0, 1440);
        prefs.KeepAwake = awake?.IsChecked == true;
        prefs.KeepDisplayOn = displayOn?.IsChecked == true;
        prefs.RandomizeInterval = randomizeInterval?.IsChecked == true;
        prefs.AutoDiscoverSessions = autoDiscover?.IsChecked == true;
        try { Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!); File.WriteAllText(PrefPath, JsonSerializer.Serialize(prefs, JsonContext.Default.Preferences)); } catch (Exception ex) { status.Text = ex.Message; }
    }
    void ApplyWakeRequest()
    {
        uint flags = LocalWakePolicy.Flags(running, locked, prefs.KeepAwake, prefs.KeepDisplayOn);
        if (SetThreadExecutionState(flags) == 0 && flags != 0x80000000)
            status.Text = Strings.T("wakeFailed");
    }
    void Reschedule()
    {
        if (running && !busy) next = DateTimeOffset.UtcNow.Add(ActivityTiming.NextDelay(prefs.Interval, prefs.RandomizeInterval));
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
        string? candidate = null;
        discovery.Schedule(DateTimeOffset.UtcNow);
        try
        {
            sessions.Items.Clear();
            if (!backendReady)
            {
                var check = await Detect();
                Program.Log("Component detected");
                setupNeeded = !check.GetProperty("configured").GetBoolean();
                if (setupNeeded) { status.Text = Strings.T("setupNeeded"); return; }
                backendReady = true;
            }
            var result = await Run("scan");
            var found = result.GetProperty("sessions").EnumerateArray().Select(s => s.GetString()!).ToArray();
            foreach (string s in found) sessions.Items.Add(s);
            candidate = discovery.SelectTarget(found);
            if (sessions.Items.Count > 0) { sessions.SelectedIndex = 0; status.Text = Strings.T("idle"); }
            else status.Text = Strings.T(prefs.AutoDiscoverSessions ? "autoWaiting" : "none");
        }
        catch (Exception ex) { backendReady = false; status.Text = Strings.T("error") + "\n" + ex.Message; }
        finally { busy = false; }
        if (prefs.AutoDiscoverSessions && !running && !locked && candidate != null && !exiting)
        {
            sessions.SelectedItem = candidate;
            enabled.IsOn = true;
        }
    }
    async Task Configure(bool restore)
    {
        if (busy) return;
        enabled.IsOn = false; busy = true; backendReady = false;
        try
        {
            var check = await Detect();
            string details = Strings.T(restore ? "restoreText" : "setupText") + "\n\n" + (architecture == "x86" ? @"HKLM\SOFTWARE\WOW6432Node\Citrix\ICA Client\CCM" : @"HKLM\SOFTWARE\Citrix\ICA Client\CCM") + "\n" + check.GetProperty("values").ToString();
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
        if (!running)
        {
            if (discovery.IsDue(prefs.AutoDiscoverSessions, running, busy, locked, DateTimeOffset.UtcNow)) await Scan();
            return;
        }
        if (!running || busy) return;
        if (prefs.Duration > 0 && DateTimeOffset.UtcNow - started >= TimeSpan.FromMinutes(prefs.Duration)) { enabled.IsOn = false; return; }
        ApplyWakeRequest();
        bool pulse = !locked && DateTimeOffset.UtcNow >= next;
        if (!pulse && DateTimeOffset.UtcNow < nextSessionCheck) return;
        busy = true;
        try
        {
            var pulseStarted = DateTimeOffset.UtcNow;
            string? checkedTarget = target;
            var result = await Run(pulse ? "pulse" : "scan", pulse ? checkedTarget : null);
            if (!running || target != checkedTarget) return;
            string[] found = result.GetProperty("sessions").EnumerateArray().Select(s => s.GetString()!).ToArray();
            sessions.Items.Clear(); foreach (string session in found) sessions.Items.Add(session);
            nextSessionCheck = DateTimeOffset.UtcNow.Add(SessionPolicy.CheckInterval);
            if (!SessionPolicy.TargetExists(checkedTarget, found))
            {
                discovery.SelectTarget(found); // Remove suppression for sessions that disappeared.
                enabled.IsOn = false; sessions.SelectedIndex = -1; status.Text = Strings.T("sessionEnded"); return;
            }
            sessions.SelectedItem = checkedTarget;
            if (pulse) { status.Text = Strings.T("active") + "\n" + DateTime.Now.ToString("T"); next = pulseStarted.Add(ActivityTiming.NextDelay(prefs.Interval, prefs.RandomizeInterval)); }
        }
        catch (Exception ex) { enabled.IsOn = false; sessions.Items.Clear(); status.Text = Strings.T("error") + "\n" + ex.Message; }
        finally { busy = false; }
    }
    async Task Error(string message) => await new ContentDialog { XamlRoot = root.XamlRoot, Title = Strings.T("error"), Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, CloseButtonText = Strings.T("close") }.ShowAsync();
}
