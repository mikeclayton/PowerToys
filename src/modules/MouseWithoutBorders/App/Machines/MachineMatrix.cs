// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;

namespace MouseWithoutBorders.Machines;

internal sealed partial class MachineMatrix
{
    private const int MAX_MACHINES = 4;

    private readonly Lock _lock = new();

    private MachineEntry?[] _slots;

    public MachineMatrix(int rowCount, int columnCount)
    {
        _slots = [];
        this.Resize(rowCount, columnCount);
    }

    /// <summary>
    /// Gets the number of rows in the matrix.
    /// Currently limited to 1 or 2.
    /// </summary>
    public int RowCount
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the number of columns in the matrix.
    /// Currently limited to 2 or 4, such that RowCount * ColumnCount = 4.
    /// </summary>
    public int ColumnCount
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the number of machines in the matrix (= RowCount * ColumnCount).
    /// Currently must always be equal to 4.
    /// </summary>
    public int MachineCount
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the cursor is able to move off
    /// the edge of the
    /// grid and wrap around to the machine on the opposite side.
    /// </summary>
    public bool WrapAround
    {
        get;
        set;
    }

    private static bool HostnamesAreEqual(string name1, string name2) =>
        string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);

    private static void CheckName(string machineName)
    {
        Debug.Assert(machineName != null, "machineName is null");
        Debug.Assert(machineName.Trim().Length == machineName.Length, "machineName contains spaces");
    }

    /// <summary>
    /// Resizes the matrix to the given dimensions, preserving existing entries where they fit.
    /// Currently limited to RowCount of 1 or 2, and RowCount * ColumnCount must equal 4.
    /// </summary>
    public void Resize(int rowCount, int columnCount)
    {
        // the settings ui only supports 1 or two rows at the moment
        if (rowCount is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be 1 or 2.");
        }

        // the settings ui only supports exactly 4 machines at the moment
        if (rowCount * columnCount != MAX_MACHINES)
        {
            throw new ArgumentException($"Row count multiplied by column count must equal {MAX_MACHINES}.");
        }

        lock (_lock)
        {
            var newSlots = new MachineEntry?[rowCount * columnCount];
            Array.Copy(_slots, newSlots, Math.Min(_slots.Length, newSlots.Length));
            _slots = newSlots;
            this.RowCount = rowCount;
            this.ColumnCount = columnCount;
            this.MachineCount = rowCount * columnCount;
        }
    }
}
