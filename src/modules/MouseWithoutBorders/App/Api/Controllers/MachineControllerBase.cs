// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Api.Imaging;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Api.Server;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class MachineControllerBase : ControllerBase
{
    public MachineControllerBase(HttpClient httpClient, bool allowRemote)
    {
        this.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.AllowRemote = allowRemote;
    }

    private HttpClient HttpClient
    {
        get;
    }

    private bool AllowRemote
    {
        get;
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/Machines/{machineId}/Screens"
    /// </summary>
    /// <returns>
    /// Returns the screen setup on the specified machine.
    /// If the machine is local it returns the local screen setup, otherwise
    /// it makes a request to the remote api server to get the screen setup.
    /// </returns>
    protected async Task<IActionResult> ScreensBaseAsync(string securityKey, string? machineId)
    {
        // verify the security key
        if (!ControllerUtils.TryValidateSecurityKey(securityKey, out var securityKeyResponse))
        {
            return securityKeyResponse;
        }

        // check if it's the local machine and use the winapi to get screen topology
        if (machineId is null)
        {
            var screenInfoList = ControllerUtils.GetLocalScreens();
            return Ok(screenInfoList);
        }

        // it's a remote machine - check we're allowed to make a call to a remote api server
        if (!this.AllowRemote)
        {
            return StatusCode(
                (int)HttpStatusCode.Forbidden, "Remote access is disabled.");
        }

        // check the machine id is in the machine matrix
        if (!ControllerUtils.TryValidateMatrixId(machineId, out var matrixIdResponse))
        {
            return matrixIdResponse;
        }

        // send a request to the remote api server
        try
        {
            var requestUrl = $"http://{machineId}:{RemoteApiServer.RemoteApiServerPort}/v1/screens" +
                $"?scurityKey={securityKey}";
            var responseJson = await this.HttpClient.GetStringAsync(requestUrl);
            var responseScreens = JsonSerializer.Deserialize<ScreenInfo[]>(responseJson)
                ?? throw new InvalidOperationException();
            return Ok(responseScreens);
        }
        catch (HttpRequestException ex)
        {
            var errorCode = ex.StatusCode.HasValue ? ex.StatusCode.Value.ToString() : "Unknown";
            var errorMessage = $"Error Code: {errorCode}, Message: {ex.Message}";
            return StatusCode((int)HttpStatusCode.BadRequest, errorMessage);
        }
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/Machines/{machineId}/Screens/{screenId}/Screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/Machines/WEMBLEY/Screens/0/Screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/Machines/DEL-ATFofRzOwRo/Screens/0/Screenshot?width=512&height=144"
    /// </summary>
    /// <returns>
    /// Returns a screenshot of the specified screen stretched to the requested width and height.
    /// If the machine is local it returns a screenshot from the local screen, otherwise
    /// it makes a request to the remote api server to get the screenshot.
    /// </returns>
    protected async Task<IActionResult> ScreenshotBaseAsync(string securityKey, string? machineId, int screenId, int width, int height)
    {
        // verify the security key
        if (!ControllerUtils.TryValidateSecurityKey(securityKey, out var securityKeyResponse))
        {
            return securityKeyResponse;
        }

        // if it's the local machine use the winapi to get a screenshot
        if (machineId is null)
        {
            var screenInfoList = ControllerUtils.GetLocalScreens();

            // make sure the screen id exists
            if (screenId < 0 || screenId >= screenInfoList.Count)
            {
                return NotFound(
                    new { Message = "The specified screen does not exist." });
            }

            // limit the size of valid screenshots
            var sourceDisplayArea = screenInfoList[screenId].DisplayArea;
            if ((width < 0) || (width > sourceDisplayArea.Width)
                || (height < 0) || (height > sourceDisplayArea.Height))
            {
                return NotFound(
                    new { Message = "Invalid screenshot image dimensions." });
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

        // it's a remote machine - check we're allowed to make a call to a remote api server
        if (!this.AllowRemote)
        {
            return StatusCode(
                (int)HttpStatusCode.Forbidden, "Remote access is disabled.");
        }

        // check the machine id is in the machine matrix
        if (!ControllerUtils.TryValidateMatrixId(machineId, out var matrixIdResponse))
        {
            return matrixIdResponse;
        }

        // send a request to the remote api server
        try
        {
            var requestUrl = $"http://{machineId}:{RemoteApiServer.RemoteApiServerPort}/v1/screens/{screenId}/screenshot" +
                $"?securityKey={securityKey}" +
                $"&width={width}" +
                $"&height={height}";
            var responseJson = await this.HttpClient.GetStringAsync(requestUrl);
            var responseScreens = JsonSerializer.Deserialize<ScreenInfo[]>(responseJson)
                ?? throw new InvalidOperationException();
            return Ok(responseScreens);
        }
        catch (HttpRequestException ex)
        {
            var errorCode = ex.StatusCode.HasValue ? ex.StatusCode.Value.ToString() : "Unknown";
            var errorMessage = $"Error Code: {errorCode}, Message: {ex.Message}";
            return StatusCode((int)HttpStatusCode.BadRequest, errorMessage);
        }
        catch (TimeoutException)
        {
            var errorMessage = $"Operation timed out";
            return StatusCode((int)HttpStatusCode.BadRequest, errorMessage);
        }
    }
}
