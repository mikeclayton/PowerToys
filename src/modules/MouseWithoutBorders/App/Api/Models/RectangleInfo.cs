// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Drawing;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public decimal Left =>
        this.X;

    [JsonIgnore]
    public decimal Top =>
        this.Y;

    [JsonIgnore]
    public decimal Right =>
        this.X + this.Width;

    [JsonIgnore]
    public decimal Bottom =>
        this.Y + this.Height;

    public Rectangle ToRectangle() =>
        new(
            (int)this.X,
            (int)this.Y,
            (int)this.Width,
            (int)this.Height);

    public override string ToString()
    {
        return "{" +
            $"{nameof(this.Left)}={this.Left}," +
            $"{nameof(this.Top)}={this.Top}," +
            $"{nameof(this.Width)}={this.Width}," +
            $"{nameof(this.Height)}={this.Height}" +
            "}";
    }
}
