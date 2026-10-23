using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FullTV;

internal static class AndroidRuntime
{
    private static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FullTV");
    private static readonly string Sdk = Path.Combine(Root, "AndroidSdk");
    private static readonly string Avds = Path.Combine(Root, "Avds");
    private static readonly string Emulator = Path.Combine(Sdk, "emulator", "emulator.exe");
    private const string Avd = "FullTV_AndroidTV";
    private static Process? process;
    public static bool IsInstalled => File.Exists(Emulator) && File.Exists(Path.Combine(Avds, Avd + ".ini"));
    public static bool IsRunning => process is { HasExited: false };

    public static void ShowManager(IWin32Window owner)
    {
        using var form = new Form { Text = "FullTV — Android TV", StartPosition = FormStartPosition.CenterParent, Size = new Size(650, 365), BackColor = Color.FromArgb(15, 18, 32), ForeColor = Color.White };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(24), AutoScroll = true };
        var title = new Label { Text = "Android TV", Font = new Font("Segoe UI", 22, FontStyle.Bold), Width = 550, Height = 55 };
        var message = new Label { Text = IsInstalled ? "Android TV is installed. Start your television below." : "Set up Android TV using Google's official Android Emulator. The first installation downloads several gigabytes and may require a Java JDK, virtualisation and acceptance of Google SDK licences.", Width = 550, Height = 92 };
        var action = new Button { Text = IsInstalled ? "Start Android TV" : "Install Android TV", Width = 550, Height = 54, BackColor = Color.FromArgb(116, 109, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        action.Click += (_, _) =>
        {
            try
            {
                if (IsInstalled) { Start(); form.Close(); }
                else { Setup(); form.Close(); }
            }
            catch (Exception ex) { MessageBox.Show(form, ex.Message, "FullTV Android TV"); }
        };
        panel.Controls.Add(title); panel.Controls.Add(message); panel.Controls.Add(action);
        form.Controls.Add(panel); form.ShowDialog(owner);
    }

    public static bool TryStartInstalled(out string error)
    {
        error = "";
        if (!IsInstalled) return false;
        try { Start(); return true; }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    private static void Start()
    {
        if (IsRunning) { process?.WaitForInputIdle(1500); return; }
        if (!IsInstalled) throw new InvalidOperationException("Android TV has not been installed.");
        var start = new ProcessStartInfo(Emulator) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(Emulator)! };
        start.Environment["ANDROID_HOME"] = Sdk;
        start.Environment["ANDROID_SDK_ROOT"] = Sdk;
        start.Environment["ANDROID_AVD_HOME"] = Avds;
        foreach (var arg in new[] { "-avd", Avd, "-gpu", "auto", "-no-snapshot-save" }) start.ArgumentList.Add(arg);
        process = Process.Start(start) ?? throw new InvalidOperationException("Android emulator did not start.");
    }

    private static void Setup()
    {
        var script = Path.Combine(AppContext.BaseDirectory, "tools", "setup-android-tv.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException("Android TV setup script not found in application package.", script);
        if (MessageBox.Show("Download Google's Android tools and an Android TV image? SDK licence acceptance may be required. Installation can use several gigabytes.", "Install Android TV", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = true };
        foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-NoExit", "-File", script }) start.ArgumentList.Add(arg);
        Process.Start(start);
    }
}
