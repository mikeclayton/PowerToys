// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

using ID = MouseWithoutBorders.Core.ID;

namespace MouseWithoutBorders.Machines;

internal sealed partial class MachineMatrix
{
    // This will set the timestamp to current time, making the machine 'alive'.
    internal bool TryUpdateMachineID(string machineName, ID id, bool updateTimeStamp, long currentTick)
    {
        bool rv = false;
        lock (_lock)
        {
            CheckName(machineName);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is MachineEntry entry)
                {
                    if (HostnamesAreEqual(entry.Hostname, machineName))
                    {
                        _slots[i] = new MachineEntry(
                            entry.Hostname,
                            id,
                            updateTimeStamp ? currentTick : entry.LastSeenTick);
                        rv = true;
                    }
                    else if (entry.Id == id)
                    {
                        // Duplicate ID — reset the old machine's ID.
                        _slots[i] = new MachineEntry(
                            entry.Hostname,
                            ID.NONE,
                            updateTimeStamp ? currentTick : entry.LastSeenTick);
                    }
                }
            }
        }

        return rv;
    }

    // Caps the LastSeenTick of the named entry to at most maxTick.
    // Returns (true, resultTick) if a matching entry was found, (false, 0) otherwise.
    internal (bool Found, long ResultTick) TryCapEntryLastSeenTick(string hostname, long maxTick)
    {
        CheckName(hostname);
        lock (_lock)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is MachineEntry entry && HostnamesAreEqual(entry.Hostname, hostname))
                {
                    long newTick = entry.LastSeenTick > maxTick ? maxTick : entry.LastSeenTick;
                    _slots[i] = new MachineEntry(entry.Hostname, entry.Id, newTick);
                    return (true, newTick);
                }
            }
        }

        return (false, 0);
    }

    public void Initialize(IEnumerable<string> machineNames)
    {
        lock (_lock)
        {
            Array.Clear(_slots);
            int count = 0;
            foreach (string name in machineNames)
            {
                if (string.IsNullOrEmpty(name?.Trim()))
                {
                    continue;
                }
                else if (count >= _slots.Length)
                {
                    throw new ArgumentException($"The number of machines exceeded the maximum allowed limit of {MachineCount}. Actual count: {count}.");
                }

                _slots[count] = new MachineEntry(name.Trim(), ID.NONE, 0);
                count++;
            }
        }
    }

    public void Initialize(IEnumerable<MachineEntry?> entries)
    {
        lock (_lock)
        {
            Array.Clear(_slots);
            int count = 0;
            foreach (MachineEntry? entry in entries)
            {
                if (entry is null || string.IsNullOrEmpty(entry.Hostname))
                {
                    continue;
                }
                else if (count >= _slots.Length)
                {
                    throw new ArgumentException($"The number of machines exceeded the maximum allowed limit of {MachineCount}. Actual count: {count}.");
                }

                _slots[count] = new MachineEntry(entry.Hostname, entry.Id, entry.LastSeenTick);
                count++;
            }
        }
    }

    // Returns the hostname at the given slot index, or an empty string if the slot is empty.
    public string GetHostname(int slotIndex) => this[slotIndex]?.Hostname ?? string.Empty;

    // Sets the hostname at the given slot index, preserving any existing ID and last-seen timestamp.
    public void SetSlotHostname(int slotIndex, string hostname)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, this.MachineCount);
            var existing = _slots[slotIndex];
            _slots[slotIndex] = string.IsNullOrEmpty(hostname)
                ? null
                : new MachineEntry(hostname, existing?.Id ?? ID.NONE, existing?.LastSeenTick ?? 0);
        }
    }

    // Adds a machine to a slot if not already present.
    // Returns true and sets result to the new entry if added; returns false and sets result to the
    // existing entry if already known, or null if all slots are occupied.
    public bool TryAddMachine(string hostname, out MachineEntry? result)
    {
        if (hostname == null)
        {
            throw new ArgumentNullException(nameof(hostname));
        }
        else if (string.IsNullOrEmpty(machineName.Trim()))
        {
            throw new ArgumentException("machineName is empty", nameof(hostname));
        }

        lock (_lock)
        {
            CheckName(hostname);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is MachineEntry e && HostnamesAreEqual(e.Hostname, hostname))
                {
                    result = e;
                    return false; // already known
                }
            }

            // Find an empty slot.
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is null || string.IsNullOrEmpty(_slots[i]!.Hostname))
                {
                    result = new MachineEntry(hostname, ID.NONE, 0);
                    _slots[i] = result;
                    return true;
                }
            }

            // All slots are occupied.
            result = null;
            return false;
        }
    }

    internal void RemoveIdsFromEntries(bool firstLoaded, string localMachineName, Func<MachineEntry, bool> isAlive)
    {
        lock (_lock)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var entry = _slots[i];
                if (entry is null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.Hostname) && entry.Hostname.Equals(localMachineName, StringComparison.Ordinal))
                {
                    continue;
                }

                // All entries in _slots are by definition in the matrix, so !InMachineMatrix is always false.
                // On firstLoaded we therefore never reset; on subsequent calls we reset dead machines.
                if (!firstLoaded && !isAlive(entry))
                {
                    _slots[i] = new MachineEntry(entry.Hostname, ID.NONE, entry.LastSeenTick);
                }
            }
        }
    }
}
