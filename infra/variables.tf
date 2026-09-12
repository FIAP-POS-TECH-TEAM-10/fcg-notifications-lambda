variable "aws_region" {
  type        = string
  default     = "sa-east-1"
  description = "Região da AWS."
}

variable "project_name" {
  type        = string
  default     = "fiap-fcgames-notifications-lambda"
  description = "Nome base para os recursos do projeto."
}

variable "sqs_name" {
  type        = string
  default     = "fiap-fcgames-sqs-notifications"
  description = "Nome base para a fila."
}

variable "environment" {
  type        = string
  default     = "Production"
  description = "Ambiente de execução (Development, Production)."
}

variable "lambda_memory_size" {
  type        = number
  default     = 512
  description = "Memória alocada (MB)."
}

variable "lambda_timeout" {
  type        = number
  default     = 30
  description = "Timeout máximo em segundos."
}

variable "tags" {
  type        = map(string)
  default = {
    Project     = "FCGames"
    Environment = "Production"
    ManagedBy   = "Terraform"
  }
}

variable "lambda_zip_path" {
  type        = string
  default     = "./publish/bootstrap.zip"
  description = "Caminho do ZIP gerado dentro da pasta infra."
}