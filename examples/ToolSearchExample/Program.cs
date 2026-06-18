#pragma warning disable MEAI001 // Experimental AI APIs

using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Trace);
});

// Uses ANTHROPIC_API_KEY environment variable.
// Tool search requires a capable model (sonnet-4-6+). Haiku does not reliably invoke it.
IChatClient chatClient = new AnthropicClient()
    .AsIChatClient("claude-sonnet-4-6")
    .AsBuilder()
    .UseFunctionInvocation()
    .UseLogging(loggerFactory)
    .Build();

// Define some tools
var getWeather = AIFunctionFactory.Create(
    (string city) => $"The weather in {city} is sunny, 72°F",
    "GetWeather",
    "Gets the current weather for a given city."
);

var getTime = AIFunctionFactory.Create(
    () => DateTime.Now.ToString("h:mm tt"),
    "GetTime",
    "Gets the current time."
);

var calculateTip = AIFunctionFactory.Create(
    (decimal amount, decimal percentage) => amount * percentage / 100m,
    "CalculateTip",
    "Calculates the tip for a given bill amount and percentage."
);

// ---- Demo 1: HostedToolSearchTool with all tools deferred ----
// When DeferredTools is null (default), ALL function tools get defer_loading=true.
// The model must invoke tool_search_tool_bm25 to discover them before calling them.
ChatOptions deferAllOptions = new()
{
    Tools =
    [
        new HostedToolSearchTool(),
        getWeather,
        getTime,
        calculateTip,
    ],
};

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  Demo 1: HostedToolSearchTool (defer all)           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

var response1 = await chatClient.GetResponseAsync(
    "What's the weather in Seattle? Be concise.",
    deferAllOptions);

Console.WriteLine();

// ---- Demo 2: Selective defer ----
// Only GetWeather and GetTime are deferred. CalculateTip stays in context.
ChatOptions selectiveOptions = new()
{
    Tools =
    [
        new HostedToolSearchTool { DeferredTools = ["GetWeather", "GetTime"] },
        getWeather,
        getTime,
        calculateTip,
    ],
};

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  Demo 2: HostedToolSearchTool (selective defer)     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

var response2 = await chatClient.GetResponseAsync(
    "What's the weather in NYC? Also, calculate a 20% tip on $85. Be concise.",
    selectiveOptions);
