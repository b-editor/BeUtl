﻿using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

using FluentAvalonia.Interop;

#nullable disable

namespace BEditor.Controls
{
    // Special Win32 window impl for a better extended window frame.
    // Not intended for outside use

    public static class WindowImplSolver
    {
        public static IWindowImpl GetWindowImpl()
        {
            if (Design.IsDesignMode)
                return PlatformManager.CreateWindow();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new CoreWindowImpl();
            }

            return PlatformManager.CreateWindow();
        }
    }

    public class CoreWindowImpl : Avalonia.Win32.WindowImpl
    {
        public CoreWindowImpl()
        {
            Win32Interop.OSVERSIONINFOEX version = new Win32Interop.OSVERSIONINFOEX
            {
                OSVersionInfoSize = Marshal.SizeOf<Win32Interop.OSVERSIONINFOEX>()
            };

            Win32Interop.RtlGetVersion(ref version);

            if (version.MajorVersion < 10)
            {
                throw new NotSupportedException("Windows versions earlier than 10 are not supported");
            }

            _isWindows11 = version.BuildNumber >= 22000;
            _version = version;
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((WM)msg)
            {
                case WM.NCCALCSIZE:
                    // Follows logic from how to extend window frame + WindowsTerminal + Firefox
                    // Windows Terminal only handles WPARAM = TRUE & only adjusts the top of the
                    // rgrc[0] RECT & gets the correct result
                    // Firefox, on the other hand, handles BOTH times WM_NCCALCSIZE is called,
                    // and modifies the RECT.
                    // This particularly differs from the "built-in" method in Avalonia in that
                    // I retain the SystemBorder & ability resize the window in the transparent
                    // area over the drop shadows, meaning resize handles don't overlap the window

                    if (wParam != IntPtr.Zero)
                    {
                        var ncParams = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);

                        var originalTop = ncParams.rgrc[0].top;

                        var ret = Win32Interop.DefWindowProc(hWnd, (uint)WM.NCCALCSIZE, wParam, lParam);
                        if (ret != IntPtr.Zero)
                            return ret;

                        var newSize = ncParams.rgrc[0];
                        newSize.top = originalTop;

                        if (WindowState == WindowState.Maximized)
                        {
                            newSize.top += GetResizeHandleHeight();
                        }

                        newSize.left += 8;
                        newSize.right -= 8;
                        newSize.bottom -= 8;

                        ncParams.rgrc[0] = newSize;

                        Marshal.StructureToPtr(ncParams, lParam, true);

                        return IntPtr.Zero;
                    }

                    return IntPtr.Zero;

                case WM.NCHITTEST:
                    return HandleNCHitTest(lParam);

                case WM.SIZE:
                    EnsureExtended();
                    break;

                case WM.NCMOUSEMOVE:
                    if (_fakingMaximizeButton)
                    {
                        var point = PointToClient(PointFromLParam(lParam));
                        _owner.FakeMaximizeHover(_owner.HitTestMaximizeButton(point));
                        return IntPtr.Zero;
                    }
                    break;

                case WM.NCLBUTTONDOWN:
                    if (_fakingMaximizeButton)
                    {
                        var point = PointToClient(PointFromLParam(lParam));
                        _owner.FakeMaximizePressed(_owner.HitTestMaximizeButton(point));
                        _wasFakeMaximizeDown = true;

                        // This is important. If we don't tell the System we've handled this, we'll get that
                        // classic Win32 button showing when we mouse press, and that's not good
                        return IntPtr.Zero;
                    }
                    break;

                case WM.NCLBUTTONUP:
                    if (_fakingMaximizeButton && _wasFakeMaximizeDown)
                    {
                        var point = PointToClient(PointFromLParam(lParam));
                        _owner.FakeMaximizePressed(false);
                        _wasFakeMaximizeDown = false;
                        _owner.FakeMaximizeClick();
                        return IntPtr.Zero;
                    }
                    break;
            }

            return base.WndProc(hWnd, msg, wParam, lParam);
        }

        internal void SetOwner(FluentWindow wnd)
        {
            _owner = wnd;

            if (_version.BuildNumber > 22000)
            {
                ((IPseudoClasses)_owner.Classes).Set(":windows11", true);
            }
            else
            {
                ((IPseudoClasses)_owner.Classes).Set(":windows10", true);
            }
        }

        private int GetResizeHandleHeight()
        {
            if (_version.BuildNumber >= 14393)
            {
                return Win32Interop.GetSystemMetricsForDpi(92 /*SM_CXPADDEDBORDER*/, (uint)(RenderScaling * 96)) +
                    Win32Interop.GetSystemMetricsForDpi(33 /* SM_CYSIZEFRAME */, (uint)(RenderScaling * 96));
            }

            return Win32Interop.GetSystemMetrics(92 /* SM_CXPADDEDBORDER */) +
                Win32Interop.GetSystemMetrics(33/* SM_CYSIZEFRAME */);
        }

        private void EnsureExtended()
        {
            // We completely ignore anything for extending client area in Avalonia Window impl b/c
            // we're doing super specialized stuff to ensure the best experience interacting with
            // the window and mimic-ing a "modern app"
            var marg = new Win32Interop.MARGINS();

            // WS_OVERLAPPEDWINDOW
            var style = 0x00000000L | 0x00C00000L | 0x00080000L | 0x00040000L | 0x00020000L | 0x00010000L;

            // This is causing the window to appear solid but is completely transparent. Weird...
            //Win32Interop.GetWindowLongPtr(Hwnd, -16).ToInt32();

            RECT frame = new RECT();
            Win32Interop.AdjustWindowRectExForDpi(ref frame,
                (int)style, false, 0, (int)(RenderScaling * 96));

            marg.topHeight = -frame.top + (_isWindows11 ? 0 : -1);
            Win32Interop.DwmExtendFrameIntoClientArea(Handle.Handle, ref marg);
        }

        protected IntPtr HandleNCHitTest(IntPtr lParam)
        {
            // Because we still have the System Border (which technically extends beyond the actual window
            // into where the Drop shadows are), we can use DefWindowProc here to handle resizing, except
            // on the top. We'll handle that below
            var originalRet = Win32Interop.DefWindowProc(Hwnd, (uint)WM.NCHITTEST, IntPtr.Zero, lParam);
            if (originalRet != new IntPtr(1))
            {
                return originalRet;
            }

            // At this point, we know that the cursor is inside the client area so it
            // has to be either the little border at the top of our custom title bar,
            // the drag bar or something else in the XAML island. But the XAML Island
            // handles WM_NCHITTEST on its own so actually it cannot be the XAML
            // Island. Then it must be the drag bar or the little border at the top
            // which the user can use to move or resize the window.

            var point = PointToClient(PointFromLParam(lParam));

            RECT rcWindow;
            Win32Interop.GetWindowRect(Hwnd, out rcWindow);

            // On the Top border, the resize handle overlaps with the Titlebar area, which matches
            // a typical Win32 window or modern app window
            var resizeBorderHeight = GetResizeHandleHeight();
            bool isOnResizeBorder = point.Y < resizeBorderHeight;

            // Make sure the caption buttons still get precedence
            // This is where things get tricky too. On Win11, we still want to suppor the snap
            // layout feature when hovering over the Maximize button. Unfortunately no API exists
            // yet to call that manually if using a custom titlebar. But, if we return HT_MAXBUTTON
            // here, the pointer events no longer enter the window
            // See https://github.com/dotnet/wpf/issues/4825 for more on this...
            // To hack our way into making this work, we'll return HT_MAXBUTTON here, but manually
            // manage the state and handle stuff through the WM_NCLBUTTON... events
            // This only applies on Windows 11, Windows 10 will work normally b/c no snap layout thing

            if (_owner.HitTestCaptionButtons(point))
            {
                if (_isWindows11)
                {
                    var result = _owner.HitTestMaximizeButton(point);

                    if (result)
                    {
                        _fakingMaximizeButton = true;
                        return new IntPtr(9);
                    }
                }
            }
            else
            {
                if (_fakingMaximizeButton)
                {
                    _fakingMaximizeButton = false;
                    _owner.FakeMaximizeHover(false);
                    _owner.FakeMaximizePressed(false);
                }

                if (WindowState != WindowState.Maximized && isOnResizeBorder)
                    return new IntPtr(12); // HT_TOP

                if (_owner.HitTestTitleBarRegion(point))
                    return new IntPtr(2); //HT_CAPTION
            }

            if (_fakingMaximizeButton)
            {
                _fakingMaximizeButton = false;
                _owner.FakeMaximizeHover(false);
                _owner.FakeMaximizePressed(false);
            }
            _fakingMaximizeButton = false;
            // return HT_CLIENT, we're in the normal window
            return new IntPtr(1);
        }

        private PixelPoint PointFromLParam(IntPtr lParam)
        {
            return new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16));
        }

        private static int ToInt32(IntPtr ptr)
        {
            if (IntPtr.Size == 4)
                return ptr.ToInt32();

            return (int)(ptr.ToInt64() & 0xffffffff);
        }

        private bool _wasFakeMaximizeDown;
        private bool _fakingMaximizeButton;
        private bool _isWindows11 = false;
        private FluentWindow _owner;
        private Win32Interop.OSVERSIONINFOEX _version;
    }
}
