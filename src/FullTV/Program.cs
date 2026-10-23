using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace FullTV;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TvForm());
    }
}

internal sealed class TvForm : Form
{
    private const string Repo = "https://github.com/bhxuk/FullTV";
    private const string Api = "https://api.github.com/repos/bhxuk/FullTV/releases/latest";
    private readonly FlowLayoutPanel apps = new();
    private readonly Label status = new();
    private readonly Label headline = new();
    private readonly List<(string Name, string Package)> installed = new();
    private readonly HttpClient http = new();
    private readonly string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FullTV");
    private string adbPath = "adb";
    private Color accent = Color.FromArgb(121, 116, 248);
    private bool connected;
    private bool tvFullscreen;
    private readonly GamepadNavigation gamepad;
    private readonly List<Button> focusable = new();

    public TvForm()
    {
        Text = "FullTV — Your screen. Your world.";
        MinimumSize = new Size(960, 620);
        Size = new Size(1320, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 15, 26);
        ForeColor = Color.White;
        KeyPreview = true;
        DoubleBuffered = true;
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FullTV", Application.ProductVersion.Split('+')[0]));
        Directory.CreateDirectory(settingsDir);
        LoadPreferences();
        var sdkAdb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FullTV", "AndroidSdk", "platform-tools", "adb.exe");
        if (adbPath == "adb" && File.Exists(sdkAdb)) adbPath = sdkAdb;
        BuildUi();
        KeyDown += HandleTvKeys;
        gamepad = new GamepadNavigation(HandleControllerKey);
        FormClosed += (_, _) => gamepad.Dispose();
        Shown += async (_, _) => { if (AndroidRuntime.IsInstalled) AndroidRuntime.TryStartInstalled(out _); await RefreshAppsAsync(); };
    }

    private Button Button(string label, EventHandler action, bool highlight = false)
    {
        var b = new Button
        {
            Text = label, Width = 158, Height = 46, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = highlight ? accent : Color.FromArgb(35, 39, 57),
            ForeColor = Color.White, Margin = new Padding(0, 0, 12, 12),
            Cursor = Cursors.Hand, TabStop = true
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += action;
        focusable.Add(b);
        return b;
    }

    private void BuildUi()
    {
        Controls.Clear();
        focusable.Clear();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(42, 30, 42, 24) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        header.Controls.Add(new Label { Text = "◈  FullTV", Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true, ForeColor = Color.White }, 0, 0);
        var version = new LinkLabel { Text = $"v{Application.ProductVersion.Split('+')[0]}  ·  GitHub", LinkColor = Color.FromArgb(184, 187, 215), TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
        version.Click += (_, _) => Open(Repo);
        header.Controls.Add(version, 1, 0);
        root.Controls.Add(header, 0, 0);

        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 29, 53), Padding = new Padding(28, 24, 25, 16) };
        headline.Text = "Welcome to your new TV.";
        headline.Font = new Font("Segoe UI", 28, FontStyle.Bold);
        headline.ForeColor = Color.White;
        headline.Dock = DockStyle.Top;
        headline.Height = 68;
        hero.Controls.Add(headline);
        var subtitle = new Label { Text = "Your apps. Your media. Your living room.\nAndroid compatibility requires a configured ADB device or guest.", Dock = DockStyle.Bottom, Height = 64, ForeColor = Color.FromArgb(193, 196, 218), Font = new Font("Segoe UI", 12) };
        hero.Controls.Add(subtitle);
        root.Controls.Add(hero, 0, 1);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        nav.Controls.Add(Button("▣ Android TV", (_, _) => AndroidRuntime.ShowManager(this), true));
        nav.Controls.Add(Button("＋ Install APK", async (_, _) => await InstallApkAsync(), true));
        nav.Controls.Add(Button("↻ Refresh apps", async (_, _) => await RefreshAppsAsync()));
        nav.Controls.Add(Button("◉ Connect ADB", async (_, _) => await ConnectAsync()));
        nav.Controls.Add(Button("⬇ Check updates", async (_, _) => await UpdateAsync()));
        nav.Controls.Add(Button("⚙ Preferences", (_, _) => Preferences()));
        nav.Controls.Add(Button("◈ Diagnostics", async (_, _) => await SystemDiagnostics.ShowAsync(this, adbPath)));
        nav.Controls.Add(Button("▶ Plex", (_, _) => Open("https://app.plex.tv/desktop/")));
        nav.Controls.Add(Button("⛶ Fullscreen", (_, _) => ToggleFullscreen()));
        root.Controls.Add(nav, 0, 2);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        bottom.Controls.Add(new Label { Text = "YOUR APPLICATIONS", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(215, 217, 232) }, 0, 0);
        apps.Dock = DockStyle.Fill;
        apps.AutoScroll = true;
        apps.WrapContents = true;
        bottom.Controls.Add(apps, 0, 1);
        status.Dock = DockStyle.Fill;
        status.ForeColor = Color.FromArgb(155, 162, 188);
        status.Text = "FullTV desktop preview · Use Connect ADB to attach an Android device/runtime.";
        bottom.Controls.Add(status, 0, 2);
        root.Controls.Add(bottom, 0, 3);
        RenderApps();
    }

