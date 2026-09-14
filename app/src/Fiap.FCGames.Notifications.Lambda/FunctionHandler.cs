using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using FCGames.IntegrationEvents;
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
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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

        // A fila é única. O tipo do evento pode vir:
        // 1. No MessageAttribute "EventType" (ex: UsuarioCriadoEvento)
        // 2. Ou detectado automaticamente pelo conteúdo do JSON se o atributo não for enviado.
        private async Task ProcessMessageAsync(SQSMessage message, ILambdaContext context)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["MessageId"] = message.MessageId }))
            {
                var body = message.Body;

                // Trata caso a mensagem tenha passado por SNS sem raw message delivery (envelope SNS)
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Message", out var snsMessage) && doc.RootElement.TryGetProperty("Type", out var snsType) && snsType.GetString() == "Notification")
                    {
                        body = snsMessage.GetString() ?? body;
                    }
                }
                catch
                {
                    // Não é envelope SNS, prossegue com o body original
                }

                string? eventType = null;
                if (message.MessageAttributes != null && message.MessageAttributes.TryGetValue("EventType", out var eventTypeAttr))
                {
                    eventType = eventTypeAttr.StringValue;
                }

                // Fallback: se não veio MessageAttribute, tenta inferir pelos campos do JSON
                if (string.IsNullOrWhiteSpace(eventType))
                {
                    if (body.Contains("Email", StringComparison.OrdinalIgnoreCase) && body.Contains("UsuarioId", StringComparison.OrdinalIgnoreCase))
                        eventType = nameof(UsuarioCriadoEvento);
                    else if (body.Contains("PedidoId", StringComparison.OrdinalIgnoreCase) && body.Contains("Status", StringComparison.OrdinalIgnoreCase))
                        eventType = nameof(PagamentoProcessadoEvento);
                }

                switch (eventType)
                {
                    case nameof(UsuarioCriadoEvento):
                        ProcessarUsuarioCriado(body);
                        break;
                    case nameof(PagamentoProcessadoEvento):
                        ProcessarPagamentoProcessado(body);
                        break;
                    default:
                        _logger.LogWarning("Não foi possível identificar o tipo do evento. Corpo: {Body}", body);
                        break;
                }

                await Task.CompletedTask;
            }
        }

        private void ProcessarUsuarioCriado(string body)
        {
            var evt = System.Text.Json.JsonSerializer.Deserialize<UsuarioCriadoEvento>(body, JsonOptions)
                ?? throw new InvalidOperationException("UsuarioCriadoEvento veio nulo após deserialização.");

            _logger.LogInformation(
                "Notificacao: {Tipo} | destinatario: {Email} | usuarioId: {UsuarioId} | correlationId: {CorrelationId}",
                "email-boas-vindas", evt.Email, evt.UsuarioId, evt.CorrelationId);
        }

        private void ProcessarPagamentoProcessado(string body)
        {
            var evt = System.Text.Json.JsonSerializer.Deserialize<PagamentoProcessadoEvento>(body, JsonOptions)
                ?? throw new InvalidOperationException("PagamentoProcessadoEvento veio nulo após deserialização.");

            if (evt.Status == "Aprovado")
            {
                _logger.LogInformation(
                    "Notificacao: {Tipo} | jogo: {NomeJogo} | valor: {Valor} | usuarioId: {UsuarioId} | pedidoId: {PedidoId} | correlationId: {CorrelationId}",
                    "email-confirmacao-compra", evt.NomeJogo, evt.Preco, evt.UsuarioId, evt.PedidoId, evt.CorrelationId);
            }
            else
            {
                _logger.LogInformation(
                    "Notificacao: {Tipo} | jogo: {NomeJogo} | motivo: {Motivo} | usuarioId: {UsuarioId} | pedidoId: {PedidoId} | correlationId: {CorrelationId}",
                    "email-pagamento-rejeitado", evt.NomeJogo, evt.Motivo, evt.UsuarioId, evt.PedidoId, evt.CorrelationId);
            }
        }
    }
}
