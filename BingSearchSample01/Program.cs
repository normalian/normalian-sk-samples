using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using Microsoft.Extensions.Configuration;

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
builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey).Build();
var kernel = builder.Build();

#pragma warning disable SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    BingConnector bingConnector = new BingConnector(bingKey, loggerFactory);
    var plugin = new WebSearchEnginePlugin(bingConnector);
    //kernel.ImportPluginFromObject(plugin, "WebSearchEnginePlugin");

    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
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
    var url = "https://raw.githubusercontent.com/normalian/My-Azure-Portal-ChromeExtension/master/README.md";
    var plugin = new WebFileDownloadPlugin(loggerFactory);
    // As reference, this function call works well
    plugin.DownloadToFileAsync(new Uri(url), "README.md").Wait();

    kernel.ImportPluginFromObject(plugin, "WebFileDownloadPlugin");
    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };
    var results = kernel.InvokePromptStreamingAsync($"Please download {url} and save it on my current directory.",
        new KernelArguments(settings));
    await foreach (var message in results)
    {
        Console.Write(message);
    }
}
Console.WriteLine("=================== End of Web download ===================");
#pragma warning restore SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

Console.ReadLine();