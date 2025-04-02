// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace MouseWithoutBorders.Api.Models.Messages;

public struct ThumbnailRequest
{
    [JsonInclude]
    public int ScreenId;
    [JsonInclude]
    public int SourceX;
    [JsonInclude]
    public int SourceY;
    [JsonInclude]
    public int SourceWidth;
    [JsonInclude]
    public int SourceHeight;
    [JsonInclude]
    public int TargetWidth;
    [JsonInclude]
    public int TargetHeight;
}
