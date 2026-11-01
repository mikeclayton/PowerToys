// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml;

using MouseJump.WinUI3.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace MouseJump.WinUI3.UI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
/// <remarks>
/// Lives under the "/MouseJumpXAML" folder to avoid collisions with the compiled *.xbf files
/// from xaml files in other modules when they all get copied to the shared build output folder.
/// </remarks>
internal sealed partial class PreviewWindow : Window
{
    internal PreviewWindow(SettingsHelper settingsHelper)
    {
        this.SettingsHelper = settingsHelper ?? throw new ArgumentNullException(nameof(settingsHelper));
        this.InitializeComponent();
        this.InitializeWindow();
    }

    private SettingsHelper SettingsHelper
    {
        get;
    }
}
