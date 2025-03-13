// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MouseJump.Common.Models.Drawing;
using MouseJump.Common.Models.Styles;
using static MouseWithoutBorders.Class.NativeMethods;

namespace MouseWithoutBorders.Api.Models;

/// <summary>
/// Immutable version of a System.Drawing.Rectangle object with some extra utility methods.
/// </summary>
public sealed record RectangleInfo
{
    public RectangleInfo(decimal x, decimal y, decimal width, decimal height)
    {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
    }

    internal RectangleInfo(RECT rect)
        : this(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)
    {
    }

    public decimal X
    {
        get;
        init;
    }

    public decimal Y
    {
        get;
        init;
    }

    public decimal Width
    {
        get;
        init;
    }

    public decimal Height
    {
        get;
        init;
    }

    public override string ToString()
    {
        return "{" +
            $"{nameof(this.X)}={this.X}," +
            $"{nameof(this.Y)}={this.Y}," +
            $"{nameof(this.Width)}={this.Width}," +
            $"{nameof(this.Height)}={this.Height}" +
            "}";
    }
}
