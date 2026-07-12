// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Drawing;

using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Machines;

internal sealed class MoveResult
{
    internal MoveResult(int? lastX, int? lastY, ID oldDestinationMachineId, ID newDestinationMachineId, Point point)
    {
        this.LastX = lastX;
        this.LastY = lastY;
        this.OldDestinationMachineId = oldDestinationMachineId;
        this.NewDestinationMachineId = newDestinationMachineId;
        this.Point = point;
    }

    internal int? LastX
    {
        get;
    }

    internal int? LastY
    {
        get;
    }

    internal ID OldDestinationMachineId
    {
        get;
    }

    internal ID NewDestinationMachineId
    {
        get;
    }

    internal Point Point
    {
        get;
    }
}
