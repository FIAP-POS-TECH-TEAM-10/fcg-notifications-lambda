# ------------------------------------------------------------------------------
# 1. Filas SQS (Fila Principal + Dead Letter Queue)
# ------------------------------------------------------------------------------
resource "aws_sqs_queue" "dlq" {
  name                      = "${var.sqs_name}-dlq"
  message_retention_seconds = 1209600 # 14 dias para retenção de mensagens com erro

  tags = var.tags
}

resource "aws_sqs_queue" "main_queue" {
  name                      = "${var.sqs_name}-queue"
  delay_seconds             = 0
  max_message_size          = 262144
  message_retention_seconds = 345600 # 4 dias
  receive_wait_time_seconds  = 10     # Long polling para redução de custos

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq.arn
    maxReceiveCount     = 3           # 3 tentativas antes de mover para DLQ
  })

  tags = var.tags
}