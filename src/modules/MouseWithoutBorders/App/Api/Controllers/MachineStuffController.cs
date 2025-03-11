// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class MachineStuffController : ControllerBase
{
    /// <summary>
    /// Invoke-RestMethod "http://localhost:5002/MachineStuff/MachineMatrix"
    /// </summary>
    /// <returns>
    /// Returns the list of connected machines in the current MachineMatrix.
    /// </returns>
    [HttpGet]
    public IActionResult MachineMatrix()
    {
        return Ok(MachineStuff.MachineMatrix);
    }
}
