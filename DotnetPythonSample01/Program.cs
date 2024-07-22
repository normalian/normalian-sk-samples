// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Text;

Console.OutputEncoding = Encoding.GetEncoding("utf-8");
var configuration = new ConfigurationBuilder()
    .AddUserSecrets("a99df0ee-8e79-4b22-a983-efef33bfb063")
    .Build();

var chat_deployment = configuration["AzureOpenAI:Deployment"];
var aoai_endpoint = configuration["AzureOpenAI:Endpoint"];
var aoai_apiKey = configuration["AzureOpenAI:ApiKey"];
var aisearch_endpoint = configuration["AISearch:Endpoint"];
var aisearch_apiKey = configuration["AISearch:ApiKey"];

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0060
var builder = Kernel.CreateBuilder();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
    //builder.SetMinimumLevel(LogLevel.Warning);
});
builder.Services.AddSingleton(loggerFactory);
builder.AddAzureOpenAIChatCompletion(
         chat_deployment,                 // Azure OpenAI Deployment Name
         aoai_endpoint,    // Azure OpenAI Endpoint
         aoai_apiKey);    // Azure OpenAI Key

var memory = new MemoryBuilder()
    .WithLoggerFactory(loggerFactory)
    .WithAzureOpenAITextEmbeddingGeneration("text-embedding-ada-002", aoai_endpoint, aoai_apiKey)
    .WithMemoryStore(new AzureAISearchMemoryStore(aisearch_endpoint, aisearch_apiKey))
    .Build();

builder.Plugins.AddFromObject(new TextMemoryPlugin(memory));

var kernel = builder.Build();

var ask = @"How many offices does Normalian Co.,Ltd. have?";

var options = new FunctionCallingStepwisePlannerOptions()
{
    MaxIterations = 5,
};

var planner = new FunctionCallingStepwisePlanner();
var result = await planner.ExecuteAsync(kernel, ask);

Console.WriteLine("============= The answer ======================="); 
Console.WriteLine(result.FinalAnswer);

#pragma warning restore SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0060
