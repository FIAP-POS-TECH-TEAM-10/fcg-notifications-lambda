output "lambda_function_arn" {
  value       = aws_lambda_function.sqs_consumer.arn
  description = "ARN da função Lambda."
}

output "sqs_queue_url" {
  value       = aws_sqs_queue.main_queue.id
  description = "URL da fila SQS principal para envio de mensagens."
}

output "sqs_queue_arn" {
  value       = aws_sqs_queue.main_queue.arn
  description = "ARN da fila SQS principal."
}

output "dlq_queue_url" {
  value       = aws_sqs_queue.dlq.id
  description = "URL da Dead Letter Queue (DLQ)."
}