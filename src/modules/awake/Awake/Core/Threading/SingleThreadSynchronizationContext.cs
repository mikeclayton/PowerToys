﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Awake.Core.Threading
{
    internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<Tuple<SendOrPostCallback, object?>?> queue = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            ArgumentNullException.ThrowIfNull(d);

            lock (queue)
            {
                queue.Enqueue(Tuple.Create(d, state));
                Monitor.Pulse(queue);
            }
        }

        public void BeginMessageLoop()
        {
            while (true)
            {
                Tuple<SendOrPostCallback, object?>? work;
                lock (queue)
                {
                    while (queue.Count == 0)
                    {
                        Monitor.Wait(queue);
                    }

                    work = queue.Dequeue();
                }

                if (work == null)
                {
                    break;
                }

                try
                {
                    work.Item1(work.Item2);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during execution: " + e.Message);
                }
            }
        }

        public void EndMessageLoop()
        {
            lock (queue)
            {
                queue.Enqueue(null);  // Signal the end of the message loop
                Monitor.Pulse(queue);
            }
        }
    }
}