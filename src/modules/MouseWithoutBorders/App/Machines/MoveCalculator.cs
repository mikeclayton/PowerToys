// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Drawing;

// import specific Core types so we know what we're referencing
// (this will help us avoid accidentally referencing static properties of Common, MachineStuff, etc)
using ID = MouseWithoutBorders.Core.ID;
using Logger = MouseWithoutBorders.Core.Logger;
using MyRectangle = MouseWithoutBorders.Core.MyRectangle;

namespace MouseWithoutBorders.Machines;

internal static class MoveCalculator
{
    private const int JUMP_TRIGGER_PIXELS = 1;  // how close to the screen edge triggers a jump to the next machine
    private const int JUMP_OFFSET_PIXELS = 2;   // how far from the screen edge the cursor lands on the target machine

    // Returns the rectangle used as the source coordinate space when normalising the mouse position
    // to 0-65535 via ConvertToUniversalValue. The perpendicular axis position (e.g. y when moving
    // left/right) is expressed as a fraction of this rectangle, then mapped to the remote machine's
    // full desktop — so the choice of scaling bounds determines how faithfully that position is
    // preserved on the target machine.
    //
    // Use DesktopBounds (full desktop) when moving relatively, or when switching FROM the controller
    // machine (its cursor isn't locked to the primary screen). In all other cases use
    // PrimaryScreenBounds, which maps from the controller's primary screen across the remote's
    // full desktop.
    private static MyRectangle GetScalingBounds(MoveState moveState, ID desMachineId, ID newDesMachineId)
    {
        return moveState.MoveMouseRelatively || (desMachineId == moveState.ThisMachineId && newDesMachineId != moveState.ThisMachineId)
            ? moveState.DesktopBounds
            : moveState.PrimaryScreenBounds;
    }

    private static Point ConvertToUniversalValue(Point p, MyRectangle r)
    {
        if (!p.IsEmpty)
        {
            p.X = (p.X - r.Left) * 65535 / (r.Right - r.Left);
            p.Y = (p.Y - r.Top) * 65535 / (r.Bottom - r.Top);
        }

        return p;
    }

    /* Let's say we have 3 machines A, B, and C. A is the controller machine.
     * (x, y) is the current Mouse position in pixel.
     * If Setting.Values.MoveMouseRelatively then (x, y) can be from any machine having the value bounded by desktopBounds (can be negative)
     * Else (x, y) is from the controller machine which is bounded by ONLY primaryScreenBounds (>=0);
     *
     * The return point is from 0 to 65535 which is then mapped to the desktop of the new controlled machine by the SendInput method.
     *  Let's say user is switching from machine B to machine C:
     *      If Setting.Values.MoveMouseRelatively the this method is called by B and the return point is calculated by B and sent back to A, A will use it to move Mouse to the right position when switching to C.
     *      Else this method is called by A and the return point is calculated by A.
     * */

