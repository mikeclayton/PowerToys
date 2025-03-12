// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MouseWithoutBorders.Api.Controllers;

namespace MouseWithoutBorders.Api;

internal sealed class ApiServer
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();

        // only listen on localhost
        builder.WebHost.ConfigureKestrel(
            options =>
            {
                options.ListenLocalhost(5002);
            });

        builder.Services.AddControllers();

        builder.Services.AddOpenApi();

        var app = builder.Build();

        // http://localhost:5002/openapi/v1.json
        app.MapOpenApi();

        app.MapControllers();

        await app.RunAsync(cancellationToken);
    }
}
