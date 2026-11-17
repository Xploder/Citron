# Development status — v0.2.2

## Implemented

- Native WinUI 3 / .NET 10 UI, custom title bar, app icon, native Windows tray, double-click activity toggle.
- English, German, Spanish, French, Portuguese; initial Windows language and persisted manual selection.
- Separate, architecture-matched x86/x64 .NET Framework 4.7.2 ICA/CCM helpers, using Windows' included runtime.
- Optional randomized intervals: fresh ±10% variation per next attempt, disabled by default, persisted and translated in all five languages.
- Per-session selection, 12-second helper timeout, interval, duration, local power request, Windows lock pause.
- Paused startup, single instance with activation of the existing window, Explorer tray recreation handling.
- Guided registry setup with UAC, previous-value backup and restoration.
- Partial trimming, focused WinUI dependency, generated JSON settings, and compressed single-file distribution.

## Verified

- Portable output contains one executable of approximately 34 MiB, down from 433 MiB / 1,160 files.
- The randomized-interval checkbox was visually checked in the packaged EXE; enabled state persisted through quit/relaunch, then was restored to the fixed default.
- Timing tests pass: fixed mode does not consume RNG; randomized values stay within bounds, vary in both directions, and retain the configured average. Old settings remain fixed and the new preference survives JSON round trips.
- Both helper PE architectures run and report the expected process architecture.
- Registry checks pass; a helper with missing setup values refuses session access.
- Startup of the actual compressed EXE succeeds with bundled icons, XAML resources, and helper execution.
- On this machine, Citrix Workspace 26.10.0.12 x64 COM activation and session enumeration succeed; no connected sessions were returned during this test.
- Required registry values were already enabled when the compact version was checked. The packaging task did not change them.
- German UI renders; German-to-English switching and first-run setup cancellation were verified on the earlier prototype.
- Existing GitHub CLI access was refreshed through browser device authorization and user-completed two-factor authentication. Upload uses the task-specific CLI configuration outside the repository.

## Remaining acceptance tests

- Broader Citrix/Windows version coverage, longer-running sessions, and F15 effects in different remote applications.
- UAC setup approval, backup/restore round trip, and denied-UAC behavior.
- Multiple sessions, reconnect handling, hung COM calls, Windows lock/unlock, and Explorer restart recovery.
- Full translated UI and tray-interaction review on the compact build.

## Build notes

The linker reports IL2104 warnings in Microsoft's Windows SDK projection and WinRT runtime assemblies. These warnings are not suppressed. The packaged application's startup and registry/session-enumeration path were smoke-tested; this does not establish that every UI or WinRT path is trim-safe. Citrix helpers are not trimmed.

Commits use repository-local author metadata `ChatGPT <chatgpt@openai.com>`. This does not imply a verified GitHub identity. The repository is public.

## Local power requests in v0.2.2

The original local wake option only set ES_SYSTEM_REQUIRED; display timeout and Windows locking were allowed. Added a separate persisted, translated screen-on checkbox using ES_DISPLAY_REQUIRED together with ES_SYSTEM_REQUIRED. Requests apply immediately when the options change, only while active and unlocked. Windows API failures are reported in the status. Tests cover system-only/display flags and request release while paused, locked, or disabled. No screen capture or computer control was used for this update. The maintainer subsequently reported that the screen stays on and that the tool works with the Citrix session minimized. The exact laptop Windows/Citrix versions and test duration were not recorded; independent Windows lock policies and broader environments remain unverified.

## Public release confirmation — November 17, 2026

The maintainer reported successful use of the screen-on option on a laptop and confirmed that Citron works after testing the minimized Citrix session. v0.2.2 is promoted from preview to a regular release using the same executable and checksum. This report is specific to the tested environment and is not a universal compatibility guarantee.
