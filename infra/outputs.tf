output "ecr_repository_url" {
  value       = aws_ecr_repository.app_repo.repository_url
  description = "URI do repositório ECR criado para o microsserviço"
}

output "ecs_cluster_name" {
  value       = aws_ecs_cluster.main.name
  description = "Nome do Cluster ECS criado"
}

output "ecs_service_name" {
  value       = aws_ecs_service.main.name
  description = "Nome do Service ECS que o GitHub Actions atualiza a cada deploy"
}

output "ecs_security_group_id" {
  description = "ID do Security Group associado às instâncias do ECS"
  value       = aws_security_group.ecs_sg.id
}