# Validation protocol

1. Build both workers and the UI. Run `.build/workers/x64/Citron.Worker.exe check` and the x86 equivalent: these are read-only and must report registry values without activating a session.
2. Open Citron. Confirm the Windows language / English fallback and all five manual language choices.
3. Inspect setup's exact registry view and current values. Cancel: no writes. Approve UAC: only the two documented values plus backup are written. Verify second setup preserves the original backup. Verify restore. Declining UAC must leave the UI usable.
4. Connect a disposable Citrix session. Refresh, select it, activate. Test with the window focused, covered, minimized, and never clicked after connection. Work in a local editor during every case: no local characters or focus changes may occur.
5. Measure remote idle time if possible and exceed the known idle timeout at least twice. API return values alone do not pass this test. Test F15 in terminal emulators and the actual remote applications.
6. Disconnect/reconnect: stale targets must pause with an error. Test multiple sessions and confirm only the selected session receives input.
7. Lock Windows: no new simulation; optional local power request released. Unlock: resume only while enabled. Verify duration expiry and tray exit release requests.
8. Exercise a hung worker: UI stays responsive; timeout pauses operation. Restart Explorer: verify tray recovery.

Do not label minimized-session compatibility as verified until steps 4–5 pass on the exact deployed Citrix version. Setup/restore and live simulation require an interactive environment and are not performed by the build script.

## Optional randomized intervals

Run `dotnet run --project tests/Citron.Timing.Tests -c Release`. It checks fixed mode without RNG use, ±10% boundaries, variation on successive draws, average interval, invalid saved values, old-settings compatibility, and JSON persistence. In the UI, verify that the checkbox is initially off, saves across restarts, and survives language changes. Changing it while active reschedules the next attempt. The first attempt after activation is still immediate. Live delivery timing depends on Windows scheduling and the one-second polling timer. No monitoring-classification claim is made.

## Automatic discovery and checkbox rendering (v0.4.0)

With auto-discovery disabled, verify that no periodic idle searches occur. Enable it, leave no session connected, and open Citrix while Citron stays in the tray: within the 30-second cadence plus worker latency, verify the selected session, activity toggle, tray state and configured PC/display options activate together. Confirm no discovery work during activity and normal watcher cleanup on termination, followed by another discovery cycle. Test multiple sessions, manual pause, duration expiry, transient scan failures, locked Windows, and explicitly resetting the option off/on. Verify that exclusions only clear after disappearance or explicit reset. Unit tests cover these decision rules and JSON persistence, but cannot prove end-to-end Citrix delivery.

Cold-start with each settings checkbox both enabled and disabled. Open collapsed sections without hovering any checkbox: verify checkmarks are present immediately, empty boxes stay empty, keyboard/space toggling works, disabled controls remain disabled, and light/dark/high-contrast themes keep adequate colors. The workaround retains the native CheckBox template and semantics while replacing its animated mark. No screenshot or screen capture was used to validate this change.