    internal static MoveResult MoveToMyNeighbourIfNeeded(MoveState moveState, int x, int y, ID desMachineId)
    {
        ArgumentNullException.ThrowIfNull(moveState);

        // for compatibility with existing code, work out what side-effect to apply to
        // Common.LastX and Common.LastY so we can return them in the MoveResult. this
        // decouples us from the global state but still lets us send a signal to the
        // caller that the values need to updated. (it makes more sense for the caller
        // to do this, but we're refactoring like-for-like at the moment - we're revisit)
        var lastX = (Math.Abs(x) > 10) ? x : (int?)null;
        var lastY = (Math.Abs(y) > 10) ? y : (int?)null;

        // don't process if we changed computer very recently, or if the destination machine is ALL
        // (which is a special case that means "don't move")
        if ((moveState.Timestamp - moveState.LastJumpTick < 100) || desMachineId == ID.ALL)
        {
            return new(lastX, lastY, desMachineId, ID.NONE, Point.Empty);
        }

        // don't move if we're near a "sensitive point" - this is typically the corners of each monitor.
        // the caller will leave this collection empty if sensitive points should be ignored
        foreach (var sensitivePoint in moveState.SensitivePoints)
        {
            if (Math.Abs(sensitivePoint.X - x) < 100 && Math.Abs(sensitivePoint.Y - y) < 100)
            {
                return new(lastX, lastY, desMachineId, ID.NONE, Point.Empty);
            }
        }

        /* If Mouse is moving in the controller machine and this method is called by the controller machine.
         * Or if Mouse is moving in the controlled machine and this method is called by the controlled machine and Setting.Values.MoveMouseRelative.
         * */
        if (desMachineId == moveState.ThisMachineId)
        {
            if (x < moveState.DesktopBounds.Left + MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveLeft(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (x >= moveState.DesktopBounds.Right - MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveRight(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (y < moveState.DesktopBounds.Top + MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveUp(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (y >= moveState.DesktopBounds.Bottom - MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveDown(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
        }

        /* If Mouse is moving in the controlled machine and this method is called by the controller machine and !Setting.Values.MoveMouseRelative.
         * Mouse location is scaled from the primary screen bound of the controller machine regardless of how many monitors the controlled machine may have.
         * */
        else
        {
            if (x < moveState.PrimaryScreenBounds.Left + MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveLeft(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (x >= moveState.PrimaryScreenBounds.Right - MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveRight(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (y < moveState.PrimaryScreenBounds.Top + MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveUp(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
            else if (y >= moveState.PrimaryScreenBounds.Bottom - MoveCalculator.JUMP_TRIGGER_PIXELS)
            {
                var moveTo = MoveCalculator.MoveDown(moveState, x, y, desMachineId);
                return new(lastX, lastY, desMachineId, moveTo.NewDestinationMachineId, moveTo.Point);
            }
        }

        return new(lastX, lastY, desMachineId, ID.NONE, Point.Empty);
    }

    private static (ID NewDestinationMachineId, Point Point) MoveRight(MoveState moveState, int x, int y, ID desMachineId)
    {
        var currentSlot = moveState.MachineMatrix.GetSlotIndex(desMachineId);
        if (currentSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var targetSlot = moveState.MachineMatrix.GetSlotToRightOf(currentSlot.Value);
        if (targetSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var newDesMachineIdEx = moveState.MachineMatrix[targetSlot.Value]!.Id;
        Logger.LogDebug("Move Right");

        var bounds = MoveCalculator.GetScalingBounds(moveState, desMachineId, newDesMachineIdEx);
        var point = MoveCalculator.ConvertToUniversalValue(new Point(bounds.Left + JUMP_OFFSET_PIXELS, y), bounds);

        return (newDesMachineIdEx, point);
    }

    private static (ID NewDestinationMachineId, Point Point) MoveLeft(MoveState moveState, int x, int y, ID desMachineId)
    {
        var currentSlot = moveState.MachineMatrix.GetSlotIndex(desMachineId);
        if (currentSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var targetSlot = moveState.MachineMatrix.GetSlotToLeftOf(currentSlot.Value);
        if (targetSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var newDesMachineIdEx = moveState.MachineMatrix[targetSlot.Value]!.Id;
        Logger.LogDebug("Move Left");

        var bounds = MoveCalculator.GetScalingBounds(moveState, desMachineId, newDesMachineIdEx);
        var point = MoveCalculator.ConvertToUniversalValue(new Point(bounds.Right - JUMP_OFFSET_PIXELS, y), bounds);

        return (newDesMachineIdEx, point);
    }

    private static (ID NewDestinationMachineId, Point Point) MoveUp(MoveState moveState, int x, int y, ID desMachineId)
    {
        var currentSlot = moveState.MachineMatrix.GetSlotIndex(desMachineId);
        if (currentSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var targetSlot = moveState.MachineMatrix.GetSlotAbove(currentSlot.Value);
        if (targetSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var newDesMachineIdEx = moveState.MachineMatrix[targetSlot.Value]!.Id;
        Logger.LogDebug("Move Up");

        var bounds = MoveCalculator.GetScalingBounds(moveState, desMachineId, newDesMachineIdEx);
        var point = MoveCalculator.ConvertToUniversalValue(new Point(x, bounds.Bottom - JUMP_OFFSET_PIXELS), bounds);

        return (newDesMachineIdEx, point);
    }

    private static (ID NewDestinationMachineId, Point Point) MoveDown(MoveState moveState, int x, int y, ID desMachineId)
    {
        var currentSlot = moveState.MachineMatrix.GetSlotIndex(desMachineId);
        if (currentSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var targetSlot = moveState.MachineMatrix.GetSlotBelow(currentSlot.Value);
        if (targetSlot is null)
        {
            return (ID.NONE, Point.Empty);
        }

        var newDesMachineIdEx = moveState.MachineMatrix[targetSlot.Value]!.Id;
        Logger.LogDebug("Move Down");

        var bounds = MoveCalculator.GetScalingBounds(moveState, desMachineId, newDesMachineIdEx);
        var point = MoveCalculator.ConvertToUniversalValue(new Point(x, bounds.Top + JUMP_OFFSET_PIXELS), bounds);

        return (newDesMachineIdEx, point);
    }
}
