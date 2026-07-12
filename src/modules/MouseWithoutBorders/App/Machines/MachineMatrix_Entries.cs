// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using ID = MouseWithoutBorders.Core.ID;

namespace MouseWithoutBorders.Machines;

internal sealed partial class MachineMatrix
{
    internal void RemoveAllEntries()
    {
        lock (_lock)
        {
            Array.Clear(_slots);
        }
    }

    /// <summary>
    /// Returns all non-empty slots as a list of <see cref="MachineEntry"/> instances.
    /// </summary>
    internal List<MachineEntry> GetAllEntries()
    {
        lock (_lock)
        {
            return _slots
                .Where(s => s is not null)
                .Cast<MachineEntry>()
                .ToList();
        }
    }

    /// <summary>
    /// Returns a positional snapshot of all slots, including empty (<see langword="null"/>) ones.
    /// </summary>
    internal MachineEntry?[] GetSlotSnapshot()
    {
        lock (_lock)
        {
            return (MachineEntry?[])_slots.Clone();
        }
    }

    /// <summary>
    /// Gets the <see cref="MachineEntry"/> at the given slot index, or <see langword="null"/> if the slot is empty.
    /// </summary>
    public MachineEntry? this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            lock (_lock)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.MachineCount);
                return _slots[index];
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="MachineEntry"/> at the given row and column index, or <see langword="null"/> if the slot is empty.
    /// </summary>
    public MachineEntry? this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            lock (_lock)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, this.RowCount);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, this.ColumnCount);
                return _slots[(row * this.ColumnCount) + column];
            }
        }
    }

    /// <summary>
    /// Returns the <see cref="MachineEntry"/> with the given <paramref name="id"/>, or <see langword="null"/> if there isn't one.
    /// </summary>
    public MachineEntry? GetEntryById(ID id)
    {
        lock (_lock)
        {
            foreach (var slot in _slots)
            {
                if (slot is MachineEntry entry && entry.Id == id)
                {
                    return entry;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns <see langword="true"/> and sets <paramref name="entry"/> if a machine with the given <paramref name="id"/> exists;
    /// otherwise returns <see langword="false"/> and sets <paramref name="entry"/> to <see langword="null"/>.
    /// </summary>
    public bool TryEntryGetById(ID id, out MachineEntry? entry)
    {
        lock (_lock)
        {
            foreach (var slot in _slots)
            {
                if (slot is MachineEntry candidate && candidate.Id == id)
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    /// <summary>
    /// Returns the <see cref="MachineEntry"/> with the given <paramref name="hostname"/>, or <see langword="null"/> if there isn't one.
    /// </summary>
    public MachineEntry? GetEntryByHostname(string hostname)
    {
        CheckName(hostname);
        lock (_lock)
        {
            foreach (var item in _slots)
            {
                if (item is not null && HostnamesAreEqual(item.Hostname, hostname))
                {
                    return item;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns <see langword="true"/> and sets <paramref name="entry"/> if a machine with the given <paramref name="hostname"/> exists;
    /// otherwise returns <see langword="false"/> and sets <paramref name="entry"/> to <see langword="null"/>.
    /// </summary>
    public bool TryGetEntryByHostname(string hostname, out MachineEntry? entry)
    {
        CheckName(hostname);
        lock (_lock)
        {
            foreach (var item in _slots)
            {
                if (item is not null && HostnamesAreEqual(item.Hostname, hostname))
                {
                    entry = item;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }
}
