using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FullTV;

/// <summary>Xbox-compatible controller D-pad / A / B navigation through Windows XInput.</summary>
internal sealed class GamepadNavigation : IDisposable
{
    private const ushort Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008;
    private const ushort A = 0x1000, B = 0x2000;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 75 };
    private readonly Action<Keys> keyHandler;
    private ushort lastButtons;
    private DateTime nextRepeat = DateTime.MinValue;
    private bool available = true;
    public GamepadNavigation(Action<Keys> keyHandler)
    {
        this.keyHandler = keyHandler;
        timer.Tick += (_, _) => Poll();
        timer.Start();
    }
    private void Poll()
    {
        if (!available) return;
        try
        {
            uint code = 1167;
            XInputState state = default;
            for (uint i = 0; i < 4; i++)
            {
                code = XInputGetState(i, out state);
                if (code == 0) break;
            }
            if (code != 0) { lastButtons = 0; return; }
            ushort current = state.Gamepad.Buttons;
            bool changed = current != lastButtons;
            if (changed || DateTime.UtcNow >= nextRepeat)
            {
                if ((current & Up) != 0) keyHandler(Keys.Up);
                else if ((current & Down) != 0) keyHandler(Keys.Down);
                else if ((current & Left) != 0) keyHandler(Keys.Left);
                else if ((current & Right) != 0) keyHandler(Keys.Right);
                else if (changed && (current & A) != 0) keyHandler(Keys.Enter);
                else if (changed && (current & B) != 0) keyHandler(Keys.Escape);
                nextRepeat = DateTime.UtcNow.AddMilliseconds(changed ? 350 : 130);
            }
            lastButtons = current;
        }
        catch (DllNotFoundException) { available = false; timer.Stop(); }
        catch (EntryPointNotFoundException) { available = false; timer.Stop(); }
    }
    public void Dispose() => timer.Dispose();
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint index, out XInputState state);
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState { public uint Packet; public XInputGamepad Gamepad; }
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
    }
}
