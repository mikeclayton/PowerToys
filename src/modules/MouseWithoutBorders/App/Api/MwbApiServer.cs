// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Api.Models.Display;
using MouseWithoutBorders.Api.Models.Messages;
using MouseWithoutBorders.Api.Transport;
using MouseWithoutBorders.Api.Transport.Events;
using MouseWithoutBorders.Core;

#nullable enable

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
                var machineMatrix = MachineStuff.MachineMatrix
                    .Where(machine => !string.IsNullOrEmpty(machine))
                    .Select(machine => machine.Trim())
                    .ToList();
                var machineMatrixResponse = Message.ToMessage(
                    correlationId: e.Message.CorrelationId,
                    messageType: (int)MessageType.MachineMatrixResponse,
                    payload: new MachineMatrixResponse(machineMatrix));
                session.SendMessage(machineMatrixResponse);
                break;
            case MessageType.ScreenInfoRequest:
                var screenInfo = MwbApiServer.GetLocalScreens();
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

    public static List<ScreenInfo> GetLocalScreens()
    {
        var localMonitorInfo = new List<Class.NativeMethods.MonitorInfoEx>();

        bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Class.NativeMethods.RECT lprcMonitor, IntPtr dwData)
        {
            var mi = default(Class.NativeMethods.MonitorInfoEx);
            mi.cbSize = Marshal.SizeOf(mi);
            _ = Class.NativeMethods.GetMonitorInfo(hMonitor, ref mi);
            localMonitorInfo.Add(mi);
            return true;
        }

        Class.NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

        const int MONITORINFOF_PRIMARY = 0x00000001;
        var screenInfo = localMonitorInfo.Select(
            (monitorInfo, index) => new ScreenInfo(
                id: index,
                primary: (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY,
                displayArea: new(
                    x: monitorInfo.rcMonitor.Left,
                    y: monitorInfo.rcMonitor.Top,
                    width: monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    height: monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top),
                workingArea: new(
                    x: monitorInfo.rcWork.Left,
                    y: monitorInfo.rcWork.Top,
                    width: monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                    height: monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top)))
            .ToList();

        return screenInfo;
    }
}
