// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

using MouseWithoutBorders.Api.Models.Display;

namespace MouseWithoutBorders.Api.Models.Messages;

public struct ThumbnailResponse
{
    [JsonInclude]
    public byte[] ImageBytes;

    public ThumbnailResponse(byte[] imageBytes)
    {
        this.ImageBytes = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
    }
}
