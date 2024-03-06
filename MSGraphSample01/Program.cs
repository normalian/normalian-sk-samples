using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.Plugins.MsGraph;
using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;
using System.Text;

Console.OutputEncoding = Encoding.GetEncoding("utf-8");
var configuration = new ConfigurationBuilder()
    .AddUserSecrets("90cdbb2e-9f8f-4322-8837-f59e9e4b4703")
    .Build();

var deployment = configuration["AzureOpenAI:Deployment"];
var endpoint = configuration["AzureOpenAI:Endpoint"];
var apiKey = configuration["AzureOpenAI:ApiKey"];

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
});
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton(loggerFactory);
builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

var scope = new[] { "Calendars.Read" };
var tenantId = configuration["EntraID:TenantId"];
var clientId = configuration["EntraID:ClientId"];

var options = new DeviceCodeCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
    TenantId = tenantId,
    ClientId = clientId,
    DeviceCodeCallback = (code, cancellation) =>
    {
        Console.WriteLine(code.Message);
        return Task.FromResult(0);
    },
};

var deviceCodeCredential = new DeviceCodeCredential(options);
var graphClient = new GraphServiceClient(deviceCodeCredential, scope);

#pragma warning disable SKEXP0053, SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
OutlookCalendarConnector connector = new OutlookCalendarConnector(graphClient);
CalendarPlugin plugin = new CalendarPlugin(connector, loggerFactory);

var result = await plugin.GetCalendarEventsAsync(10, 0);
var kernel = builder.Build();
kernel.ImportPluginFromObject(plugin, "CalendarPlugin");

OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

string prompt = "Please list all meetings";
{
    var results = kernel.InvokePromptStreamingAsync(prompt, new KernelArguments(settings));
    await foreach (var message in results)
    {
        Console.Write(message);
    }
}
{
    var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
    var plan = await planner.CreatePlanAsync(kernel, prompt);
    //Console.WriteLine("Plan: {Plan}", plan);

    // Execute the plan
    var results = (await plan.InvokeAsync(kernel)).Trim();
    Console.WriteLine("Results: {Result}", results);
}
#pragma warning restore SKEXP0053, SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

Console.WriteLine();
Console.ReadLine();



