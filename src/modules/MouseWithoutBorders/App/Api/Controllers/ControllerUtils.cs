// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using MouseWithoutBorders.Api.Models;

namespace MouseWithoutBorders.Api.Controllers;

internal static class ControllerUtils
{
    public static void ValidateMachineId(string machineId)
    {
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        ArgumentNullException.ThrowIfNullOrWhiteSpace(machineId);
        machineId = machineId.Trim();
        if (string.IsNullOrEmpty(machineId))
        {
            throw new ArgumentException("Machine ID cannot be empty.", nameof(machineId));
        }

        foreach (char c in machineId)
        {
            if (!allowedChars.Contains(c))
            {
                throw new ArgumentException($"Machine ID contains an invalid character. Allowed characters are: {allowedChars}", nameof(machineId));
            }
        }
    }

    public static List<ScreenInfo> GetLocalScreens()
    {
        var localMonitorInfo = new List<Class.NativeMethods.MonitorInfoEx>();

        bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Class.NativeMethods.RECT lprcMonitor, IntPtr dwData)
        {
            var mi = default(Class.NativeMethods.MonitorInfoEx);
            mi.cbSize = Marshal.SizeOf(mi);
            _ = Class.NativeMethods.GetMonitorInfo(hMonitor, ref mi);
            localMonitorInfo.Add(mi);
            return true;
        }

        Class.NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

        const int MONITORINFOF_PRIMARY = 0x00000001;
        var screenInfo = localMonitorInfo.Select(
            (monitorInfo, index) => new ScreenInfo(
                id: index,
                primary: (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY,
                displayArea: new(
                    x: monitorInfo.rcMonitor.Left,
                    y: monitorInfo.rcMonitor.Top,
                    width: monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    height: monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top),
                workingArea: new(
                    x: monitorInfo.rcWork.Left,
                    y: monitorInfo.rcWork.Top,
                    width: monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                    height: monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top)))
            .ToList();

        return screenInfo;
    }
}
