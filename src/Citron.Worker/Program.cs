using Microsoft.Win32;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Security.Principal;

namespace Citron.Worker;

internal static class Program
{
    const string Ccm = @"SOFTWARE\Citrix\ICA Client\CCM";
    static readonly string[] Names = ["AllowLiveMonitoring", "AllowSimulationAPI"];
    static readonly Guid Ica = new("238F6F83-B8B4-11CF-8771-00A024541EE3");
    static RegistryView View => Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32;

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            string command = args.FirstOrDefault() ?? "scan";
            if (command is "setup" or "restore")
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                    throw new InvalidOperationException("Administrator rights are required.");
                Configure(command == "restore");
                return 0;
            }
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);
            using var ccm = hive.OpenSubKey(Ccm);
            bool configured = Names.All(n => ccm?.GetValue(n) is int i && i == 1);
            if (command == "check")
            {
                using var registration = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, View);
                using var clsid = registration.OpenSubKey($@"CLSID\{{{Ica}}}\InprocServer32");
                Emit(new { ok = true, configured, architecture = Environment.Is64BitProcess ? "x64" : "x86", component = clsid?.GetValue(null), path = @"HKLM\" + Ccm, values = Names.ToDictionary(n => n, n => ccm?.GetValue(n)) });
                return 0;
            }
            if (!configured) throw new InvalidOperationException("Citrix monitoring and simulation are not enabled. Run setup first.");
            object? clientObject = null;
            var sessions = new List<string>();
            try
            {
                clientObject = Activator.CreateInstance(Type.GetTypeFromCLSID(Ica, true)!);
                dynamic client = clientObject!;
                var handle = client.EnumerateCCMSessions();
                try
                {
                    int count = client.GetEnumNameCount(handle);
                    for (int i = 0; i < count; i++) sessions.Add((string)client.GetEnumNameByIndex(handle, i));
                }
                finally { client.CloseEnumHandle(handle); }
                if (command == "pulse")
                {
                    if (args.Length != 2) throw new ArgumentException("A target session is required.");
                    if (!sessions.Contains(args[1])) { Emit(new { ok = true, configured, sessions, sent = false, missing = true }); return 0; }
                    object? targetObject = null;
                    bool monitoring = false;
                    try
                    {
                        targetObject = Activator.CreateInstance(Type.GetTypeFromCLSID(Ica, true)!);
                        dynamic target = targetObject!;
                        target.SetProp("OutputMode", "1");
                        target.StartMonitoringCCMSession(args[1], true);
                        monitoring = true;
                        // Windows virtual-key F15. Citrix behavior must be verified in the target environment.
                        target.Session.Keyboard.SendKeyDown(126);
                        target.Session.Keyboard.SendKeyUp(126);
                    }
                    finally
                    {
                        if (monitoring) { try { ((dynamic)targetObject!).StopMonitoringCCMSession(args[1]); } catch { } }
                        Release(targetObject);
                    }
                }
                else if (command != "scan") throw new ArgumentException("Unknown command.");
                Emit(new { ok = true, configured, sessions, sent = command == "pulse" });
            }
            finally { Release(clientObject); }
            return 0;
        }
        catch (Exception ex) { Emit(new { ok = false, error = ex.Message, hresult = ex.HResult }); return 1; }
    }

    static void Configure(bool restore)
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);
        using var registration = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, View);
        using var clsid = registration.OpenSubKey($@"CLSID\{{{Ica}}}\InprocServer32");
        if (clsid?.GetValue(null) is not string dll || !File.Exists(dll.Trim('"')))
            throw new InvalidOperationException("No registered ICA component for this architecture.");
        using var key = hive.CreateSubKey(Ccm, true);
        using var backup = hive.CreateSubKey(@"SOFTWARE\Citron\RegistryBackup", true);
        foreach (string name in Names)
        {
            if (restore)
            {
                if (backup.GetValue(name + "Saved") is not int) continue;
                if (key.GetValue(name) is not int current || current != 1)
                    throw new InvalidOperationException("A setting changed after setup; restoration stopped to preserve it.");
                if (backup.GetValue(name + "Exists") is int exists && exists == 1)
                    key.SetValue(name, backup.GetValue(name)!, (RegistryValueKind)(int)backup.GetValue(name + "Kind")!);
                else key.DeleteValue(name, false);
                foreach (string suffix in new[] { "", "Saved", "Exists", "Kind" }) backup.DeleteValue(name + suffix, false);
            }
            else
            {
                if (key.GetValue(name) is int existing && existing == 1) continue;
                if (backup.GetValue(name + "Saved") == null)
                {
                    object? old = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    backup.SetValue(name + "Exists", old == null ? 0 : 1);
                    if (old != null) { backup.SetValue(name, old, key.GetValueKind(name)); backup.SetValue(name + "Kind", (int)key.GetValueKind(name)); }
                    backup.SetValue(name + "Saved", 1);
                }
                key.SetValue(name, 1, RegistryValueKind.DWord);
            }
        }
    }
    static void Release(object? value) { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
    static void Emit(object result) => Console.WriteLine(new JavaScriptSerializer().Serialize(result));
}
