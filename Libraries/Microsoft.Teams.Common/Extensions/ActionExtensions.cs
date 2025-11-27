// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Extensions;

public static class ActionExtensions
{
    /// <summary>
    /// Creates a debounced version of the action that delays execution until after the specified
    /// number of milliseconds have elapsed since the last invocation.
    /// </summary>
    /// <typeparam name="T">The type of the action parameter.</typeparam>
    /// <param name="func">The action to debounce.</param>
    /// <param name="milliseconds">The delay in milliseconds. Default is 300ms.</param>
    /// <returns>A debounced action.</returns>
    /// <remarks>
    /// The CancellationTokenSource is properly disposed when cancelled or when the debounce completes.
    /// </remarks>
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            var previousSource = cancelTokenSource;
            previousSource?.Cancel();
            previousSource?.Dispose();
            
            cancelTokenSource = new CancellationTokenSource();
            var currentToken = cancelTokenSource.Token;

            Task.Delay(milliseconds, currentToken)
                .ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }

    /// <summary>
    /// Creates a debounced version of the async function that delays execution until after the specified
    /// number of milliseconds have elapsed since the last invocation.
    /// </summary>
    /// <param name="func">The async function to debounce.</param>
    /// <param name="milliseconds">The delay in milliseconds. Default is 300ms.</param>
    /// <returns>A debounced action.</returns>
    /// <remarks>
    /// The CancellationTokenSource is properly disposed when cancelled or when the debounce completes.
    /// </remarks>
    public static Action Debounce(this Func<Task> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            var previousSource = cancelTokenSource;
            previousSource?.Cancel();
            previousSource?.Dispose();
            
            cancelTokenSource = new CancellationTokenSource();
            var currentToken = cancelTokenSource.Token;

            Task.Delay(milliseconds, currentToken)
                .ContinueWith(async t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        await func();
                    }
                }, TaskScheduler.Default);
        };
    }
}