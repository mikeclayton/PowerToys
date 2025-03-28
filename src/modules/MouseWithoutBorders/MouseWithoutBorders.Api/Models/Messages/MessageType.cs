// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MouseWithoutBorders.Api.Models.Messages;

public enum MessageType
{
    HeartbeatMessage,

    PingRequest,
    PingResponse,

    MachineMatrixRequest,
    MachineMatrixResponse,

    ScreenInfoRequest,
    ScreenInfoResponse,

    ScreenshotRequest,
    ScreenshotStartResponse,
    ScreenshotDataResponse,
    ScreenshotFinishResponse,
}
