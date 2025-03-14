// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Api.Imaging;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Api.Server;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class MachineController : ControllerBase
{
    public MachineController(HttpClient httpClient)
    {
        this.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private HttpClient HttpClient
    {
        get;
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/Machines/{machineId}/Screens"
    /// </summary>
    /// <returns>
    /// Returns the screen setup on the specified machine.
    /// </returns>
    [HttpGet]
    [Route("Machines/{machineId}/Screens")]
    public async Task<IActionResult> ScreensAsync(string machineId)
    {
        ControllerUtils.ValidateMachineId(machineId);

        // check if the machine id is in the machine matrix
        var stringComparer = StringComparison.OrdinalIgnoreCase;
        var matrixId = MachineStuff.MachineMatrix
            .Where(matrixId => !string.IsNullOrEmpty(matrixId))
            .FirstOrDefault(matrixId => string.Equals(matrixId, machineId, stringComparer));
        if (matrixId is null)
        {
            return NotFound();
        }

        // check if it's the local machine and use the winapi to get screen topology
        if (string.Equals(matrixId, Environment.MachineName, stringComparer))
        {
            var screenInfoList = ControllerUtils.GetLocalScreens();
            return Ok(screenInfoList);
        }

        // must be a remote machine - send a request to the remote api server
        try
        {
            var requestUrl = $"http://{machineId}:{RemoteApiServer.RemoteApiServerPort}/Machines/{machineId}/Screens";
            var responseJson = await this.HttpClient.GetStringAsync(requestUrl);
            var responseScreens = JsonSerializer.Deserialize<ScreenInfo[]>(responseJson)
                ?? throw new InvalidOperationException();
            return Ok(responseScreens);
        }
        catch
        {
            return Ok(Array.Empty<ScreenInfo>());
        }
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/Machines/{machineId}/Screens/{screenId}/Screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/Machines/WEMBLEY/Screens/0/Screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/Machines/DEL-ATFofRzOwRo/Screens/0/Screenshot?width=512&height=144"
    /// </summary>
    /// <returns>
    /// Returns a screenshot of the specified screen stretched to the requested width and height.
    /// </returns>
    [HttpGet]
    [Route("Machines/{machineId}/Screens/{screenId}/Screenshot")]
    public async Task<IActionResult> ScreenshotAsync(string machineId, int screenId, [FromQuery] int width, [FromQuery] int height)
    {
        ControllerUtils.ValidateMachineId(machineId);

        // check if the machine id is in the machine matrix
        var stringComparer = StringComparison.OrdinalIgnoreCase;
        var matrixId = MachineStuff.MachineMatrix
            .Where(matrixId => !string.IsNullOrEmpty(matrixId))
            .FirstOrDefault(matrixId => string.Equals(matrixId, machineId, stringComparer));
        if (matrixId is null)
        {
            return NotFound();
        }

        // check if it's the local machine and use the winapi to get a screenshot
        if (string.Equals(matrixId, Environment.MachineName, stringComparer))
        {
            var screenInfoList = ControllerUtils.GetLocalScreens();

            // make sure the screen id exists
            if (screenId < 0 || screenId >= screenInfoList.Count)
            {
                return NotFound();
            }

            // limit the size of valid screenshots
            var sourceDisplayArea = screenInfoList[screenId].DisplayArea;
            if ((width < 0) || (width > sourceDisplayArea.Width)
                || (height < 0) || (height > sourceDisplayArea.Height))
            {
                return BadRequest();
            }

            // generate the screenshot image
            using var targetBitmap = new Bitmap(width, height);
            using var targetGraphics = Graphics.FromImage(targetBitmap);
            var desktopCopyService = new DesktopImageRegionCopyService();
            desktopCopyService.CopyImageRegion(
                targetGraphics: targetGraphics,
                sourceBounds: new(sourceDisplayArea.X, sourceDisplayArea.Y, sourceDisplayArea.Width, sourceDisplayArea.Height),
                targetBounds: new(0, 0, width, height));

            // convert to a byte array and write the response
            using var ms = new MemoryStream();
            targetBitmap.Save(ms, ImageFormat.Png);
            byte[] imageBytes = ms.ToArray();
            return File(imageBytes, "image/png");
        }

        // must be a remote machine - send a request to the remote api server
        try
        {
            var requestUrl = $"http://{machineId}:{RemoteApiServer.RemoteApiServerPort}/Machines/{machineId}/Screens/{screenId}/Screenshot" +
                $"&width={width}" +
                $"&height={height}";
            var responseJson = await this.HttpClient.GetStringAsync(requestUrl);
            var responseScreens = JsonSerializer.Deserialize<ScreenInfo[]>(responseJson)
                ?? throw new InvalidOperationException();
            return Ok(responseScreens);
        }
        catch
        {
            return Ok(Array.Empty<ScreenInfo>());
        }
    }
}
