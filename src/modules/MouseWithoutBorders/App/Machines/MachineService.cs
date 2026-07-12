// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

using System.Collections.Generic;

using ID = MouseWithoutBorders.Core.ID;

namespace MouseWithoutBorders.Machines;

internal static class MachineService
{
    private static readonly char[] Comma = new char[] { ',' };
    private static readonly char[] Colon = new char[] { ':' };

    internal static bool SetMachineDisconnected(MachineMatrix matrix, string machineName, long currentTick, long heartbeatTimeout)
    {
        long cutoff = currentTick - heartbeatTimeout + 10000;
        (bool found, long resultTick) = matrix.TryCapEntryLastSeenTick(machineName, cutoff);
        return found && resultTick < cutoff - 5000;
    }

    internal static bool IsAlive(MachineEntry? entry, long currentTick, long heartbeatTimeout, Func<ID, bool> isConnected)
    {
        return entry is not null
            && entry.Id != ID.NONE
            && (currentTick - entry.LastSeenTick <= heartbeatTimeout
                || isConnected(entry.Id));
    }

    internal static MachineMatrix DeserializeMachinePoolSetting(string s, long currentTick, long heartbeatTimeout)
    {
        ArgumentNullException.ThrowIfNull(s);

        string[] st = s.Split(Comma);

        // hard-code the machine matrix size for now
        MachineMatrix matrix = new MachineMatrix(1, 4);

        if (st.Length < matrix.MachineCount)
        {
            throw new ArgumentException("Not enough elements in encoded MachinePool string");
        }

        var entries = new List<MachineEntry?>();
        for (int i = 0; i < matrix.MachineCount; i++)
        {
            string[] mc = st[i].Split(Colon);
            if (mc.Length == 2)
            {
                var hostname = mc[0].Trim();
                var id = uint.TryParse(mc[1], out uint ip) ? (ID)ip : ID.NONE;
                var lastSeenTick = id == ID.NONE ? currentTick - heartbeatTimeout : currentTick;
                entries.Add(new MachineEntry(hostname, id, lastSeenTick));
            }
            else
            {
                entries.Add(null);
            }
        }

        matrix.Initialize(entries);
        return matrix;
    }

    internal static string SerializeMachinePoolSetting(MachineMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        MachineEntry?[] slots = matrix.GetSlotSnapshot();
        var parts = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            parts[i] = slots[i] is MachineEntry e ? $"{e.Hostname}:{e.Id}" : ":";
        }

        return string.Join(",", parts);
    }
}
