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
kernel.ImportPluginFromType<FileIOPlugin>();

string filename = @"test.txt ";

// This code as follows can read the file directly.
Console.WriteLine("========================================== Start plan");
{
    // Create a plan
    var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
    var plan = await planner.CreatePlanAsync(kernel, $"Please read {filename} file on current directory");
    Console.WriteLine($"Plan: {plan}");

    // Execute the plan
    Console.WriteLine("========================================== Start execute plan");
    var result = (await plan.InvokeAsync(kernel)).Trim();
    Console.WriteLine($"Results: {result}");
}

// This code as follows can not read the file directly.
//Console.WriteLine("========================================== Start invokeprompt");
//{
//    OpenAIPromptExecutionSettings settings = new()
//    {
//        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
//    }; var results = kernel.InvokePromptStreamingAsync($"Please read {filename} file on current directory", new KernelArguments(settings));
//    await foreach (var message in results)
//    {
//        Console.Write(message);
//    }
//}
#pragma warning restore SKEXP0050, SKEXP0060
Console.WriteLine("========================================== End of Application ");
