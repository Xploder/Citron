# Development status

## Implemented

- WinUI 3 settings window with custom title bar, app icon, tray icon states, and double-click toggle.
- English, German, Spanish, French, Portuguese; follows initial OS language, manual choice persists.
- Selected-session ICA/CCM backend in separate x86/x64 STA worker processes; 12-second UI timeout.
- Interval, duration, local power request, Windows lock pause, paused startup, single instance.
- Registry checks, first-run setup dialog, UAC setup helper, previous-value backup and restore.
- Self-contained Windows x64 distribution, including both helper architectures.

## Verified on the development machine

- Release compilation and publication of UI, x64 worker, and x86 worker.
- Read-only detection of Citrix Workspace 26.10.0.12, native x64 ICA COM registration.
- Both required CCM settings are missing. No registry setup has been performed by the developer.
- UI starts and renders in German; language picker exposes all five languages.
- Private GitHub repository created at https://github.com/Xploder/Citron.

## Acceptance tests still required

- UAC approval / cancellation, registry restoration, actual connected-session enumeration.
- Successful input delivery and idle-timer reset while minimized, with no previous click.
- Reconnect, multiple-session selection, remote application F15 side effects.
- Tray toggle/Explorer restart recovery and Windows lock/unlock behavior.
- Full visual review of all five translations.

## GitHub CLI issue

The browser and GitHub connector authenticate as Xploder. GitHub CLI's separate stored token returns HTTP 401 (Bad credentials). Native Git Credential Manager has no usable credential in the current execution context. Re-authenticate the CLI with `gh auth login -h github.com`, then verify `gh auth status`. Do not copy tokens from browser storage. Repository-local Git author is `ChatGPT <chatgpt@openai.com>`; this metadata does not guarantee association with an official GitHub profile. Source upload remains pending until authenticated Git push is available.
