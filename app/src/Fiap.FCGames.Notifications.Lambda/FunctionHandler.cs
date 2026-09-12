using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

// Define o serializador global para a ferramenta de testes
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace Fiap.FCGames.Notifications.Lambda
{
    public class FunctionHandler
    {
        private readonly ILogger<FunctionHandler> _logger;
      
        public FunctionHandler()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                    .AddJsonConsole(options => // Formato JSON ideal para CloudWatch / AWS Logs
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                    });
            });

            // 2. Instancia a interface ILogger<T> manualmente
            _logger = loggerFactory.CreateLogger<FunctionHandler>();
        }


        public async Task FunctionHandlerAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            _logger.LogInformation("Iniciando processamento do lote com {Count} mensagens. AWS Request ID: {RequestId}",
                sqsEvent.Records.Count, context.AwsRequestId);

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
