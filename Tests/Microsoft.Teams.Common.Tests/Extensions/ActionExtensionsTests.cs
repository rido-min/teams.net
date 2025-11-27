// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Extensions;

namespace Microsoft.Teams.Common.Tests.Extensions;

public class ActionExtensionsTests
{
    [Fact]
    public async Task Debounce_ShouldExecuteAfterDelay()
    {
        // Arrange
        var executed = false;
        Action<int> action = _ => executed = true;
        var debounced = action.Debounce<int>(50);

        // Act
        debounced(1);
        await Task.Delay(100);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task Debounce_ShouldCancelPreviousCall()
    {
        // Arrange
        var callCount = 0;
        Action<int> action = _ => Interlocked.Increment(ref callCount);
        var debounced = action.Debounce<int>(100);

        // Act - rapid calls should cancel previous ones
        debounced(1);
        await Task.Delay(20);
        debounced(2);
        await Task.Delay(20);
        debounced(3);
        await Task.Delay(200); // Wait for final debounce

        // Assert - only the last call should execute
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task DebounceFunc_ShouldExecuteAfterDelay()
    {
        // Arrange
        var executed = false;
        Func<Task> func = () => { executed = true; return Task.CompletedTask; };
        var debounced = func.Debounce(50);

        // Act
        debounced();
        await Task.Delay(100);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task DebounceFunc_ShouldCancelPreviousCall()
    {
        // Arrange
        var callCount = 0;
        Func<Task> func = () => { Interlocked.Increment(ref callCount); return Task.CompletedTask; };
        var debounced = func.Debounce(100);

        // Act - rapid calls should cancel previous ones
        debounced();
        await Task.Delay(20);
        debounced();
        await Task.Delay(20);
        debounced();
        await Task.Delay(200); // Wait for final debounce

        // Assert - only the last call should execute
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Debounce_ShouldNotThrowOnRapidCalls()
    {
        // Arrange
        Action<int> action = _ => { };
        var debounced = action.Debounce<int>(50);

        // Act & Assert - should not throw on rapid calls that trigger dispose
        var exception = Record.Exception(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                debounced(i);
            }
        });

        Assert.Null(exception);
    }
}
