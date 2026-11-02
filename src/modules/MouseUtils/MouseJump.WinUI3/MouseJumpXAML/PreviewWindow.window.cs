// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using ManagedCommon;
using Microsoft.PowerToys.Telemetry;
using Microsoft.UI.Windowing;

using MouseJump.Common.Capture;
using MouseJump.Common.Helpers;
using MouseJump.Models.Drawing;
using MouseJump.WinUI3.Helpers;
using MouseJump.WinUI3.Win32Gen;

using Windows.Graphics;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace MouseJump.WinUI3.UI;

internal sealed partial class PreviewWindow
{
    public async Task ShowPreviewAsync(CancellationToken cancellationToken)
    {
        Logger.LogInfo(string.Join(
            '\n',
            "-----------",
            nameof(PreviewWindow.ShowPreviewAsync),
            "-----------"));

        // hide the form while we redraw it...
        await this.HideWindowAsync()
            .ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();

        // capture this first so we get an accurate mouse location
        // (in case the user moves it a few pixels while the form is rendered)
        var activatedLocation = MouseHelper.GetCursorPosition();

        var appSettings = this.SettingsHelper.CurrentSettings ?? throw new InvalidOperationException();
        var displayInfo = DeviceHelper.GetDisplayInfo();

        var activatedScreen = DeviceHelper.GetActivatedScreen(displayInfo.Devices[0], activatedLocation);

        var previewStyle = SettingsHelper.GetActivePreviewStyle(appSettings);
        var formLayout = LayoutHelper.GetFormLayout(
            previewStyle,
            displayInfo,
            maximumSize: activatedScreen.DisplayArea.Size);

        // remember the layout so we can map the mouse clicks back to
        // the appropriate device and screen location
        this.FormLayout = formLayout;

        var formBounds = LayoutHelper.PositionOnScreen(
            formLayout.CanvasLayout.CanvasBounds.OuterBounds, activatedScreen, activatedLocation);
        await this.PositionWindowAsync(formBounds)
            .ConfigureAwait(false);

        var captureProviders = displayInfo.Devices
            .Select(
                deviceInfo => (IScreenshotCaptureProvider)new DesktopScreenshotCaptureProvider())
            .ToList();

        await DrawingHelper.RenderPreviewAsync(
                this.FormLayout.CanvasLayout,
                activatedScreen,
                captureProviders,
                this.OnPreviewImageCreatedAsync,
                this.OnPreviewImageUpdatedAsync)
            .ConfigureAwait(false);

        stopwatch.Stop();

        PowerToysTelemetry.Log.WriteEvent(new Telemetry.MouseJumpShowEvent());

        await this.ShowWindowAsync()
            .ConfigureAwait(false);
    }

    private void ClearPreview()
    {
        if (this.PreviewImage.Source is null)
        {
            return;
        }

        this.PreviewImage.Source = null;

        // force preview image memory to be released - otherwise
        // all the disposed images can pile up without being GC'ed
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void HideWindow()
    {
        this.AppWindow.Hide();
        this.ClearPreview();
    }

    private async Task HideWindowAsync()
    {
        await this.InvokeOnUiThreadAsync(
            () =>
            {
                this.HideWindow();
            }).ConfigureAwait(false);
    }

    private async Task ShowWindowAsync()
    {
        await this.InvokeOnUiThreadAsync(
            () =>
            {
                var presenter = this.AppWindow.Presenter as OverlappedPresenter
                    ?? throw new InvalidOperationException();
                if (!this.Visible)
                {
                    // we seem to need to turn off topmost and then re-enable it again
                    // when we show the form - otherwise it doesn't always get shown topmost...
                    presenter.IsAlwaysOnTop = false;
                    presenter.IsAlwaysOnTop = true;
                }

                var hWnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(this);

                // use raw Win32 calls instead of WinUI3's AppWindow.Show()/Activate()/Focus()
                // because WinUI3's activation sequence intermittently fails to transfer keyboard
                // focus, which causes the Window.Activated "Deactivated" event to never fire
                // when the user clicks away, leaving the preview window stuck open.
                _ = User32.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW)
                    .ThrowIfFailed();

                _ = User32.SetForegroundWindow(hWnd)
                    .ThrowIfFailed();

                _ = User32.SetFocus(hWnd)
                    .ThrowIfFailed();

                _ = User32.SetActiveWindow(hWnd)
                    .ThrowIfFailed();
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Resize and position the form.
    /// </summary>
    private async Task PositionWindowAsync(RectangleInfo bounds)
    {
        await this.InvokeOnUiThreadAsync(
            () =>
            {
                // note - do this with two calls to MoveAndResize rather than one as there appears to
                // be an issue with dpi scaling even when using PerMonitorV2, where if the window is
                // resized *and* moved in one call the resize uses the scaling of the *current*
                // monitor before it's moved.
                //
                // If the move then happens to be to a different monitor, *and* the monitor has a
                // different dpi scaling configured, the window size is then wrong for the dpi scaling
                // of the new monitor.
                //
                // the workaround seems to be to call MoveAndResize twice - the first call might
                // resize it incorrectly, but it moves the window to the correct monitor, and the
                // second call then resizes it correctly.
                //
                // see https://github.com/mikeclayton/FancyMouse/issues/2 for more details
                var windowBounds = new RectInt32((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
                this.AppWindow.MoveAndResize(windowBounds);
                this.AppWindow.MoveAndResize(windowBounds);
            }).ConfigureAwait(false);
    }
}
