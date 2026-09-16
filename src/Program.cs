using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Screen Color Picker Helper")]
[assembly: System.Reflection.AssemblyDescription("Open-source Windows screen color picker helper")]
[assembly: System.Reflection.AssemblyCompany("Vladimir Tsiko")]
[assembly: System.Reflection.AssemblyProduct("Screen Color Picker Helper")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 Vladimir Tsiko")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

internal static class Program
{
    private const int VkLeftButton = 0x01;
    private const int VkRightButton = 0x02;
    private const int VkEscape = 0x1B;
    private const uint InvalidColor = 0xFFFFFFFF;
    private const int ClickModeMaxMs = 260;
    private const int ClickModeMaxTravel = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr deviceContext, int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc,
        ref System.Drawing.Point destinationPoint, ref System.Drawing.Size size,
        IntPtr sourceDc, ref System.Drawing.Point sourcePoint, int colorKey,
        ref BlendFunction blend, int flags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr graphicObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    private delegate IntPtr LowLevelMouseProc(int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelMouseProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    private static bool IsDown(int key)
    {
        return (GetAsyncKeyState(key) & 0x8000) != 0;
    }

    private static string Sample(IntPtr desktop)
    {
        Point point;
        if (!GetCursorPos(out point)) return null;
        uint color = GetPixel(desktop, point.X, point.Y);
        if (color == InvalidColor) return null;
        int red = (int)(color & 0xFF);
        int green = (int)((color >> 8) & 0xFF);
        int blue = (int)((color >> 16) & 0xFF);
        return string.Format("#{0:X2}{1:X2}{2:X2}", red, green, blue);
    }

    private sealed class PickerPreview : Form
    {
        private const int WsExLayered = 0x00080000;
        private const int UlwAlpha = 0x00000002;
        private const byte AcSrcOver = 0x00;
        private const byte AcSrcAlpha = 0x01;
        private const int PreviewWidth = 140;
        private const int PreviewHeight = 50;
        private const int CursorGap = 18;
        private Color swatchColor = Color.Black;
        private string colorText = "000000";

        internal PickerPreview()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(PreviewWidth, PreviewHeight);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000;
                parameters.ExStyle |= 0x00000080;
                parameters.ExStyle |= 0x00000020;
                parameters.ExStyle |= WsExLayered;
                return parameters;
            }
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2.0f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal void UpdatePreview(string hex, Point pointer)
        {
            if (String.IsNullOrEmpty(hex) || hex.Length != 7) return;
            try { swatchColor = ColorTranslator.FromHtml(hex); }
            catch { return; }
            colorText = hex.Substring(1).ToLowerInvariant();

            Screen screen = Screen.FromPoint(new System.Drawing.Point(pointer.X, pointer.Y));
            Rectangle area = screen.WorkingArea;
            int x = pointer.X + CursorGap;
            int y = pointer.Y + CursorGap;
            if (x + Width > area.Right) x = pointer.X - Width - CursorGap;
            if (y + Height > area.Bottom) y = pointer.Y - Height - CursorGap;
            x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));
            Location = new System.Drawing.Point(x, y);

            if (!IsHandleCreated) CreateControl();
            RenderLayered();
            if (!Visible) { Show(); RenderLayered(); }
        }

        private void RenderLayered()
        {
            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (GraphicsPath outer = RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1.0f, Height - 1.0f), 10.0f))
                using (SolidBrush background = new SolidBrush(Color.FromArgb(246, 36, 36, 36)))
                using (Pen border = new Pen(Color.FromArgb(255, 74, 74, 74), 1.0f))
                {
                    graphics.FillPath(background, outer);
                    graphics.DrawPath(border, outer);
                }

                using (GraphicsPath swatch = RoundedRectangle(new RectangleF(5.5f, 5.5f, 39.0f, 39.0f), 7.0f))
                using (SolidBrush color = new SolidBrush(swatchColor))
                using (Pen border = new Pen(Color.FromArgb(255, 78, 78, 78), 1.0f))
                {
                    graphics.FillPath(color, swatch);
                    graphics.DrawPath(border, swatch);
                }

                using (Font font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Regular, GraphicsUnit.Point))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Center;
                    format.FormatFlags = StringFormatFlags.NoWrap;
                    graphics.DrawString(colorText, font, textBrush,
                        new RectangleF(56.0f, 0.0f, Width - 62.0f, Height), format);
                }
                ApplyLayeredBitmap(bitmap);
            }
        }

        private void ApplyLayeredBitmap(Bitmap bitmap)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memoryDc = CreateCompatibleDC(screenDc);
            IntPtr bitmapHandle = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memoryDc, bitmapHandle);
                System.Drawing.Point destination = new System.Drawing.Point(Left, Top);
                System.Drawing.Point source = new System.Drawing.Point(0, 0);
                System.Drawing.Size size = bitmap.Size;
                BlendFunction blend = new BlendFunction();
                blend.BlendOp = AcSrcOver;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AcSrcAlpha;
                UpdateLayeredWindow(Handle, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, UlwAlpha);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(memoryDc, oldBitmap);
                if (bitmapHandle != IntPtr.Zero) DeleteObject(bitmapHandle);
                if (memoryDc != IntPtr.Zero) DeleteDC(memoryDc);
                if (screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDc);
            }
        }
    }

    private sealed class GlobalPickerSession : IDisposable
    {
        private const int WhMouseLl = 14;
        private const int WmLeftButtonDown = 0x0201;
        private const int WmLeftButtonUp = 0x0202;
        private const int WmRightButtonDown = 0x0204;
        private const int WmRightButtonUp = 0x0205;
        private readonly IntPtr desktop;
        private readonly PickerPreview preview;
        private readonly LowLevelMouseProc hookCallback;
        private readonly Cursor previousCursor;
        private IntPtr hook;
        private bool leftButtonDown;
        private bool completed;
        private bool cancelled;
        private bool disposed;
        private string lastColor;

        internal string SelectedColor { get; private set; }
        internal bool IsReady { get { return desktop != IntPtr.Zero && hook != IntPtr.Zero; } }

        internal GlobalPickerSession()
        {
            desktop = GetDC(IntPtr.Zero);
            preview = new PickerPreview();
            previousCursor = Cursor.Current;
            hookCallback = HandleMouse;
            hook = SetWindowsHookEx(WhMouseLl, hookCallback, GetModuleHandle(null), 0);
        }

        internal DialogResult Run()
        {
            if (!IsReady) return DialogResult.Abort;
            try
            {
                while (!completed)
                {
                    if (IsDown(VkEscape)) { cancelled = true; completed = true; break; }
                    Point point;
                    if (GetCursorPos(out point))
                    {
                        string sampled = Sample(desktop);
                        if (sampled != null) { lastColor = sampled; preview.UpdatePreview(sampled, point); }
                    }
                    Cursor.Current = Cursors.Cross;
                    Application.DoEvents();
                    Thread.Sleep(8);
                }
                preview.Hide();
                if (cancelled) return DialogResult.Cancel;
                SelectedColor = lastColor;
                return SelectedColor == null ? DialogResult.Abort : DialogResult.OK;
            }
            finally { Dispose(); }
        }

        private IntPtr HandleMouse(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                int value = message.ToInt32();
                if (value == WmLeftButtonDown) { leftButtonDown = true; return new IntPtr(1); }
                if (value == WmLeftButtonUp)
                {
                    if (leftButtonDown)
                    {
                        string sampled = Sample(desktop);
                        if (sampled != null) lastColor = sampled;
                        completed = true;
                    }
                    leftButtonDown = false;
                    return new IntPtr(1);
                }
                if (value == WmRightButtonDown || value == WmRightButtonUp)
                {
                    cancelled = true;
                    completed = true;
                    return new IntPtr(1);
                }
            }
            return CallNextHookEx(hook, code, message, data);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
            preview.Close();
            preview.Dispose();
            if (desktop != IntPtr.Zero) ReleaseDC(IntPtr.Zero, desktop);
            try { Cursor.Current = previousCursor; } catch { }
        }
    }

    private sealed class HotkeyHost : Form
    {
        private const int WmHotkey = 0x0312;
        private const int HotkeyId = 0x4952;
        private readonly uint modifiers;
        private readonly uint virtualKey;
        private bool registered;
        private bool picking;
        internal int ExitCode { get; private set; }

        internal HotkeyHost(uint modifierMask, uint keyCode)
        {
            modifiers = modifierMask;
            virtualKey = keyCode;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(-32000, -32000);
            ClientSize = new Size(1, 1);
            ShowInTaskbar = false;
            Opacity = 0.0;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000;
                parameters.ExStyle |= 0x00000080;
                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            registered = RegisterHotKey(Handle, HotkeyId, modifiers, virtualKey);
            if (!registered)
            {
                ExitCode = 15;
                Console.Out.WriteLine("HOTKEY_ERROR:REGISTER");
                Console.Out.Flush();
                BeginInvoke(new MethodInvoker(Close));
                return;
            }
            Console.Out.WriteLine("HOTKEY_READY");
            Console.Out.Flush();
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmHotkey && message.WParam.ToInt32() == HotkeyId && !picking)
                BeginInvoke(new MethodInvoker(Pick));
            base.WndProc(ref message);
        }

        private void Pick()
        {
            if (picking) return;
            picking = true;
            Console.Out.WriteLine("HOTKEY_PICKING");
            Console.Out.Flush();
            try
            {
                using (GlobalPickerSession session = new GlobalPickerSession())
                {
                    DialogResult result = session.Run();
                    if (result == DialogResult.OK && session.SelectedColor != null)
                        Console.Out.WriteLine("PICK:" + session.SelectedColor);
                    else Console.Out.WriteLine("HOTKEY_CANCEL");
                    Console.Out.Flush();
                }
            }
            catch
            {
                Console.Out.WriteLine("HOTKEY_ERROR:PICK");
                Console.Out.Flush();
            }
            finally { picking = false; }
        }

        protected override void OnFormClosed(FormClosedEventArgs eventArgs)
        {
            if (registered) UnregisterHotKey(Handle, HotkeyId);
            registered = false;
            base.OnFormClosed(eventArgs);
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try { SetProcessDPIAware(); } catch { }

        if (args != null && args.Length == 1 && args[0] == "--picker-hook-check")
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (GlobalPickerSession session = new GlobalPickerSession()) return session.IsReady ? 0 : 17;
        }

        if (args != null && args.Length == 3 && args[0] == "--hotkey-server")
        {
            uint modifierMask;
            uint keyCode;
            if (!UInt32.TryParse(args[1], out modifierMask) || !UInt32.TryParse(args[2], out keyCode)) return 16;
            if (keyCode < 8 || keyCode > 255) return 16;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (HotkeyHost host = new HotkeyHost(modifierMask, keyCode))
            {
                Thread stdinWatcher = new Thread(delegate()
                {
                    try
                    {
                        while (Console.In.ReadLine() != null) { }
                        if (host.IsHandleCreated && !host.IsDisposed) host.BeginInvoke(new MethodInvoker(host.Close));
                    }
                    catch { }
                });
                stdinWatcher.IsBackground = true;
                stdinWatcher.Start();
                Application.Run(host);
                return host.ExitCode;
            }
        }

        if (args != null && args.Length != 0) return 16;
        if (!IsDown(VkLeftButton)) return 3;

        IntPtr desktop = GetDC(IntPtr.Zero);
        if (desktop == IntPtr.Zero) return 4;
        string lastColor = null;
        Point startPoint;
        GetCursorPos(out startPoint);
        int startedAt = Environment.TickCount;
        int maxTravel = 0;
        using (PickerPreview preview = new PickerPreview())
        try
        {
            while (IsDown(VkLeftButton))
            {
                if (IsDown(VkEscape) || IsDown(VkRightButton)) return 2;
                Point currentPoint;
                if (GetCursorPos(out currentPoint))
                {
                    int travel = Math.Max(Math.Abs(currentPoint.X - startPoint.X), Math.Abs(currentPoint.Y - startPoint.Y));
                    if (travel > maxTravel) maxTravel = travel;
                    string sampled = Sample(desktop);
                    if (sampled != null) { lastColor = sampled; preview.UpdatePreview(sampled, currentPoint); }
                }
                Application.DoEvents();
                Thread.Sleep(8);
            }
        }
        finally { preview.Hide(); ReleaseDC(IntPtr.Zero, desktop); }

        int heldMs = unchecked(Environment.TickCount - startedAt);
        if (heldMs <= ClickModeMaxMs && maxTravel <= ClickModeMaxTravel)
        {
            using (GlobalPickerSession session = new GlobalPickerSession())
            {
                DialogResult result = session.Run();
                if (result == DialogResult.Cancel) return 2;
                if (result != DialogResult.OK || session.SelectedColor == null) return 4;
                lastColor = session.SelectedColor;
            }
        }

        if (lastColor == null) return 4;
        Console.Out.WriteLine(lastColor);
        return 0;
    }
}
