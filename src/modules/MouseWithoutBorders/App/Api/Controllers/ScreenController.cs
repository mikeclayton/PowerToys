// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Microsoft.AspNetCore.Mvc;
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
        return Ok(machineId);
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
