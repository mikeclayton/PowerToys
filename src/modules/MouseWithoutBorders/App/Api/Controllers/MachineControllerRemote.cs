// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class MachineControllerRemote : MachineControllerBase
{
    public MachineControllerRemote(HttpClient httpClient)
        : base(httpClient, false)
    {
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/{machineId}/screens"
    /// </summary>
    /// <returns>
    /// Returns the screen setup on the local machine.
    /// </returns>
    [HttpGet]
    [Route("v1/screens")]
    public async Task<IActionResult> ScreensAsync([FromQuery] string securityKey)
    {
        return await this.ScreensBaseAsync(securityKey, null);
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/{machineId}/screens/{screenId}/screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/WEMBLEY/Screens/0/screenshot?width=512&height=144"
    /// Invoke-RestMethod "http://localhost:15102/v1/machines/DEL-ATFofRzOwRo/screenshot/0/Screenshot?width=512&height=144"
    /// </summary>
    /// <returns>
    /// Returns a screenshot of the specified local screen stretched to the requested width and height.
    /// </returns>
    [HttpGet]
    [Route("v1/screens/{screenId}/screenshot")]
    public async Task<IActionResult> ScreenshotAsync([FromQuery] string securityKey, int screenId, [FromQuery] int width, [FromQuery] int height)
    {
        return await this.ScreenshotBaseAsync(securityKey, null, screenId, width, height);
    }
}
