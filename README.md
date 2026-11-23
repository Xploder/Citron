<div align="center">

<img src="docs/icon.png" width="96" alt="Citron lemon icon" />

# 🍋 Citron

### A little energy for your workspace.

A compact, multilingual Windows tray companion for Citrix sessions.

**WinUI 3 · One EXE · 5 languages · Windows x64 · Public release**

[Download v0.4.1](https://github.com/Xploder/Citron/releases/tag/v0.4.1) · [Build from source](#-build-it-yourself) · [Validation](docs/VALIDATION.md)

<img src="docs/citron-v0.3.0.png" width="420" alt="Citron settings with session selection, activity controls, timing, and language options" />

</div>

---

## ✨ Small app. Focused purpose.

Citron is a native Windows utility designed to send periodic input directly to a selected Citrix session through the ICA Client Object / CCM interface. It does not use local keyboard injection or switch the foreground window to Citrix.

**The maintainer reported a successful live test with a minimized Citrix session on November 17, 2026.** The separate screen-on option was also reported to work on the tested laptop. This validates that environment, not every Citrix deployment: client versions, installation variants, remote applications, and server policies can affect compatibility. The laptop Windows/Citrix versions and exact test duration were not recorded.

## 🧩 Features

| | What you get |
| --- | --- |
| 🎯 Session selection | Choose one detected Citrix session rather than broadcasting to every session |
| 🍋 System tray | Single-click to enable/pause; double-click to open; right-click for quick actions |
| ⏱️ Timing | Adjustable interval and optional duration limit |
| 🖥️ Local wake option | Optionally prevent automatic sleep on this PC without local key presses |
| 🔐 Guided setup | Review the required registry values, approve UAC, and restore previous settings later |
| 🌍 Five languages | English, German, Spanish, French, and Portuguese |
| 🪟 Native UI | WinUI 3, custom title bar, Mica backdrop, and app/tray icons |
| 🧯 Process isolation | A stuck Citrix call times out after 12 seconds instead of freezing the UI |
| 📦 Portable download | One compressed executable with its runtime, assets, notices, and both helper architectures bundled |

## 🚀 Getting started

1. Download **Citron.exe** from [Releases](https://github.com/Xploder/Citron/releases/tag/v0.4.1).
2. Run it from a folder you can keep permanently. No separate .NET 10 or Windows App Runtime installation is needed.
3. On first launch, review **Set up Citrix access**. Approve the Windows UAC prompt if setup is required.
4. Connect a session using the native Citrix Workspace client, then select **Find sessions**.
5. Select the target, choose an interval/duration, and turn on **Keep session active**.

Citron starts paused unless the optional automatic session discovery is enabled and finds an eligible session. Closing the settings window keeps it in the tray; **Quit** exits completely. A second launch brings the existing instance forward.

### Requirements

- Windows x64, Windows 10 version 1809 or later / Windows 11.
- Native Citrix Workspace with a registered ICA Client Object component. HTML5/browser-only sessions and the Microsoft Store SDK variant are not supported by this backend.
- .NET Framework 4.7.2 or newer for the tiny Citrix helpers. This is part of the supported Windows baseline; see [Microsoft's version table](https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies).
- Administrator approval only when changing the machine-wide Citrix setup values. The ordinary UI and simulation helpers run unelevated.

A single left-click waits for the Windows double-click interval before toggling, so a double-click opens the window without also changing activity. Pending actions are cancelled on context menu, keyboard activation, quitting, or tray recreation.

## Automatic tray mode (v0.4.0)

Enable **Automatically find and activate Citrix sessions** to search every **30 seconds** while no session is being kept active. This option is off by default and is saved across restarts. Combine it with **Start with Windows** to let Citron wait in the tray after sign-in. If enabled, the automatic option can activate an already-connected session on startup; otherwise startup remains paused.

The first eligible session is selected in ordinal session-ID order. Automatic activation uses the same toggle as manual activation, so interval, randomized timing, duration, local PC wake, display wake, and tray status all apply normally. The 30-second search is suspended during activity; the existing 15-second session-presence watcher takes over. After the session disappears, the app stops activity/releases its power requests and returns to discovery. No automatic activation occurs while Windows is locked.

Manually paused sessions, sessions whose duration expired, and sessions paused after an activity error are excluded from automatic activation until they disappear from a successful session snapshot. New session IDs remain eligible. Turn the automatic option off and on to reset those exclusions. Turning it off stops future discovery, without interrupting activity already running.

Searches run serially with the existing 12-second per-helper timeout. The normal waiting state performs at most two session scans per minute, plus the initial check; component detection is cached after success and retried on failure. No extra discovery workers run while active. This is throttled polling, not a zero-CPU guarantee.

Settings checkboxes now use explicit two-state values and a static checkmark over the native control template, avoiding the AnimatedIcon mark used by WinUI during initial rendering. This addresses the reported initial minus/dash appearance; cold-start, theme, and hover rendering still need visual confirmation on the affected device. No screen recording or new GitHub screenshot was used for this update.

## New in v0.3.0

- **Start with Windows** is off by default. Under **Settings and Citrix setup**, enable per-user startup using the real portable EXE path. It starts paused in the tray after sign-in. No administrator rights are needed. If you move the EXE, toggle the option off/on to update its path.
- While active, Citron checks session presence every **15 seconds**, reusing normal activity enumeration. A missing target automatically pauses activity, removes the stale selection, and releases local power requests. Failed checks also pause activity and clear the list. Slow checks may take up to the 12-second deadline. Paused Citron only searches when optional auto-discovery is enabled; workers never overlap.
- A local read-only scan measured approximately **402 ms elapsed / 297 ms CPU**. Costs vary by machine and Citrix client.
- The smaller window groups controls into expandable **Timing**, **PC and display**, and **Settings and Citrix setup** sections, with interval/duration side by side. The screenshot above shows the compact v0.3.0 layout in English.
- **Restore Citrix registry backup from before Citron setup** restores the backed-up registry values, not interval/language or other app preferences. The confirmation explains this distinction.

## ⚙️ What the settings do

- **Interval:** time between attempts, from 15 to 3,600 seconds.
- **Vary intervals randomly (±10%):** optional and off by default. Each next gap gets a fresh random value; 60 seconds becomes approximately 54–66 seconds. The configured base interval stays unchanged. Turning this off uses the configured interval without intentional variation. Windows scheduling and the one-second polling timer can still introduce small timing delays. This option makes no claim about monitoring-tool classification.
- **Duration:** automatically pause after the selected number of minutes; 0 means unlimited.
- **Prevent automatic PC sleep while active:** prevents automatic local system sleep while Citron is active.
- **Also keep the screen on:** additionally requests that the display remain on. Requires the PC-sleep option and active Citron. This does not override Windows locking policies, manual sleep, or lid-close actions. No local keyboard/mouse input is generated.
- **Language:** follows the Windows display language initially, falls back to English, and saves your manual selection immediately.

Windows locking pauses simulation and releases the local power request. Unlocking resumes only if the app remains enabled and the duration has not expired. Disconnects or failed calls pause activity with an error.

## 🔐 Transparent first-run setup

Citron checks `AllowLiveMonitoring` and `AllowSimulationAPI` under:

```text
HKLM\SOFTWARE\Citrix\ICA Client\CCM
```

The registry view matches the detected Citrix COM component. On x64 Windows, the 32-bit view appears under `WOW6432Node`.

Setup displays current values, requests UAC approval, saves previous values under `HKLM\SOFTWARE\Citron\RegistryBackup` in the same view, and sets the two DWORD values to `1`. Cancelling the review dialog makes no changes. Restoration preserves externally changed settings rather than overwriting them. Citrix updates may reset the values; refresh checks them again. Citron does not automatically terminate Workspace to apply changes.

## 📦 Why the download is smaller

The original prototype shipped **433 MiB across 1,160 files**. This release bundles the application into a single executable of approximately **34 MiB**, around **92% smaller** by download size.

The build uses the focused WinUI NuGet package, partial .NET trimming, native tray integration instead of Windows Forms, source-generated settings serialization, and single-file compression. The architecture-specific helpers use Windows' existing .NET Framework, avoiding two additional runtime distributions.

On launch, bundled runtime files, helper executables, assets, and dependency notices are extracted under Windows' `.net` temporary cache and reused. **One downloadable EXE does not mean zero extracted files or a 34 MiB installed footprint.** Old-version caches can consume additional disk space. User settings and diagnostic startup logs are stored in `%LOCALAPPDATA%\Citron`.

## 🛠️ Build it yourself

Install the **.NET 10 SDK** on Windows x64. Build tools and .NET Framework reference assemblies are restored through NuGet.

```powershell
.\build.ps1
.\verify.ps1
dotnet run --project .\tests\Citron.Timing.Tests -c Release
```

The portable output is `dist\portable\Citron.exe`. The build verifies that this directory contains exactly one file. Both x86 and x64 helpers are embedded automatically. Use a fresh output directory when switching packaging formats.

Optional build parameters:

```powershell
.\build.ps1 -DotnetPath 'C:\path\to\dotnet.exe' -OutputDirectory '.\dist\custom'
# Restricted build hosts that cannot create MSBuild task-host pipes:
.\build.ps1 -InProcessTasks
```

`verify.ps1` checks helper architectures, registry reads, refusal of unconfigured session access, and the one-file distribution. It never sets registry values or sends keys. See the [acceptance protocol](docs/VALIDATION.md) for live Citrix and tray checks.

## 🔬 Compatibility and limitations

Citron sends F15 key-down/key-up through the selected Citrix session object. F15 is not universally side-effect-free: remote applications, including terminals, can react to it. This does not unlock a locked desktop, reconnect a disconnected session, or override an absolute server session-duration policy.

The ICA/CCM approach was identified from [Andy Morgan's CaffeineForCitrixWorkspace](https://github.com/andyjmorgan/CaffeineForCitrixWorkspace). No binaries or source files from that repository are bundled. The native tray implementation is adapted from our Luma project.

## 🤖 Built with Codex

Citron is an AI-assisted project built with ChatGPT/Codex and reviewed through real Windows builds and UI checks. Commits created for this project use `ChatGPT <chatgpt@openai.com>` as author metadata; this is not a verified GitHub identity or an endorsement by OpenAI.

Development, GitHub documentation, and release notes are in English. The application itself is multilingual. Citron is publicly available.

Dependency license texts and notices are in [docs/licenses](docs/licenses) and are embedded in the executable.
