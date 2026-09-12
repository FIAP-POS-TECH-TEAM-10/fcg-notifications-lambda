# ------------------------------------------------------------------------------
# 2. IAM Role & Políticas de Segurança para a Lambda
# ------------------------------------------------------------------------------
resource "aws_iam_role" "lambda_role" {
  name = "${var.project_name}-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = var.tags
}

# Permissão básica de logs no CloudWatch
resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Permissões mínimas necessárias para consumir SQS
resource "aws_iam_role_policy" "lambda_sqs_policy" {
  name = "${var.project_name}-sqs-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes"
        ]
        Resource = aws_sqs_queue.main_queue.arn
      }
    ]
  })
}

# ------------------------------------------------------------------------------
# 3. Função AWS Lambda (.NET 10 / Runtime AL2023)
# ------------------------------------------------------------------------------
resource "aws_lambda_function" "sqs_consumer" {
  filename         = var.lambda_zip_path
  function_name    = var.project_name
  role             = aws_iam_role.lambda_role.arn
  handler          = "Fiap.FCGames.Notifications.Lambda::Fiap.FCGames.Notifications.Lambda.FunctionHandler::FunctionHandlerAsync"
  runtime          = "provided.al2023"
  architectures    = ["arm64"] # ARM64 (Graviton) para menor custo e melhor performance em .NET
  memory_size      = var.lambda_memory_size
  timeout          = var.lambda_timeout
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      ENVIRONMENT = var.environment
      LOG_LEVEL   = "Information"
    }
  }

  tags = var.tags
}

# Log Group no CloudWatch com política de retenção definida
resource "aws_cloudwatch_log_group" "lambda_log_group" {
  name              = "/aws/lambda/${aws_lambda_function.sqs_consumer.function_name}"
  retention_in_days = 5

  tags = var.tags
}

# ------------------------------------------------------------------------------
# 4. Event Source Mapping (Acionamento automático da Lambda via SQS)
# ------------------------------------------------------------------------------
resource "aws_lambda_event_source_mapping" "sqs_trigger" {
  event_source_arn = aws_sqs_queue.main_queue.arn
  function_name    = aws_lambda_function.sqs_consumer.arn
  batch_size       = 10
  enabled          = true

  # Permite tratar falhas parciais do lote sem reprocessar todas as mensagens
  function_response_types = ["ReportBatchItemFailures"]
}