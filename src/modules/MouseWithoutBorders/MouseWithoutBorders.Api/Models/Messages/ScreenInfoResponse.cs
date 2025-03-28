// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

using MouseWithoutBorders.Api.Models.Display;

namespace MouseWithoutBorders.Api.Models.Messages;

public struct ScreenInfoResponse
{
    [JsonInclude]
    public List<ScreenInfo> ScreenInfo;

    public ScreenInfoResponse(List<ScreenInfo> screenInfo)
    {
        this.ScreenInfo = screenInfo ?? throw new ArgumentNullException(nameof(screenInfo));
    }
}
