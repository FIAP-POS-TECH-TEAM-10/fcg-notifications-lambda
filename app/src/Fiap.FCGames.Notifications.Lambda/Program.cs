using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Fiap.FCGames.Notifications.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddSingleton<FunctionHandler>();

var host = builder.Build();
var handler = host.Services.GetRequiredService<FunctionHandler>();
//var serializer = new DefaultLambdaJsonSerializer();

try
{
    var serializer = new DefaultLambdaJsonSerializer();

    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")))
    {
        await host.RunAsync();
    }
    else
    {
        await LambdaBootstrapBuilder
            .Create<SQSEvent>(handler.FunctionHandlerAsync, serializer)
            .Build()
            .RunAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"CRASH NO STARTUP DA LAMBDA: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}