// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

using MouseWithoutBorders.Api.Models.Drawing;

namespace MouseWithoutBorders.Api.Models.Styles;

public sealed class PreviewStyle
{
    public PreviewStyle(
        SizeInfo canvasSize,
        BoxStyle canvasStyle,
        BoxStyle screenStyle,
        IEnumerable<Color> mwbColors)
    {
        this.CanvasSize = canvasSize ?? throw new ArgumentNullException(nameof(canvasSize));
        this.CanvasStyle = canvasStyle ?? throw new ArgumentNullException(nameof(canvasStyle));
        this.ScreenStyle = screenStyle ?? throw new ArgumentNullException(nameof(screenStyle));
        this.MwbColors = new(
            (mwbColors ?? throw new ArgumentNullException(nameof(mwbColors)))
                .ToList());
    }

    public SizeInfo CanvasSize
    {
        get;
    }

    public BoxStyle CanvasStyle
    {
        get;
    }

    public BoxStyle ScreenStyle
    {
        get;
    }

    public ReadOnlyCollection<Color> MwbColors
    {
        get;
    }
}
