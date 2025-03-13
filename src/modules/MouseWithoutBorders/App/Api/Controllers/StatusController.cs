// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

[ApiController]
public class StatusController : ControllerBase
{
    public StatusController(HttpClient httpClient)
    {
        this.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private HttpClient HttpClient
    {
        get;
    }

    /// <summary>
    /// Invoke-RestMethod "http://localhost:15102/Status/ApiEnabled"
    /// </summary>
    /// <returns>
    /// Returns a HTTP 200 response if the api is enabled.
    /// Is unreachable if the api is not enabled.
    /// </returns>
    /// <remarks>
    /// External applications can use this endpoint as a way of checking if the api
    /// is enabled - a HTTP 200 means enabled, any other response means disabled.
    /// </remarks>
    [HttpGet]
    [Route("Status/ApiEnabled")]
    public IActionResult ApiEnabled()
    {
        return Ok();
    }
}
