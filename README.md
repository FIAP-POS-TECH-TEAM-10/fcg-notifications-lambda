# fcg-notifications-lambda

Substitui o `fcg-notifications-service` (container 24/7) por uma função **AWS Lambda**
(.NET 10, custom runtime `provided.al2023`, self-contained ARM64), acionada diretamente
por mensagens na fila SQS `fiap-fcgames-sqs-notifications-queue`.

## Contrato da mensagem (IMPORTANTE pra quem for publicar nesta fila)

A fila é **única** (não tem uma fila por tipo de evento). Por isso, cada mensagem
enviada pra `fiap-fcgames-sqs-notifications-queue` precisa ter um **MessageAttribute**
chamado `EventType` com um destes valores:

- `UsuarioCriadoEvento` — corpo da mensagem = JSON do `UsuarioCriadoEvento`
- `PagamentoProcessadoEvento` — corpo da mensagem = JSON do `PagamentoProcessadoEvento`

Sem esse atributo, a Lambda loga um aviso e ignora a mensagem (não derruba pra DLQ,
já que não é um erro de processamento, é falta de contrato).

Exemplo de envio (C#, `AWSSDK.SQS`):
```csharp
await sqsClient.SendMessageAsync(new SendMessageRequest
{
    QueueUrl = queueUrl,
    MessageBody = JsonSerializer.Serialize(evento),
    MessageAttributes = new Dictionary<string, MessageAttributeValue>
    {
        ["EventType"] = new() { DataType = "String", StringValue = nameof(UsuarioCriadoEvento) }
    }
});
```

Os records `UsuarioCriadoEvento`/`PagamentoProcessadoEvento` vêm do pacote
`FCGames.IntegrationEvents` (mesma fonte usada pelos outros microsserviços) — não
foram duplicados aqui, já que este projeto compila em `net10.0`.

Rodar local precisa instalar

dotnet tool install -g Amazon.Lambda.TestTool-10.0