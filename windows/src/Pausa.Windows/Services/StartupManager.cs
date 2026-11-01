using Microsoft.Win32;

namespace Pausa.Windows.Services;

/// <summary>
/// Launch at sign-in via the per-user Run key, the same entry the installer's
/// "Start Pausa when I sign in" option writes. Respects the Task Manager / Settings toggle.
/// </summary>
internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "Pausa";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var run = Registry.CurrentUser.OpenSubKey(RunKey);
                if (run?.GetValue(ValueName) is not string) return false;

                // Task Manager's "Disable" leaves the Run value and flags it here (first byte 3).
                using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
                return approved?.GetValue(ValueName) is not byte[] { Length: > 0 } flags || flags[0] != 3;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var run = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                run.SetValue(ValueName, Command);
                using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
                approved?.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            else
            {
                run.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Group Policy can lock the Run key; the toggle simply reflects the real state afterwards.
        }
    }

    /// <summary>If the app was moved or updated to a new path, point the existing entry at this copy.</summary>
    public static void RefreshPathIfEnabled()
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (run?.GetValue(ValueName) is string current
                && !string.Equals(current, Command, StringComparison.OrdinalIgnoreCase))
            {
                run.SetValue(ValueName, Command);
            }
        }
        catch
        {
        }
    }
}
