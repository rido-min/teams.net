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
    /// Uses Interlocked.Exchange for thread-safe source replacement.
    /// </remarks>
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            var newSource = new CancellationTokenSource();
            var previousSource = Interlocked.Exchange(ref cancelTokenSource, newSource);
            
            // Cancel and dispose the previous source safely
            if (previousSource != null)
            {
                try
                {
                    previousSource.Cancel();
                }
                finally
                {
                    previousSource.Dispose();
                }
            }
            
            var currentToken = newSource.Token;

            Task.Delay(milliseconds, currentToken)
                .ContinueWith(t =>
                {
                    try
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            func(arg);
                        }
                    }
                    finally
                    {
                        // Atomically try to set cancelTokenSource to null only if it's still newSource.
                        // CompareExchange returns the original value, so if it returns newSource,
                        // we successfully cleared it and should dispose.
                        if (Interlocked.CompareExchange(ref cancelTokenSource, null, newSource) == newSource)
                        {
                            newSource.Dispose();
                        }
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
    /// Uses Interlocked.Exchange for thread-safe source replacement.
    /// </remarks>
    public static Action Debounce(this Func<Task> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            var newSource = new CancellationTokenSource();
            var previousSource = Interlocked.Exchange(ref cancelTokenSource, newSource);
            
            // Cancel and dispose the previous source safely
            if (previousSource != null)
            {
                try
                {
                    previousSource.Cancel();
                }
                finally
                {
                    previousSource.Dispose();
                }
            }
            
            var currentToken = newSource.Token;

            Task.Delay(milliseconds, currentToken)
                .ContinueWith(async t =>
                {
                    try
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            await func();
                        }
                    }
                    finally
                    {
                        // Atomically try to set cancelTokenSource to null only if it's still newSource.
                        // CompareExchange returns the original value, so if it returns newSource,
                        // we successfully cleared it and should dispose.
                        if (Interlocked.CompareExchange(ref cancelTokenSource, null, newSource) == newSource)
                        {
                            newSource.Dispose();
                        }
                    }
                }, TaskScheduler.Default);
        };
    }
}