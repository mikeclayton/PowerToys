// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Class;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class MachineController : ControllerBase
{
    /// <summary>
    /// Invoke-RestMethod "http://localhost:5002/Machines/{machineId}/Screens"
    /// </summary>
    /// <returns>
    /// Returns the screen setup on the specified machine.
    /// </returns>
    [HttpGet]
    [Route("Machines/{machineId}/Screens")]
    public IActionResult Screens(string machineId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(machineId);

        // check if the machine id is in the machine matrix
        var stringComparer = StringComparison.OrdinalIgnoreCase;
        var matrixId = MachineStuff.MachineMatrix
            .Where(matrixId => !string.IsNullOrEmpty(matrixId))
            .FirstOrDefault(matrixId => string.Equals(matrixId, machineId, stringComparer));
        if (matrixId is null)
        {
            return BadRequest();
        }

        // check if it's the local machine
        if (string.Equals(matrixId, Environment.MachineName, stringComparer))
        {
            var localMonitorInfo = new List<NativeMethods.MonitorInfoEx>();

            bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData)
            {
                var mi = default(NativeMethods.MonitorInfoEx);
                mi.cbSize = Marshal.SizeOf(mi);
                _ = NativeMethods.GetMonitorInfo(hMonitor, ref mi);
                localMonitorInfo.Add(mi);
                return true;
            }

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

            const int MONITORINFOF_PRIMARY = 0x00000001;
            var screenInfo = localMonitorInfo.Select(
                (monitorInfo, index) => new ScreenInfo(
                    id: index,
                    primary: (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY,
                    displayArea: new(monitorInfo.rcMonitor),
                    workingArea: new(monitorInfo.rcWork)))
                .ToList();

            return Ok(screenInfo);
        }

        // must be a remote machine
        var remoteMonitorInfo = new List<NativeMethods.MonitorInfoEx>();
        return Ok(remoteMonitorInfo);
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:5002/Machines/{machineId}/Screens/{screenId}/Screenshot?width=400&height=300"
    /// </summary>
    /// <returns>
    /// Returns a screenshot of the specified screen stretched to the requested width and height.
    /// </returns>
    [HttpGet]
    [Route("Machines/{machineId}/Screens/{screenId}/Screenshot")]
    public IActionResult Screenshot(string machineId, int screenId, [FromQuery] int width, [FromQuery] int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.FillRectangle(Brushes.Blue, 0, 0, width, height);

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        byte[] imageBytes = ms.ToArray();

        return File(imageBytes, "image/png");
    }
}
