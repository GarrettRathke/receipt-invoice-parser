variable "aws_region" {
  description = "AWS region for deployment"
  type        = string
  default     = "us-east-2"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "openai_api_key" {
  description = "OpenAI API key (stored in Secrets Manager)"
  type        = string
  sensitive   = true
}

variable "lambda_memory" {
  description = "Lambda function memory in MB"
  type        = number
  default     = 512
  
  validation {
    condition     = var.lambda_memory >= 128 && var.lambda_memory <= 3008
    error_message = "Lambda memory must be between 128 and 3008 MB."
  }
}

variable "lambda_timeout" {
  description = "Lambda function timeout in seconds"
  type        = number
  default     = 60
  
  validation {
    condition     = var.lambda_timeout >= 1 && var.lambda_timeout <= 900
    error_message = "Lambda timeout must be between 1 and 900 seconds."
  }
}

variable "frontend_build_path" {
  description = "Path to built frontend files (dist directory)"
  type        = string
  default     = "../../../frontend/dist"
}

variable "lambda_handler_path" {
  description = "Path to Lambda handler zip file"
  type        = string
  default     = "../lambda/handler.zip"
}
