# Citron

A multilingual Windows tray utility for keeping a selected Citrix session active through the ICA Client Object / CCM simulation interface.

**Status: experimental.** API success is not proof of remote input delivery or idle-timer reset. Minimized-session compatibility must be measured with your exact Workspace version. This app does not prevent absolute server session limits, reconnect disconnected sessions, or unlock Windows.

## Build and run

Windows x64, .NET 8 SDK, and NuGet access are required. Run `./build.ps1`, then `dist/Citron/Citron.App.exe`. Both self-contained x86 and x64 workers are included. Citrix is detected through COM registration, not a hardcoded installation path. The Microsoft Store / HTML5 client is not supported by this backend.

Select a session, set an interval and optional duration, then enable. Double-click the tray icon to toggle. Closing the window keeps the app running; use Quit in the tray menu to exit. Activation starts paused. Inputs are sent only to the selected session, never through local SendInput or foreground switching. F15 can still have effects inside remote applications. The current backend sends balanced key-down/key-up events.

Languages: English, German, Spanish, French, Portuguese. Windows language is used initially, with English fallback. GitHub documentation and development language are English.

## First-run setup

The app checks `AllowLiveMonitoring` and `AllowSimulationAPI` under `HKLM\SOFTWARE\Citrix\ICA Client\CCM` in the detected component's registry view. On x64 Windows, the 32-bit view appears under WOW6432Node. Setup displays current values and requests Windows UAC approval before writing DWORD 1. The normal app and simulation workers remain unelevated. Previous values are saved under `HKLM\SOFTWARE\Citron\RegistryBackup` in the same registry view. Restore preserves values changed externally after setup. Updates may reset Citrix settings; refresh rechecks them. No active Workspace session is terminated by setup.

## Architecture

WinUI 3 / .NET 8 desktop UI and native Windows tray integration. A fresh, architecture-matched STA helper performs each COM operation. The UI enforces a 12-second deadline and kills a stuck helper; COM calls cannot freeze the UI. Enumeration handles and COM roots are released. Windows lock pauses simulation and releases the optional local power request. Settings are stored per user under LocalAppData/Citron.

The ICA/CCM approach was identified by studying [Andy Morgan's CaffeineForCitrixWorkspace](https://github.com/andyjmorgan/CaffeineForCitrixWorkspace). This is a new implementation; no source files or binaries from that project are bundled.

See [validation](docs/VALIDATION.md) for acceptance criteria. Repository visibility must remain private until explicitly changed by the owner.
