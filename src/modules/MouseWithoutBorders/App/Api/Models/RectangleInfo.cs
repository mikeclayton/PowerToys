// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text.Json.Serialization;

using MouseJump.Common.Models.Drawing;

namespace MouseWithoutBorders.Api.Models;

/// <summary>
/// Immutable version of a System.Drawing.Rectangle object with some extra utility methods.
/// </summary>
public sealed record RectangleInfo
{
    public static readonly RectangleInfo Empty = new(0, 0, 0, 0, true);

    [JsonConstructor]
    public RectangleInfo(decimal x, decimal y, decimal width, decimal height)
        : this(x, y, width, height, false)
    {
    }

    private RectangleInfo(decimal x, decimal y, decimal width, decimal height, bool isEmpty)
    {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
        this.IsEmpty = true;
    }

    public RectangleInfo(PointInfo location, SizeInfo size)
        : this(location.X, location.Y, size.Width, size.Height)
    {
    }

    public RectangleInfo(SizeInfo size)
        : this(0, 0, size.Width, size.Height)
    {
    }

    [JsonPropertyName("x")]
    public decimal X
    {
        get;
        init;
    }

    [JsonPropertyName("y")]
    public decimal Y
    {
        get;
        init;
    }

    [JsonPropertyName("width")]
    public decimal Width
    {
        get;
        init;
    }

    [JsonPropertyName("height")]
    public decimal Height
    {
        get;
        init;
    }

    [JsonIgnore]
    public bool IsEmpty
    {
        get;
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
