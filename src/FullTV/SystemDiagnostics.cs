using System.Diagnostics;
using System.Management;
using System.Windows.Forms;

namespace FullTV;

/// <summary>Checks prerequisites. Never silently modifies hypervisor or Windows security settings.</summary>
internal static class SystemDiagnostics
{
    public static async Task<string> RunAsync(string adbPath)
    {
        var lines = new List<string>
        {
            "FullTV system diagnostics",
            "Windows: " + Environment.OSVersion.VersionString,
            "Architecture: " + System.Runtime.InteropServices.RuntimeInformation.OSArchitecture,
            "Processors: " + Environment.ProcessorCount,
            "Memory: " + (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024) + " GB (managed process estimate; not installed RAM)",
            "64-bit process: " + Environment.Is64BitProcess,
        };
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        lines.Add("Windows build: " + (key?.GetValue("CurrentBuild") ?? "Unknown"));
        lines.Add("Hypervisor requirement: Windows Hypervisor Platform feature + BIOS/UEFI virtualization for WHPX.");
        lines.Add("Bundled Android runtime: NOT INSTALLED. This preview requires an external authorized Android device or emulator.");
        try
        {
            var psi = new ProcessStartInfo(adbPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            psi.ArgumentList.Add("devices");
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Cannot start ADB");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync(cts.Token);
            lines.Add("ADB exit code: " + p.ExitCode);
            lines.Add((await stdout).Trim());
            var err = (await stderr).Trim();
            if (err.Length != 0) lines.Add("ADB messages: " + err);
        }
        catch (Exception ex) { lines.Add("ADB unavailable: " + ex.Message); }
        return string.Join(Environment.NewLine, lines);
    }

    public static async Task ShowAsync(IWin32Window owner, string adbPath)
    {
        var results = await RunAsync(adbPath);
        using var dialog = new Form { Text = "FullTV — Compatibility diagnostics", Width = 800, Height = 520, StartPosition = FormStartPosition.CenterParent };
        var output = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new System.Drawing.Font("Consolas", 10), Text = results };
        var copy = new Button { Text = "Copy diagnostics", Dock = DockStyle.Bottom, Height = 48 };
        copy.Click += (_, _) => { Clipboard.SetText(results); };
        dialog.Controls.Add(output); dialog.Controls.Add(copy);
        dialog.ShowDialog(owner);
    }
}
