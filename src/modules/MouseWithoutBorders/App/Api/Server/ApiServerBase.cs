// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace MouseWithoutBorders.Api.Server;

internal class ApiServerBase
{
    private CancellationTokenSource? CancellationTokenSource
    {
        get;
        set;
    }

    private JoinableTask? JoinableTask
    {
        get;
        set;
    }

    public void Start()
    {
        if (this.CancellationTokenSource != null)
        {
            throw new InvalidOperationException("The server is already running.");
        }

        // note, [STAThread] and "public async Task Main()" conflict with each other
        // so we have to do some additional dancing to avoid using "async Task" on "Main"
        // while still allowing async tasks below.
        var joinableTaskContext = new JoinableTaskContext();
        var joinableTaskFactory = new JoinableTaskFactory(joinableTaskContext);

        var cancellationTokenSource = new CancellationTokenSource();
        var joinableTask = joinableTaskFactory.RunAsync(async () =>
        {
            var apiServer = new LocalApiServer();
            await apiServer.RunAsync(cancellationTokenSource.Token);
        });

        this.JoinableTask = joinableTask;
    }

    public void Stop()
    {
        if (this.CancellationTokenSource == null)
        {
            throw new InvalidOperationException("The server is not running.");
        }

        this.CancellationTokenSource.Cancel();
        (this.JoinableTask ?? throw new InvalidOperationException()).Join();
    }

    protected virtual async Task RunAsync(CancellationToken cancellationToken)
    {
        // make the compiler happy that we're awaiting something in an async method
        await Task.CompletedTask;

        throw new NotImplementedException();
    }
}
