# fcg-notifications-lambda

Notificações do FCGames **na AWS**: função Lambda `fiap-fcgames-notifications-lambda` (.NET 10,
runtime `provided.al2023`, ARM64) acionada pela fila SQS `fiap-fcgames-sqs-notifications-queue`.
Escreve logs simulando os e-mails. No ambiente local o papel é do worker
[`fcg-notifications-service`](https://github.com/FIAP-POS-TECH-TEAM-10/fcg-notifications-service).

## Eventos

| Evento | Log (`tipo`) |
|---|---|
| `UsuarioCriadoEvento` | `email-boas-vindas` |
| `PagamentoProcessadoEvento` | Aprovado: `email-confirmacao-compra` · Rejeitado: `email-pagamento-rejeitado` |

A fila é única. O tipo vem do atributo `EventType` da mensagem ou é detectado no envelope do
SNS/MassTransit, então os eventos publicados pelos serviços chegam sem nada a mais.

**Situação hoje:** a fila está assinada só no tópico SNS `FCGames_IntegrationEvents-UsuarioCriadoEvento`
(assinatura criada fora deste Terraform). O tópico do `PagamentoProcessadoEvento` não está assinado,
então na AWS as notificações de compra não chegam à Lambda.

## Ver os logs

Console → Lambda → `fiap-fcgames-notifications-lambda` → **Monitor** → **View CloudWatch logs**
(grupo `/aws/lambda/fiap-fcgames-notifications-lambda`). Mensagens que falham vão para a DLQ
`fiap-fcgames-sqs-notifications-dlq`.

## Infra e deploy

- `infra/` (Terraform, estado no S3 em `notification-lambda/terraform.tfstate`): fila + DLQ, role,
  log group, função e o gatilho SQS (lotes de 10).
- `.github/workflows/deploy-on-pr-merge.yml`: push na `main` → build e `terraform apply`.
- `.github/workflows/terraform-pr-develop.yml`: PR para `develop` → `terraform plan` comentado no PR.

Variável da função: `ENVIRONMENT` (padrão `Production`). Rodar local: `dotnet tool install -g Amazon.Lambda.TestTool-10.0`.
