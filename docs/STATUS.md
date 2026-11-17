# Development status — v0.2.0

## Implemented

- Native WinUI 3 / .NET 10 UI, custom title bar, app icon, native Windows tray, double-click activity toggle.
- English, German, Spanish, French, Portuguese; initial Windows language and persisted manual selection.
- Separate, architecture-matched x86/x64 .NET Framework 4.7.2 ICA/CCM helpers, using Windows' included runtime.
- Per-session selection, 12-second helper timeout, interval, duration, local power request, Windows lock pause.
- Paused startup, single instance with activation of the existing window, Explorer tray recreation handling.
- Guided registry setup with UAC, previous-value backup and restoration.
- Partial trimming, focused WinUI dependency, generated JSON settings, and compressed single-file distribution.

## Verified

- Portable output contains one executable of approximately 34 MiB, down from 433 MiB / 1,160 files.
- Both helper PE architectures run and report the expected process architecture.
- Registry checks pass; a helper with missing setup values refuses session access.
- Startup of the actual compressed EXE succeeds with bundled icons, XAML resources, and helper execution.
- On this machine, Citrix Workspace 26.10.0.12 x64 COM activation and session enumeration succeed; no connected sessions were returned during this test.
- Required registry values were already enabled when the compact version was checked. The packaging task did not change them.
- German UI renders; German-to-English switching and first-run setup cancellation were verified on the earlier prototype.
- Existing GitHub CLI access was refreshed through browser device authorization and user-completed two-factor authentication. Upload uses the task-specific CLI configuration outside the repository.

## Remaining acceptance tests

- Connected-session input delivery, minimized/background operation, remote idle-timer reset, and F15 effects.
- UAC setup approval, backup/restore round trip, and denied-UAC behavior.
- Multiple sessions, reconnect handling, hung COM calls, Windows lock/unlock, and Explorer restart recovery.
- Full translated UI and tray-interaction review on the compact build.

## Build notes

The linker reports IL2104 warnings in Microsoft's Windows SDK projection and WinRT runtime assemblies. These warnings are not suppressed. The packaged application's startup and registry/session-enumeration path were smoke-tested; this does not establish that every UI or WinRT path is trim-safe. Citrix helpers are not trimmed.

Commits use repository-local author metadata `ChatGPT <chatgpt@openai.com>`. This does not imply a verified GitHub identity. The repository remains private.
