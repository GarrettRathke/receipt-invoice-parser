# Terraform variables - Update with your values
# Use command line or environment variable for sensitive values:
# terraform apply -var="openai_api_key=sk-..."

aws_region  = "us-east-2"
environment = "dev"

# Lambda configuration
lambda_memory   = 512
lambda_timeout  = 60

# Build paths (relative to terraform directory)
frontend_build_path  = "../../../frontend/dist"
lambda_handler_path  = "../lambda/handler.zip"
