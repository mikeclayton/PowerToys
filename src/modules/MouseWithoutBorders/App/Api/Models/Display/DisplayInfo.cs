// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable

namespace MouseWithoutBorders.Api.Models.Display;

public sealed record DisplayInfo
{
    public DisplayInfo(IEnumerable<DeviceInfo> devices)
    {
        this.Devices = new(
            (devices ?? throw new ArgumentNullException(nameof(devices)))
                .ToList());
    }

    public ReadOnlyCollection<DeviceInfo> Devices
    {
        get;
    }
}
