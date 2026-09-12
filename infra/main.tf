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

# Permissões gerenciadas oficiais da AWS para consumo de SQS via Lambda
resource "aws_iam_role_policy_attachment" "lambda_sqs_execution" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaSQSQueueExecutionRole"
}

# ------------------------------------------------------------------------------
# 3. CloudWatch Log Group (Criado ANTES da Lambda para evitar conflito)
# ------------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "lambda_log_group" {
  name              = "/aws/lambda/${var.project_name}"
  retention_in_days = 5

  tags = var.tags
}

# ------------------------------------------------------------------------------
# 4. Função AWS Lambda (.NET 10 / Runtime AL2023)
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

  # Garanta que o Log Group já exista antes de criar a Lambda
  depends_on = [
    aws_cloudwatch_log_group.lambda_log_group,
    aws_iam_role_policy_attachment.lambda_basic_execution
  ]

  tags = var.tags
}

# ------------------------------------------------------------------------------
# 5. Event Source Mapping (Acionamento automático da Lambda via SQS)
# ------------------------------------------------------------------------------
resource "aws_lambda_event_source_mapping" "sqs_trigger" {
  event_source_arn = aws_sqs_queue.main_queue.arn
  function_name    = aws_lambda_function.sqs_consumer.arn
  batch_size       = 10
  enabled          = true

  # Permite tratar falhas parciais do lote sem reprocessar todas as mensagens
  function_response_types = ["ReportBatchItemFailures"]

  # CRÍTICO: Esperar as permissões do IAM e a Lambda estarem 100% ativas antes de mapear
  depends_on = [
    aws_iam_role_policy_attachment.lambda_sqs_execution,
    aws_lambda_function.sqs_consumer
  ]
}