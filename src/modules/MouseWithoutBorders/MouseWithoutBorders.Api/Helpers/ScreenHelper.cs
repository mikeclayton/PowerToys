// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

using MouseWithoutBorders.Api.Models.Display;
using MouseWithoutBorders.Api.Models.Drawing;
using MouseWithoutBorders.Api.NativeMethods;

using static MouseWithoutBorders.Api.NativeMethods.Core;
using static MouseWithoutBorders.Api.NativeMethods.User32;

namespace MouseWithoutBorders.Api.Helpers;

public static class ScreenHelper
{
    public static IEnumerable<ScreenInfo> GetAllScreens()
    {
        // enumerate the monitors attached to the system
        var hMonitors = new List<HMONITOR>();
        var callback = new User32.MONITORENUMPROC(
            (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                hMonitors.Add(hMonitor);
                return true;
            });
        var result = User32.EnumDisplayMonitors(HDC.Null, LPCRECT.Null, callback, LPARAM.Null);
        if (!result)
        {
            throw new Win32Exception(
                result.Value,
                $"{nameof(User32.EnumDisplayMonitors)} failed with return code {result.Value}");
        }

        // get detailed info about each monitor
        var id = 0;
        foreach (var hMonitor in hMonitors)
        {
            var monitorInfoPtr = new LPMONITORINFO(
                new MONITORINFO((DWORD)MONITORINFO.Size, RECT.Empty, RECT.Empty, 0));
            result = User32.GetMonitorInfoW(hMonitor, monitorInfoPtr);
            if (!result)
            {
                throw new Win32Exception(
                    result.Value,
                    $"{nameof(User32.GetMonitorInfoW)} failed with return code {result.Value}");
            }

            var monitorInfo = monitorInfoPtr.ToStructure();
            monitorInfoPtr.Free();

            yield return new ScreenInfo(
                id: id,
                primary: monitorInfo.dwFlags.HasFlag(User32.MONITOR_INFO_FLAGS.MONITORINFOF_PRIMARY),
                displayArea: new RectangleInfo(
                    monitorInfo.rcMonitor.left,
                    monitorInfo.rcMonitor.top,
                    monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left,
                    monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top),
                workingArea: new RectangleInfo(
                    monitorInfo.rcWork.left,
                    monitorInfo.rcWork.top,
                    monitorInfo.rcWork.right - monitorInfo.rcWork.left,
                    monitorInfo.rcWork.bottom - monitorInfo.rcWork.top));
            id++;
        }
    }
}
