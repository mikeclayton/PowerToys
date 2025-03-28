// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Helpers;
using MouseWithoutBorders.Api.Models.Messages;
using MouseWithoutBorders.Api.Transport;
using MouseWithoutBorders.Api.Transport.Events;

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
            port: 12345);

        server.ClientConnected += (sender, e) =>
        {
            var client = e.ServerSession as ServerSession ?? throw new InvalidOperationException();
            client.MessageReceived += MwbApiServer.MessageReceived;
        };

        var task = Task.Run(
            () => server.StartServerAsync(cancellationToken).ConfigureAwait(false));

        try
        {
            // wait for the task to be cancelled
            await task;
        }
        catch (OperationCanceledException)
        {
            // swallow this exception
        }

        logger.LogInformation("Goodbye, World!");
    }

    private static void MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var session = sender as ServerSession ?? throw new InvalidOperationException();
        switch ((MessageType)e.Message.MessageType)
        {
            case MessageType.MachineMatrixRequest:
                /*
                var machineMatrix = MachineStuff.MachineMatrix
                    .Where(machine => !string.IsNullOrEmpty(machine))
                    .Select(machine => machine.Trim())
                    .ToList();
                */
                var machineMatrix = new List<string> { "aaa", "bbb" };
                var machineMatrixResponse = Message.ToMessage(
                    correlationId: e.Message.CorrelationId,
                    messageType: (int)MessageType.MachineMatrixResponse,
                    payload: new MachineMatrixResponse(machineMatrix));
                session.SendMessage(machineMatrixResponse);
                break;
            case MessageType.ScreenInfoRequest:
                var screenInfo = ScreenHelper.GetAllScreens().ToList();
                var screenInfoResponse = Message.ToMessage(
                    correlationId: e.Message.CorrelationId,
                    messageType: (int)MessageType.ScreenInfoResponse,
                    payload: new ScreenInfoResponse(screenInfo));
                session.SendMessage(screenInfoResponse);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
