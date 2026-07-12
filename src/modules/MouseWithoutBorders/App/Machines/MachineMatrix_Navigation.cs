// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

using ID = MouseWithoutBorders.Core.ID;

namespace MouseWithoutBorders.Machines;

internal sealed partial class MachineMatrix
{
    /// <summary>
    /// Returns the flat slot index of the machine with the given <paramref name="id"/>,
    /// or <see langword="null"/> if not found.
    /// </summary>
    public int? GetSlotIndex(ID id)
    {
        if (id == ID.NONE)
        {
            return null;
        }

        lock (_lock)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] is MachineEntry entry && entry.Id == id)
                {
                    return i;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the slot index of the next connected machine to the right of <paramref name="slotIndex"/>
    /// in the same row, or <see langword="null"/> if none exists.
    /// Wraps to the start of the row if <see cref="WrapAround"/> is set.
    /// </summary>
    public int? GetSlotToRightOf(int slotIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, this.MachineCount);
            var rowStartIndex = slotIndex - (slotIndex % this.ColumnCount);
            var result = slotIndex + 1;
            if (result < rowStartIndex + this.ColumnCount)
            {
                return result;
            }

            if (this.WrapAround)
            {
                return rowStartIndex;
            }

            return null;
        }
    }

    /// <summary>
    /// Returns the slot index of the next connected machine to the left of <paramref name="slotIndex"/>
    /// in the same row, or <see langword="null"/> if none exists.
    /// Wraps to the end of the row if <see cref="WrapAround"/> is set.
    /// </summary>
    public int? GetSlotToLeftOf(int slotIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, this.MachineCount);
            var rowStartIndex = slotIndex - (slotIndex % this.ColumnCount);
            var result = slotIndex - 1;
            if (result >= rowStartIndex)
            {
                return result;
            }

            if (this.WrapAround)
            {
                return rowStartIndex + this.ColumnCount - 1;
            }

            return null;
        }
    }

    /// <summary>
    /// Returns the slot index of the connected machine directly above <paramref name="slotIndex"/>,
    /// or <see langword="null"/> if the slot above is empty.
    /// Wraps to the bottom row if <see cref="WrapAround"/> is set.
    /// On a single-row matrix with <see cref="WrapAround"/> enabled, returns the same slot index.
    /// </summary>
    public int? GetSlotAbove(int slotIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, this.MachineCount);
            var result = slotIndex - this.ColumnCount;
            if (result >= 0)
            {
                return result;
            }

            if (this.WrapAround)
            {
                return result + this.MachineCount;
            }

            return null;
        }
    }

    /// <summary>
    /// Returns the slot index of the connected machine directly below <paramref name="slotIndex"/>,
    /// or <see langword="null"/> if the slot below is empty.
    /// Wraps to the top row if <see cref="WrapAround"/> is set.
    /// On a single-row matrix with <see cref="WrapAround"/> enabled, returns the same slot index.
    /// </summary>
    public int? GetSlotBelow(int slotIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, this.MachineCount);
            var result = slotIndex + this.ColumnCount;
            if (result < this.MachineCount)
            {
                return result;
            }

            if (this.WrapAround)
            {
                return result - this.MachineCount;
            }

            return null;
        }
    }
}
