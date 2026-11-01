// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml;

namespace MouseJump.WinUI3.UI;

internal sealed partial class PreviewWindow
{
    private void PreviewWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        switch (e.WindowActivationState)
        {
            case WindowActivationState.CodeActivated:
                this.PreviewImage.Focus(FocusState.Programmatic);
                break;
            case WindowActivationState.Deactivated:
                this.HideWindow();
                break;
            case WindowActivationState.PointerActivated:
                this.PreviewImage.Focus(FocusState.Pointer);
                break;
            default:
                throw new InvalidOperationException();
        }
    }
}
