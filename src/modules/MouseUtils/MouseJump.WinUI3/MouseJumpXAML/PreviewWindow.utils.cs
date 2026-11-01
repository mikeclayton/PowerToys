// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MouseJump.WinUI3.Win32Gen;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace MouseJump.WinUI3.UI;

internal sealed partial class PreviewWindow
{
    /// <summary>
    /// Calculates the high-dpi scaling ratio based on the current monitor's display settings.
    /// </summary>
    private double GetHighDpiScalingRatio()
    {
        var hWnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowDpi = User32.GetDpiForWindow(hWnd)
            .ThrowIfFailed()
            .GetValue();
        var scalingRatio = (double)PInvoke.USER_DEFAULT_SCREEN_DPI / windowDpi;
        return scalingRatio;
    }

    private async Task InvokeOnUiThreadAsync(Action action)
    {
        // this might be called from a task that we're awaiting
        // so we need to make sure we use the UI thread
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!this.DispatcherQueue.TryEnqueue(
            () =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            tcs.SetException(
                new InvalidOperationException("DispatcherQueue is not accepting work items."));
        }

        // wait for the task to complete
        await tcs.Task.ConfigureAwait(false);
    }
}
