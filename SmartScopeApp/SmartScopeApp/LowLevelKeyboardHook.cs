using System; using System.Runtime.InteropServices; using System.Windows.Forms;
namespace SmartScopeApp
{
    public class LowLevelKeyboardHook
    {
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc fn, IntPtr hMod, uint tid);
        [DllImport("user32.dll")] private static extern bool   UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int n, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string m);
        private delegate IntPtr LowLevelKeyboardProc(int n, IntPtr wp, IntPtr lp);
        private const int WH_KEYBOARD_LL=13, WM_KEYDOWN=0x100, WM_SYSKEYDOWN=0x104;
        private const int VK_TAB=9, VK_ESC=0x1B, VK_F4=0x73, VK_LWIN=0x5B, VK_RWIN=0x5C, VK_DEL=0x2E, VK_F11=0x7A;
        private LowLevelKeyboardProc? _proc;
        private IntPtr _hook = IntPtr.Zero;
        public void Install()
        {
            _proc = Hook;
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            using var m = p.MainModule!;
            _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(m.ModuleName!), 0);
        }
        public void Uninstall() { if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; } }
        private IntPtr Hook(int n, IntPtr wp, IntPtr lp)
        {
            if (n >= 0 && (wp==(IntPtr)WM_KEYDOWN || wp==(IntPtr)WM_SYSKEYDOWN))
            {
                int vk = Marshal.ReadInt32(lp);
                bool alt  = (Control.ModifierKeys & Keys.Alt)     != 0;
                bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;
                if (vk==VK_LWIN || vk==VK_RWIN)              return (IntPtr)1;
                if (alt  && (vk==VK_TAB||vk==VK_F4||vk==VK_ESC)) return (IntPtr)1;
                if (ctrl && vk==VK_ESC)                        return (IntPtr)1;
                if (ctrl && alt && vk==VK_DEL)                 return (IntPtr)1;
                if (vk==VK_F11)                                return (IntPtr)1;
            }
            return CallNextHookEx(_hook, n, wp, lp);
        }
    }
}
