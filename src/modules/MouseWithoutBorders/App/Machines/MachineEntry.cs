// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using ID = MouseWithoutBorders.Core.ID;

namespace MouseWithoutBorders.Machines;

internal sealed class MachineEntry
{
    internal MachineEntry(string hostname, ID id, long lastSeenTick)
    {
        this.Hostname = hostname;
        this.Id = id;
        this.LastSeenTick = lastSeenTick;
    }

    public string Hostname
    {
        get;
    }

    public ID Id
    {
        get;
    }

    public long LastSeenTick
    {
        get;
    }
}
