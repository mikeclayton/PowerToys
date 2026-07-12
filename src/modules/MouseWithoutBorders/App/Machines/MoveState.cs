// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using ID = MouseWithoutBorders.Core.ID;
using MyRectangle = MouseWithoutBorders.Core.MyRectangle;

namespace MouseWithoutBorders.Machines;

/// <summary>
/// Captures a snapshot of the global state values from the main application that
/// are referenced in the Move* functions. This decouples us from the global state
/// and lets Move* members be functionally pure - i.e. don't reference global state
/// and don't cause side effects. this makes writing unit tests *much* easier.
/// </summary>
internal sealed class MoveState
{
    public MoveState(
        long timestamp,
        long lastJumpTick,
        IEnumerable<Point> sensitivePoints,
        MachineMatrix machineMatrix,
        ID thisMachineId,
        bool moveMouseRelatively,
        MyRectangle desktopBounds,
        MyRectangle primaryScreenBounds)
    {
        this.Timestamp = timestamp;
        this.LastJumpTick = lastJumpTick;
        this.SensitivePoints = (sensitivePoints ?? throw new ArgumentNullException(nameof(sensitivePoints))).ToList().AsReadOnly();
        this.MachineMatrix = machineMatrix ?? throw new ArgumentNullException(nameof(machineMatrix));
        this.ThisMachineId = thisMachineId;
        this.MoveMouseRelatively = moveMouseRelatively;
        this.DesktopBounds = desktopBounds ?? throw new ArgumentNullException(nameof(desktopBounds));
        this.PrimaryScreenBounds = primaryScreenBounds ?? throw new ArgumentNullException(nameof(primaryScreenBounds));
    }

    /// <summary>
    /// Gets the value returned by Common.Ticks() at the point when this
    /// MoveState object was created.
    /// </summary>
    internal long Timestamp
    {
        get;
    }

    /// <summary>
    /// Gets the timestamp of the last time the active machine was changed
    /// (as set by PrepareToSwitchToMachine).
    /// </summary>
    internal long LastJumpTick
    {
        get;
    }

    /// <summary>
    /// Gets a list of points that are excluded from mouse movement.
    /// If the mouse is within 100 pixels of any of these points, the mouse will not be moved to a neighboring machine.
    /// </summary>
    /// <remarks>
    /// The caller should only set these points if they need to be considered - for example if
    /// Setting.Values.BlockMouseAtCorners us *disabled* the caller should *not* populate the collection.
    /// </remarks>
    internal ReadOnlyCollection<Point> SensitivePoints
    {
        get;
        init;
    }

    /// <summary>
    /// Gets the machine matrix that describes the layout of the machines in the network.
    /// </summary>
    internal MachineMatrix MachineMatrix
    {
        get;
    }

    /// <summary>
    /// Gets the ID for the local "controller" machine this code is running on.
    /// </summary>
    internal ID ThisMachineId
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the mouse cursor position when switching machines
    /// should be adjusted to match the relative position on the new machine,
    /// or whether it should jump to a fixed position.
    /// </summary>
    internal bool MoveMouseRelatively
    {
        get;
    }

    /// <summary>
    /// Gets the coordinates of the entire desktop of the local computer.
    /// </summary>
    /// <remarks>
    /// Could contain negative coordinates if the screen layout for multiple
    /// monitors includes monitors that are above or left of the primary monitor.
    /// </remarks>
    internal MyRectangle DesktopBounds
    {
        get;
    }

    /// <summary>
    /// Gets the coordinates of the primary monitor.
    /// </summary>
    internal MyRectangle PrimaryScreenBounds
    {
        get;
    }
}
