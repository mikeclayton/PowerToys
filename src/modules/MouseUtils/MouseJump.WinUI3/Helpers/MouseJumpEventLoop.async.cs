// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;

using Microsoft.UI.Dispatching;

namespace MouseJump.WinUI3.Helpers;

internal static partial class MouseJumpEventLoop
{
    /// <summary>
    /// Based on NativeEventWaiter.WaitForEventLoop,
    /// but takes an async callback.
    /// </summary>
    public static void RunAsyncEventHandler(string eventName, Func<Task> callback, DispatcherQueue dispatcherQueue, CancellationToken cancel)
    {
        Logger.LogDebug($"starting event handler for event '{eventName}'");

        // propagates the outcome of the original callback onto the task we returned to the caller
        static void CompleteFromCallbackTask(Task callbackTask, TaskCompletionSource tcs)
        {
            if (callbackTask.IsFaulted)
            {
                tcs.SetException(callbackTask.Exception!.InnerExceptions);
            }
            else if (callbackTask.IsCanceled)
            {
                tcs.SetCanceled(CancellationToken.None);
            }
            else
            {
                tcs.SetResult();
            }
        }

        // runs on the ui thread - starts the callback, then arranges for its outcome to be copied
        // across when it completes. this returns as soon as the callback reaches its first await,
        // so the ui thread isn't blocked while the callback runs
        static void StartCallback(Func<Task> callback, TaskCompletionSource tcs)
        {
            try
            {
                _ = callback().ContinueWith(
                    t => CompleteFromCallbackTask(t, tcs),
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        // queues the callback on the dispatcher (ui thread) and returns a task that completes when
        // the callback's own task does. DispatcherQueue.TryEnqueue can only tell us whether the
        // work was *queued*, not when it finishes, so we complete this one by hand instead -
        // either when the callback itself completes, or straight away if it can't even be started
        static Task InvokeOnDispatcherAsync(Func<Task> callback, DispatcherQueue dispatcherQueue)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcherQueue.TryEnqueue(
                () => StartCallback(callback, tcs)))
            {
                // the dispatcher is shutting down, so StartCallback will never run
                tcs.SetException(new InvalidOperationException("DispatcherQueue is not accepting work items."));
            }

            return tcs.Task;
        }

        // queues the callback on the dispatcher (ui thread) and blocks until it finishes
        static void InvokeOnDispatcherAndWait(string eventName, Func<Task> callback, DispatcherQueue dispatcherQueue)
        {
            try
            {
                Logger.LogDebug($"[{eventName}] - invoking callback");
                InvokeOnDispatcherAsync(callback, dispatcherQueue).GetAwaiter().GetResult(); // block until callback completes
            }
            catch (Exception ex)
            {
                // if something went wrong the application could be in an unstable state.
                // the main MouseJump module will restart the background process the next
                // time the hotkey is invoked, so just rethrow the exception here.
                Logger.LogDebug($"[{eventName}] - error occurred");
                Logger.LogError(ex.ToString());
                throw;
            }
        }

        // runs on the event loop thread - invokes the callback every time the specified
        // event is signalled, until cancel is signalled
        static void RunLoop(string eventName, Func<Task> callback, DispatcherQueue dispatcherQueue, CancellationToken cancel)
        {
            using var eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
            var waitHandles = new WaitHandle[] { cancel.WaitHandle, eventHandle };
            while (true)
            {
                Logger.LogDebug($"[{eventName}] - entering event handler loop");
                if (WaitHandle.WaitAny(waitHandles) != 1)
                {
                    Logger.LogDebug($"[{eventName}] - exiting event handler loop");
                    return;
                }

                InvokeOnDispatcherAndWait(eventName, callback, dispatcherQueue);
            }
        }

        var thread = new Thread(
            () => RunLoop(eventName, callback, dispatcherQueue, cancel));
        thread.Name = "MouseJumpEventLoopThread";
        thread.IsBackground = true;

        thread.Start();
    }
}
