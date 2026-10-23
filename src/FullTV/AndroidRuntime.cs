using System.Diagnostics;
using System.Windows.Forms;

namespace FullTV;

/// <summary>Starts a locally provisioned Android x86 guest using a user-supplied QEMU binary and disk.
/// FullTV does not distribute an unlicensed OS image or silently install third-party software.</summary>
internal static class AndroidRuntime
{
    private static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FullTV", "Android");
    private static string Disk => Path.Combine(Data, "android.qcow2");
    private static string Config => Path.Combine(Data, "qemu-path.txt");
    private static Process? running;
    public static bool IsRunning => running is { HasExited: false };
    public static bool IsConfigured => File.Exists(Disk) && File.Exists(Config);
    public static void ShowManager(IWin32Window owner)
    {
        Directory.CreateDirectory(Data);
        using var dialog = new Form { Text="FullTV — Android runtime", Width=670, Height=460, StartPosition=FormStartPosition.CenterParent, BackColor=System.Drawing.Color.FromArgb(18,23,39), ForeColor=System.Drawing.Color.White };
        var panel = new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(22), FlowDirection=FlowDirection.TopDown, AutoScroll=true, WrapContents=false };
        var help = new Label { AutoSize=false, Size=new System.Drawing.Size(590,95), Text="FullTV Android runtime\n\nChoose a QEMU executable and a legally obtained, already installed Android x86 QCOW2 disk. FullTV can boot it with WHPX; it cannot supply a ready-to-use Android image yet." };
        panel.Controls.Add(help);
        var state = new Label { AutoSize=false, Size=new System.Drawing.Size(590,50), Text=IsRunning?"Android running":IsConfigured?"Android disk configured":"Android disk not configured" };
        panel.Controls.Add(state);
        Button Make(string title, Action callback) { var b=new Button { Text=title, Width=360, Height=38, BackColor=System.Drawing.Color.FromArgb(55,59,94), ForeColor=System.Drawing.Color.White, FlatStyle=FlatStyle.Flat };b.Click+=(_,_)=>callback();panel.Controls.Add(b);return b; }
        Make("1. Select qemu-system-x86_64.exe",()=>{
            using var fd=new OpenFileDialog { Title="Select QEMU executable",Filter="QEMU executable (*.exe)|*.exe" };
            if(fd.ShowDialog(dialog)==DialogResult.OK) { File.WriteAllText(Config,fd.FileName);state.Text="QEMU configured. Select Android disk."; }
        });
        Make("2. Import installed Android QCOW2 disk",()=>{
            using var fd=new OpenFileDialog { Title="Select Android QCOW2 disk",Filter="QEMU disk (*.qcow2)|*.qcow2" };
            if(fd.ShowDialog(dialog)!=DialogResult.OK)return;
            if(MessageBox.Show(dialog,"Copy selected disk into FullTV app data? Existing guest disk will be replaced. Back up important app data first.","Import Android",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            File.Copy(fd.FileName,Disk,true);state.Text="Android disk imported.";
        });
        Make("3. Start Android TV environment",()=>{
            try { Start();state.Text="Android started in QEMU window. Use the app's Connect ADB feature when the guest has debugging enabled."; }
            catch(Exception ex){ MessageBox.Show(dialog,ex.Message,"Android runtime"); }
        });
        Make("Stop Android",()=>{
            if(running is { HasExited:false }) { running.CloseMainWindow();state.Text="Requested shutdown. Allow guest to exit safely."; }
        });
        var note=new Label { Size=new System.Drawing.Size(590,70),Text="Requirements: BIOS virtualisation and Windows Hypervisor Platform enabled. Android network and ADB configuration are guest-dependent. Do not import untrusted disk images."};
        panel.Controls.Add(note); dialog.Controls.Add(panel);dialog.ShowDialog(owner);
    }
    private static void Start()
    {
        if(IsRunning) throw new InvalidOperationException("Android is already running.");
        if(!IsConfigured) throw new InvalidOperationException("Select QEMU and import an installed Android disk first.");
        var exe=File.ReadAllText(Config).Trim();
        if(!File.Exists(exe))throw new FileNotFoundException("QEMU executable not found",exe);
        var psi=new ProcessStartInfo(exe) { UseShellExecute=false, WorkingDirectory=Path.GetDirectoryName(exe)! };
        foreach(var arg in new[]{"-accel","whpx","-M","pc","-smp","4","-m","4096","-device","usb-tablet","-drive",$"file={Disk},format=qcow2,if=ide","-netdev","user,id=net0,hostfwd=tcp:127.0.0.1:5555-:5555","-device","e1000,netdev=net0"})psi.ArgumentList.Add(arg);
        running=Process.Start(psi) ?? throw new InvalidOperationException("QEMU did not start.");
    }
}
