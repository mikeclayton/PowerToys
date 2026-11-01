// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Windowing;

using MouseJump.WinUI3.Win32Gen;

using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace MouseJump.WinUI3.UI;

internal sealed partial class PreviewWindow
{
    /// <summary>
    /// Initializes some settings on the application window.
    /// </summary>
    private void InitializeWindow()
    {
        var appWindow = this.AppWindow;
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            var hWnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(this);

            // get the current window style
            var result = User32.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE)
                .ThrowIfFailed()
                .GetValue();

            // set the window to be borderless, with no title bar, and hide all of the max / min / close buttons
            var style = (WINDOW_STYLE)result;
            style &= ~WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
            style |= WINDOW_STYLE.WS_POPUP;
            _ = User32.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style)
                .ThrowIfFailed();

            // get the current extended window style
            result = User32.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE)
                .ThrowIfFailed()
                .GetValue();

            // set the window to be borderless, with no title bar, and hide all of the max / min / close buttons
            var exStyle = (WINDOW_EX_STYLE)result;
            exStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW; // hide the taskbar icon
            exStyle |= WINDOW_EX_STYLE.WS_EX_TOPMOST;    // make topmost
            _ = User32.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle)
                .ThrowIfFailed();
        }

        this.Activated += this.PreviewWindow_Activated;
        this.StackPanel.PreviewKeyDown += this.PreviewWindow_PreviewKeyDown;
        this.PreviewImage.PreviewKeyDown += this.PreviewWindow_PreviewKeyDown;
        this.PreviewImage.PointerPressed += this.PreviewImage_PointerPressed;
    }
}
