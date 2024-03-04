using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Text;

Console.WriteLine("========================================== Start applicaion");

Console.OutputEncoding = Encoding.GetEncoding("utf-8");
var configuration = new ConfigurationBuilder()
    .AddUserSecrets("90cdbb2e-9f8f-4322-8837-f59e9e4b4703")
    .Build();
var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];
var deployment = configuration["AzureOpenAI:Deployment"];
var endpoint = configuration["AzureOpenAI:Endpoint"];
var apiKey = configuration["AzureOpenAI:ApiKey"];
var collectioname = "GundamWiki";

var urls = new Dictionary<string, string>
{
    ["https://gundam.fandom.com/wiki/Amuro_Ray"] = "Amuro Ray is a Newtype, he is most famous for piloting the powerful RX-78-2 Gundam during the One Year War.",
    ["https://gundam.fandom.com/wiki/Char_Aznable"] = "Char Aznable is given the nom de guerre of The Red Comet for his performance at the Battle of Loum during the One Year War.",
    ["https://gundam.fandom.com/wiki/Kamille_Bidan"] = "Kamille Bidan is the pilot of the MSZ-006 Zeta Gundam. Yoshiyuki Tomino mentioned that Kamille is the best Newtype.",
    ["https://gundam.fandom.com/wiki/Banagher_Links"] = "Banagher is also portrayed as a Newtype with very high potential. He is able to control the Unicorn in Destroy Mode during his first time in the cockpit.",
};

#pragma warning disable SKEXP0003, SKEXP0011, SKEXP0021
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    //builder.SetMinimumLevel(LogLevel.Trace);
    builder.SetMinimumLevel(LogLevel.Warning);
});
var memory = new MemoryBuilder().WithAzureOpenAITextEmbeddingGeneration(embeddingDeployment, endpoint, apiKey)
    .WithMemoryStore(new AzureAISearchMemoryStore(configuration["AzureAISearch:EndPoint"], configuration["AzureAISearch:ApiKey"]))
    .WithLoggerFactory(loggerFactory)
    .Build();

foreach (var entry in urls)
{
    await memory.SaveReferenceAsync(
        collection: collectioname,
        externalSourceName: "GundamWiki",
        externalId: entry.Key,
        description: entry.Value,
        text: entry.Value);
}

var responses = memory.SearchAsync(collectioname, "Who is the best Newtype?", limit: 5, minRelevanceScore: 0.7);
Console.WriteLine($"result = {responses}");
int i = 0;
await foreach (var response in responses)
{
    Console.WriteLine($"Result {++i}:");
    Console.WriteLine("  URL:     : " + response.Metadata.Id);
    Console.WriteLine("  Title    : " + response.Metadata.Description);
    Console.WriteLine("  Relevance: " + response.Relevance);
    Console.WriteLine();
}


Console.WriteLine("========================================== Start with TextMemoryPlugin");
#pragma warning disable SKEXP0052
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton(loggerFactory);
builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey).Build();

var kernel = builder.Build();
kernel.ImportPluginFromObject(new TextMemoryPlugin(memory));

const string skPrompt = @"
ChatBot can have a conversation with you about any topic.
It can give explicit instructions or say 'I don't know' if it does not have an answer.

Information about me, from previous conversations:
- {{$fact1}} {{recall $fact1}}
- {{$fact2}} {{recall $fact2}}
- {{$fact3}} {{recall $fact3}}
- {{$fact4}} {{recall $fact4}}
- {{$fact5}} {{recall $fact5}}

Chat:
{{$history}}
User: {{$userInput}}
ChatBot: ";

var chatFunction = kernel.CreateFunctionFromPrompt(skPrompt, new OpenAIPromptExecutionSettings { MaxTokens = 200, Temperature = 0.8 });
var arguments = new KernelArguments();
arguments["fact1"] = "Who is the best Newtype?";
arguments["fact2"] = "Who is Amuro Ray?";
arguments["fact3"] = "Who is Char Aznable?";
arguments["fact4"] = "Who is Kamille Bidan?";
arguments["fact5"] = "What is the RX-78-2 Gundam?";
arguments["history"] = "I am a big fan of Gundam. I want to know more about the characters and the mobile suits.";
arguments["userInput"] = "Who is the best Newtype?";
arguments[TextMemoryPlugin.CollectionParam] = collectioname;
arguments[TextMemoryPlugin.LimitParam] = "2";
arguments[TextMemoryPlugin.RelevanceParam] = "0.7";

var history = "";
arguments["history"] = history;

var input = "Who is the youngest Newtype?";
arguments["userInput"] = input;
var answer = await chatFunction.InvokeAsync(kernel, arguments);
var result = $"\nUser: {input}\nChatBot: {answer}\n";
Console.WriteLine(result);

//history += result;
//arguments["history"] = history;

#pragma warning restore SKEXP0052
