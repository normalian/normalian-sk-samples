using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.Plugins.Core;
using System.Text;

Console.WriteLine("========================================== Start applicaion");

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
//builder.SetMinimumLevel(LogLevel.Trace);
builder.SetMinimumLevel(LogLevel.Warning);
});

#pragma warning disable SKEXP0050, SKEXP0060
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton(loggerFactory);
builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

var kernel = builder.Build();
kernel.ImportPluginFromType<HttpPlugin>();

var uri = "https://raw.githubusercontent.com/normalian/My-Azure-Portal-ChromeExtension/master/README.md";

Console.WriteLine("========================================== Start plan");
{
    // Create a plan
    var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
    var plan = await planner.CreatePlanAsync(kernel, $"Please send http request to {uri}");
    Console.WriteLine($"Plan: {plan}");

    // Execute the plan
    Console.WriteLine("========================================== Start execute plan");
    var result = (await plan.InvokeAsync(kernel)).Trim();
    Console.WriteLine($"Results: {result}");
}
#pragma warning restore SKEXP0050, SKEXP0060
Console.WriteLine("========================================== End of Application ");
