using System;
using System.IO;
using Microsoft.Win32;
namespace Citron;
internal static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string Command(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || executable.Contains('"')) throw new ArgumentException("Invalid executable path.");
        return "\"" + Path.GetFullPath(executable) + "\" --tray";
    }
    internal static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("Citron") is string command && command == Command(Environment.ProcessPath!);
    }
    internal static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (enabled) key.SetValue("Citron", Command(Environment.ProcessPath!), RegistryValueKind.String);
        else key.DeleteValue("Citron", false);
    }
}
