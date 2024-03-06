using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using System.Text;

Console.OutputEncoding = Encoding.GetEncoding("utf-8");
var configuration = new ConfigurationBuilder()
    .AddUserSecrets("90cdbb2e-9f8f-4322-8837-f59e9e4b4703")
    .Build();

var deployment = configuration["AzureOpenAI:Deployment"];
var endpoint = configuration["AzureOpenAI:Endpoint"];
var apiKey = configuration["AzureOpenAI:ApiKey"];
var bingKey = configuration["AzureOpenAI:BingKey"];

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
});
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton(loggerFactory);
builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
var kernel = builder.Build();

#pragma warning disable SKEXP0050, SKEXP0054, SKEXP0060  // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    BingConnector bingConnector = new BingConnector(bingKey, loggerFactory);
    var plugin = new WebSearchEnginePlugin(bingConnector);
    //kernel.ImportPluginFromObject(plugin, "WebSearchEnginePlugin");

    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,        
    };

    var results = kernel.InvokePromptStreamingAsync("Please tell me about Final Fantasy 7 remake.", new KernelArguments(settings));
    await foreach (var message in results)
    {
        Console.Write(message);
    }
}
Console.WriteLine("=================== End of Web search ===================");
{
    // TODO: This does not work well. Need to Improve.
    var uri= "https://raw.githubusercontent.com/normalian/My-Azure-Portal-ChromeExtension/master/README.md";
    var plugin = new WebFileDownloadPlugin(loggerFactory);
    // As reference, this function call works well
    // plugin.DownloadToFileAsync(new Uri(url), "README.md").Wait();

    //kernel.ImportPluginFromObject(plugin, "WebFileDownloadPlugin");
    //kernel.ImportPluginFromType<FileIOPlugin>();
    //kernel.ImportPluginFromType<HttpPlugin>();

    // Create a plan
    var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
    var plan = await planner.CreatePlanAsync(kernel, $"Please download {uri} and save it on my current directory.");
    Console.WriteLine($"Plan: {plan}");

    // Execute the plan
    var result = (await plan.InvokeAsync(kernel)).Trim();
    Console.WriteLine($"Results: {result}");

    Console.WriteLine("=================== middle of Web download ===================");

    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };
    var results = kernel.InvokePromptStreamingAsync($"Please download {uri} and save it on my current directory.",
        new KernelArguments(settings));
    await foreach (var message in results)
    {
        Console.Write(message);
    }
}
Console.WriteLine("=================== End of Web download ===================");
#pragma warning restore SKEXP0050, SKEXP0054, SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

Console.ReadLine();