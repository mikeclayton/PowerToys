// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

using MouseWithoutBorders.Api.Models.Drawing;

#nullable enable

namespace MouseWithoutBorders.Api.Models.Display;

/// <summary>
/// Immutable version of a System.Windows.Forms.Screen object so we don't need to
/// take a dependency on WinForms just for screen info.
/// </summary>
public sealed record ScreenInfo
{
    public ScreenInfo(int id, bool primary, RectangleInfo displayArea, RectangleInfo? workingArea)
    {
        this.Id = id;
        this.Primary = primary;
        this.DisplayArea = displayArea ?? throw new ArgumentNullException(nameof(displayArea));
        this.WorkingArea = workingArea;
    }

    [JsonPropertyName("id")]
    public int Id
    {
        get;
    }

    [JsonPropertyName("primary")]
    public bool Primary
    {
        get;
    }

    [JsonPropertyName("displayArea")]
    public RectangleInfo DisplayArea
    {
        get;
    }

    [JsonPropertyName("workingArea")]
    public RectangleInfo? WorkingArea
    {
        get;
    }
}
