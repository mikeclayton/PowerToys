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

namespace MouseWithoutBorders.Api.Server;

internal sealed class RemoteApiServer : ApiServerBase
{
    public const int RemoteApiServerPort = 15103;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();

        // add a httpclient that can be accessed by all controllers
        builder.Services.AddHttpClient("DefaultHttpClient", client =>
        {
            client.Timeout = TimeSpan.FromMilliseconds(250);
        });

        // listen on all ip addresses
        builder.WebHost.ConfigureKestrel(
            options =>
            {
                options.ListenAnyIP(RemoteApiServer.RemoteApiServerPort);
            });

        // specify the controller types to register
        // (we don't want to register all controllers in the assembly)
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(apm =>
            {
                apm.FeatureProviders.Add(
                    new CustomControllerFeatureProvider(
                        [
                            typeof(MachineController),
                            typeof(MachineStuffController),
                            typeof(StatusController),
                        ]));
            });

        builder.Services.AddOpenApi();

        var app = builder.Build();

        // http://localhost:5002/openapi/v1.json
        app.MapOpenApi();

        app.MapControllers();

        await app.RunAsync(cancellationToken);
    }
}
