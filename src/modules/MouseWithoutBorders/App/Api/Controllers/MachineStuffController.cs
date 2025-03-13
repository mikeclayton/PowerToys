// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class MachineStuffController : ControllerBase
{
    public MachineStuffController(HttpClient httpClient)
    {
        this.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private HttpClient HttpClient
    {
        get;
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:5002/MachineStuff/MachineMatrix"
    /// </summary>
    /// <returns>
    /// Returns the list of connected machines in the current MachineMatrix.
    /// </returns>
    [HttpGet]
    public IActionResult MachineMatrix()
    {
        var machineMatrix = MachineStuff.MachineMatrix
            .Where(matrixId => !string.IsNullOrEmpty(matrixId))
            .ToList();
        return Ok(machineMatrix);
    }
}
