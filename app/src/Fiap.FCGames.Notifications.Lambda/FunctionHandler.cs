using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Fiap.FCGames.Notifications.Lambda.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

// Define o serializador global para a ferramenta de testes
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace Fiap.FCGames.Notifications.Lambda
{
    public class FunctionHandler
    {
        private readonly ILogger<FunctionHandler> _logger;        
        private readonly NotificationSettings _settings;        

        public FunctionHandler()
        {
            // 1. Carregar configurações do appsettings.json e Variáveis de Ambiente
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables() // Sobrescreve appsettings via env vars da AWS
                .Build();

            // 2. Mapear manualmente a seção para a classe NotificationSettings
            _settings = new NotificationSettings();
            configuration.GetSection("NotificationSettings").Bind(_settings);

            // 3. Criar a fábrica de logs manualmente
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
            });

            // 4. Instanciar o ILogger manualmente
            _logger = loggerFactory.CreateLogger<FunctionHandler>();
        }

        public async Task FunctionHandlerAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            _logger.LogInformation("Iniciando processamento do lote com {Count} mensagens. AWS Request ID: {RequestId}",
                sqsEvent.Records.Count, context.AwsRequestId);
            _logger.LogInformation("Provider URL carregada do appsettings: {Url}", _settings.SmsProviderUrl);
            _logger.LogInformation("Max Retry: {MaxRetry}", _settings.MaxRetryAttempts);

            string sqsEventJson = System.Text.Json.JsonSerializer.Serialize(sqsEvent);

            _logger.LogInformation("Conteudo do evento SQS: {SqsEventJson}", sqsEventJson);
            _logger.LogInformation("Conteudo da mensagem Body: {Body}", sqsEvent?.Records[0]?.Body);

            foreach (var record in sqsEvent.Records)
            {
                try
                {
                    await ProcessMessageAsync(record, context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no processamento da mensagem MessageId: {MessageId}", record.MessageId);
                    // Lançar a exceção fará com que o lote/mensagem falhe e vá para a DLQ (se configurada)
                    throw;
                }
            }
        }

        private async Task ProcessMessageAsync(SQSMessage message, ILambdaContext context)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["MessageId"] = message.MessageId }))
            {
                _logger.LogInformation("Processando mensagem. Corpo: {Body}", message.Body);

                // Simula processamento assíncrono da regra de negócio
                await Task.Delay(50);

                _logger.LogInformation("Mensagem processada com sucesso.");
            }
        }
    }
}
