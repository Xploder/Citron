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
