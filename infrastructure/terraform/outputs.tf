output "frontend_url" {
  description = "Frontend application URL (CloudFront)"
  value       = "https://${aws_cloudfront_distribution.frontend.domain_name}"
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain name"
  value       = aws_cloudfront_distribution.frontend.domain_name
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID (for cache invalidation)"
  value       = aws_cloudfront_distribution.frontend.id
}

output "api_endpoint" {
  description = "API Gateway endpoint URL"
  value       = "${aws_api_gateway_deployment.api_deployment.invoke_url}/"
}

output "api_health_check_url" {
  description = "API health check endpoint"
  value       = "${aws_api_gateway_deployment.api_deployment.invoke_url}/api/hello"
}

output "s3_bucket_name" {
  description = "S3 bucket name for frontend"
  value       = aws_s3_bucket.frontend.id
}

output "lambda_function_name" {
  description = "Lambda function name"
  value       = aws_lambda_function.backend.function_name
}

output "lambda_function_arn" {
  description = "Lambda function ARN"
  value       = aws_lambda_function.backend.arn
}

output "openai_secret_name" {
  description = "Secrets Manager secret name"
  value       = aws_secretsmanager_secret.openai_key.name
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group for Lambda"
  value       = aws_cloudwatch_log_group.lambda_logs.name
}