    private void RenderApps()
    {
        apps.Controls.Clear();
        if (installed.Count == 0)
        {
            apps.Controls.Add(new Label { Text = "No Android apps discovered. Connect a device or runtime, then refresh.", AutoSize = true, ForeColor = Color.FromArgb(164, 170, 192), Margin = new Padding(6, 30, 0, 0), Font = new Font("Segoe UI", 12) });
            return;
        }
        foreach (var item in installed)
        {
            var package = item.Package;
            var button = Button(item.Name.Length > 19 ? item.Name[..19] + "…" : item.Name, async (_, _) => await LaunchAsync(package));
            button.Size = new Size(205, 110);
            button.Margin = new Padding(4, 6, 18, 16);
            var menu = new ContextMenuStrip();
            menu.Items.Add("Launch", null, async (_, _) => await LaunchAsync(package));
            menu.Items.Add("Uninstall…", null, async (_, _) => await UninstallAsync(package));
            button.ContextMenuStrip = menu;
            apps.Controls.Add(button);
        }
    }

    private async Task<(int Code, string Output)> RunAdbAsync(params string[] args)
    {
        var info = new ProcessStartInfo(adbPath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in args) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("ADB could not start.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(timeout.Token);
        return (process.ExitCode, (await stdout) + "\n" + (await stderr));
    }

    private async Task RefreshAppsAsync()
    {
        status.Text = "Checking Android connection…";
        try
        {
            var check = await RunAdbAsync("devices");
            connected = check.Output.Split('\n').Any(x => x.TrimEnd().EndsWith("\tdevice"));
            installed.Clear();
            if (connected)
            {
                var result = await RunAdbAsync("shell", "pm", "list", "packages", "-3");
                if (result.Code == 0)
                    foreach (var line in result.Output.Split('\n'))
                    {
                        var package = line.Trim();
                        if (package.StartsWith("package:"))
                        {
                            package = package["package:".Length..];
                            installed.Add((package.Split('.').LastOrDefault() ?? package, package));
                        }
                    }
                status.Text = $"Connected · {installed.Count} third-party apps found. Right-click an app to uninstall.";
            }
            else status.Text = "No Android device connected. Install Android platform-tools and connect using ADB.";
        }
        catch (Exception ex) { connected = false; status.Text = "ADB unavailable: " + ex.Message; }
        RenderApps();
    }

    private async Task ConnectAsync()
    {
        var address = Microsoft.VisualBasic.Interaction.InputBox("Enter Android ADB address (IP:port). Leave blank to refresh locally connected devices.", "Connect Android", "");
        try
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(address, @"^[a-zA-Z0-9.:-]+$")) throw new ArgumentException("Invalid address.");
                var result = await RunAdbAsync("connect", address);
                status.Text = result.Output.Trim();
            }
            await RefreshAppsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "ADB connection"); }
    }

    private async Task InstallApkAsync()
    {
        if (!connected) { MessageBox.Show("Connect Android through ADB first.", "Android not connected"); return; }
        using var dialog = new OpenFileDialog { Title = "Install Android APK", Filter = "Android package (*.apk)|*.apk" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        if (MessageBox.Show($"Install {Path.GetFileName(dialog.FileName)}? Only install APKs from trusted sources.", "Confirm installation", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            status.Text = "Installing APK…";
            var result = await RunAdbAsync("install", "-r", dialog.FileName);
            MessageBox.Show(result.Output.Trim(), result.Code == 0 ? "Installation result" : "Installation failed");
            await RefreshAppsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "APK installation"); }
    }

    private async Task LaunchAsync(string package)
    {
        try { var result = await RunAdbAsync("shell", "monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1"); status.Text = result.Code == 0 ? "Launching " + package : result.Output.Trim(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Launch failed"); }
    }

    private async Task UninstallAsync(string package)
    {
        if (MessageBox.Show($"Uninstall {package} and remove its app data?", "Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { var result = await RunAdbAsync("uninstall", package); MessageBox.Show(result.Output.Trim()); await RefreshAppsAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Uninstall failed"); }
    }

    private async Task UpdateAsync()
    {
        try
        {
            status.Text = "Checking latest GitHub release…";
            var json = await http.GetStringAsync(Api);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var current = Version.TryParse(Application.ProductVersion.Split('+')[0], out var installedVersion) ? installedVersion : new Version(0, 1, 0);
            if (!Version.TryParse(tag.TrimStart('v','V'), out var latest) || latest <= current)
            { MessageBox.Show($"Installed: v{current}\nLatest release: {tag}\nNo newer update detected.", "FullTV updates"); status.Text = "Up to date."; return; }
            var assets = root.GetProperty("assets").EnumerateArray().ToArray();
            var installer = assets.FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase) == true);
            if (installer.ValueKind == JsonValueKind.Undefined) { Open(Repo + "/releases/latest"); return; }
            var name = installer.GetProperty("name").GetString()!;
            if (MessageBox.Show($"Update available: {tag}\nDownload and run {name}?\n\nOnly install signed releases from the official FullTV repository.", "FullTV update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var checksum = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == name + ".sha256");
            if (checksum.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("Release checksum is missing. Update cancelled.");
            var bytes = await http.GetByteArrayAsync(installer.GetProperty("browser_download_url").GetString());
            var expectedText = await http.GetStringAsync(checksum.GetProperty("browser_download_url").GetString());
            var expected = expectedText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            var actual = Convert.ToHexString(SHA256.HashData(bytes));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Update checksum verification failed.");
            var path = Path.Combine(Path.GetTempPath(), "FullTV-" + tag.Replace("/", "") + "-Setup.exe");
            await File.WriteAllBytesAsync(path, bytes);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex) { status.Text = "Update check unavailable."; MessageBox.Show(ex.Message, "Update"); }
    }

    private void ToggleFullscreen()
    {
        tvFullscreen = !tvFullscreen;
        if (tvFullscreen)
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
        }
        else
        {
            TopMost = false;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.Sizable;
        }
        status.Text = tvFullscreen ? "TV fullscreen mode · Esc to exit · Arrow keys to navigate · Enter to select" : "Windowed mode";
    }

    // Keyboard navigation also works with gamepads that map their D-pad to arrow keys.
    // Direct XInput controller support remains separate and is not claimed here.
    private void HandleControllerKey(Keys key)
    {
        if (key == Keys.Enter) { if (ActiveControl is Button btn) btn.PerformClick(); return; }
        HandleTvKeys(this, new KeyEventArgs(key));
    }

    private void HandleTvKeys(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if (e.KeyCode == Keys.Escape && tvFullscreen) { ToggleFullscreen(); e.Handled = true; return; }
        if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) return;
        var buttons = focusable.Where(b => !b.IsDisposed && b.Visible && b.Enabled && b.CanFocus).ToList();
        if (buttons.Count == 0) return;
        var selected = buttons.FindIndex(b => b.Focused);
        var step = e.KeyCode is Keys.Left or Keys.Up ? -1 : 1;
        var next = (selected + step + buttons.Count) % buttons.Count;
        buttons[next].Focus();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void Preferences()
    {
        using var dialog = new Form { Text = "FullTV preferences", Size = new Size(510, 290), StartPosition = FormStartPosition.CenterParent, BackColor = BackColor, ForeColor = ForeColor };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown };
        panel.Controls.Add(new Label { Text = "ADB executable path (platform-tools)", AutoSize = true, ForeColor = Color.White });
        var input = new TextBox { Text = adbPath, Width = 420 };
        panel.Controls.Add(input);
        var theme = Button("Switch accent theme", (_, _) => { accent = accent.R == 121 ? Color.FromArgb(19, 155, 145) : Color.FromArgb(121, 116, 248); });
        panel.Controls.Add(theme);
        panel.Controls.Add(Button("Save settings", (_, _) => { adbPath = input.Text.Trim(); SavePreferences(); dialog.Close(); BuildUi(); }));
        dialog.Controls.Add(panel);
        dialog.ShowDialog(this);
    }

    private void LoadPreferences()
    {
        var path = Path.Combine(settingsDir, "preferences.json");
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("adbPath", out var adb)) adbPath = adb.GetString() ?? "adb";
            if (doc.RootElement.TryGetProperty("accent", out var a)) accent = ColorTranslator.FromHtml(a.GetString() ?? "#7974F8");
        }
        catch { /* Keep safe defaults if preferences are invalid. */ }
    }
    private void SavePreferences() => File.WriteAllText(Path.Combine(settingsDir, "preferences.json"), JsonSerializer.Serialize(new { adbPath, accent = ColorTranslator.ToHtml(accent) }));
    private static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
