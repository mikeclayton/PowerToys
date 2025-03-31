// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Helpers;
using MouseWithoutBorders.Api.Models.Messages;
using MouseWithoutBorders.Api.Transport;
using MouseWithoutBorders.Core;
using Message = MouseWithoutBorders.Api.Models.Messages.Message;

#pragma warning disable CA1848 // Use the LoggerMethod delegates
namespace MouseWithoutBorders.Api;

public static class MwbApiServer
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
        });
        var logger = loggerFactory.CreateLogger<Message>();

        logger.LogInformation("Hello, World!");

        var server = new ServerEndpoint(
            logger: logger,
            name: "server",
            address: IPAddress.Loopback,
            port: 12345,
            callback: MwbApiServer.ReceiveMessageAsync);

        var serverTask = Task.Run(
            () => server.StartServerAsync(cancellationToken).ConfigureAwait(false));

        try
        {
            // wait for the task to be cancelled
            await serverTask;
        }
        catch (OperationCanceledException)
        {
            // swallow this exception
        }

        logger.LogInformation("Goodbye, World!");
    }

    private static async Task ReceiveMessageAsync(ServerEndpoint server, ServerSession session, Message message, CancellationToken cancellationToken)
    {
        // process the message
        switch ((MessageType)message.MessageType)
        {
            case MessageType.MachineMatrixRequest:
                var machineMatrix = MachineStuff.MachineMatrix
                    .Where(machine => !string.IsNullOrEmpty(machine))
                    .Select(machine => machine.Trim())
                    .ToList();
                var machineMatrixResponse = Message.ToMessage(
                    correlationId: message.CorrelationId,
                    messageType: (int)MessageType.MachineMatrixResponse,
                    payload: new MachineMatrixResponse(machineMatrix));
                await session.SendMessageAsync(machineMatrixResponse, cancellationToken);
                break;
            case MessageType.ScreenInfoRequest:
                var screenInfo = ScreenHelper.GetAllScreens().ToList();
                var screenInfoResponse = Message.ToMessage(
                    correlationId: message.CorrelationId,
                    messageType: (int)MessageType.ScreenInfoResponse,
                    payload: new ScreenInfoResponse(screenInfo));
                await session.SendMessageAsync(screenInfoResponse, cancellationToken);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
