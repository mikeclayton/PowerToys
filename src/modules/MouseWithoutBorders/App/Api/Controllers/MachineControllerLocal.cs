// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class MachineControllerLocal : MachineControllerBase
{
    public MachineControllerLocal(HttpClient httpClient)
        : base(httpClient, true)
    {
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/{machineId}/screens"
    /// </summary>
    /// <returns>
    /// Returns the screen setup on the specified machine.
    /// If the machine is local it returns the local screen setup, otherwise
    /// it proxies a request to the remote api server to get the screen setup.
    /// </returns>
    [HttpGet]
    [Route("v1/machines/{machineId}/screens")]
    public async Task<IActionResult> ScreensAsync([FromQuery] string securityKey, string machineId)
    {
        return await this.ScreensBaseAsync(securityKey, machineId);
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/{machineId}/screens/{screenId}/screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/WEMBLEY/screens/0/Screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/DEL-ATFofRzOwRo/screens/0/screenshot?width=512&height=144"
    /// </summary>
    /// <returns>
    /// Returns a screenshot of the specified screen stretched to the requested width and height.
    /// If the machine is local it returns a local screenshot, otherwise
    /// it proxies a request to the remote api server to get the screenshot.
    /// </returns>
    [HttpGet]
    [Route("v1/machines/{machineId}/screens/{screenId}/screenshot")]
    public async Task<IActionResult> ScreenshotAsync([FromQuery] string securityKey, string machineId, int screenId, [FromQuery] int width, [FromQuery] int height)
    {
        return await this.ScreenshotBaseAsync(securityKey, machineId, screenId, width, height);
    }
}
