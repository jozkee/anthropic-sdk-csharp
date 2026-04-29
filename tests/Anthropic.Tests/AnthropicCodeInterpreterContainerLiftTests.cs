// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Xunit;

#pragma warning disable MEAI001 // [Experimental] APIs in Microsoft.Extensions.AI

namespace Anthropic.Tests;

public class AnthropicCodeInterpreterContainerLiftTests
{
    [Fact]
    public void NoPriorMessages_NoLift()
    {
        // Single-turn no-reuse: container is null and there are no prior tool calls.
        var tool = new HostedCodeInterpreterTool();
        ChatMessage[] messages =
        [
            new ChatMessage(ChatRole.User, "hello"),
        ];

        string? containerId = AnthropicBetaClientExtensions.ResolveCodeInterpreterContainerId(
            tool,
            messages
        );

        Assert.Null(containerId);
    }

    [Fact]
    public void NullContainer_LiftsLatestFromHistory()
    {
        // Multi-turn implicit lift: pick the most recent prior CodeInterpreterToolCallContent.
        var tool = new HostedCodeInterpreterTool();
        ChatMessage[] messages =
        [
            new ChatMessage(ChatRole.User, "first"),
            new ChatMessage(ChatRole.Assistant, [
                new CodeInterpreterToolCallContent("call-1") { ContainerId = "container-old" },
            ]),
            new ChatMessage(ChatRole.User, "second"),
            new ChatMessage(ChatRole.Assistant, [
                new CodeInterpreterToolCallContent("call-2") { ContainerId = "container-latest" },
            ]),
            new ChatMessage(ChatRole.User, "third"),
        ];

        string? containerId = AnthropicBetaClientExtensions.ResolveCodeInterpreterContainerId(
            tool,
            messages
        );

        Assert.Equal("container-latest", containerId);
    }

    [Fact]
    public void ExistingContainer_OverridesImplicitLift()
    {
        // Explicit FromExisting takes priority over any implicit lift.
        var tool = new HostedCodeInterpreterTool
        {
            Container = ContainerInfo.FromExisting("container-explicit"),
        };
        ChatMessage[] messages =
        [
            new ChatMessage(ChatRole.Assistant, [
                new CodeInterpreterToolCallContent("call-1") { ContainerId = "container-history" },
            ]),
        ];

        string? containerId = AnthropicBetaClientExtensions.ResolveCodeInterpreterContainerId(
            tool,
            messages
        );

        Assert.Equal("container-explicit", containerId);
    }

    [Fact]
    public void AutomaticContainer_OptsOutOfImplicitLift()
    {
        // Explicit AutomaticContainerInfo means "force fresh / let the service decide" -
        // the adapter must not lift a prior container ID even if one is in history.
        var tool = new HostedCodeInterpreterTool
        {
            Container = ContainerInfo.Automatic(),
        };
        ChatMessage[] messages =
        [
            new ChatMessage(ChatRole.Assistant, [
                new CodeInterpreterToolCallContent("call-1") { ContainerId = "container-history" },
            ]),
        ];

        string? containerId = AnthropicBetaClientExtensions.ResolveCodeInterpreterContainerId(
            tool,
            messages
        );

        Assert.Null(containerId);
    }

    [Fact]
    public void PriorCallWithoutContainerId_NoLift()
    {
        // Prior CodeInterpreterToolCallContent with null ContainerId must not be lifted.
        var tool = new HostedCodeInterpreterTool();
        ChatMessage[] messages =
        [
            new ChatMessage(ChatRole.Assistant, [
                new CodeInterpreterToolCallContent("call-1"), // ContainerId left null
            ]),
            new ChatMessage(ChatRole.User, "follow up"),
        ];

        string? containerId = AnthropicBetaClientExtensions.ResolveCodeInterpreterContainerId(
            tool,
            messages
        );

        Assert.Null(containerId);
    }
}
