# AWS Serverless Deployment Guide

This guide walks through deploying the Receipt Invoice Parser application to AWS using serverless components: S3 + CloudFront for the frontend, Lambda for the backend, and API Gateway for routing.

## Architecture Overview

- **Frontend**: Angular app built and hosted in S3, served via CloudFront CDN
- **Backend**: .NET Lambda function proxying requests to the backend API
- **API Gateway**: REST API routing `/api/*` requests to Lambda
- **Secrets Manager**: Secure storage for OpenAI API key
- **CloudWatch**: Logs for Lambda execution and debugging

## Prerequisites

1. **AWS Account** with permissions to create:

   - S3 buckets
   - CloudFront distributions
   - Lambda functions
   - API Gateway
   - IAM roles and policies
   - Secrets Manager secrets
   - CloudWatch log groups

2. **AWS CLI** installed and configured

   ```bash
   aws --version
   aws configure
   ```

3. **Terraform** >= 1.0 installed

   ```bash
   terraform --version
   ```

4. **Node.js** 18+ (for frontend build)

   ```bash
   node --version
   npm --version
   ```

5. **.NET SDK** 10 (or 8 as fallback)

   ```bash
   dotnet --version
   ```

6. **OpenAI API Key** (get from https://platform.openai.com/api-keys)

## Deployment Steps

### 1. Clone/Setup the Repository

```bash
cd /home/towmater/Projects/public/receipt-invoice-parser
```

### 2. Set Environment Variables

```bash
# Required: OpenAI API Key
export OPENAI_API_KEY="sk-your-api-key-here"

# Optional: AWS region (defaults to us-east-1)
export AWS_REGION="us-east-2"

# Optional: Environment name (dev, staging, prod)
export TF_VAR_environment="dev"
```

### 3. Review Terraform Variables

Edit [`infrastructure/terraform/terraform.tfvars`](../infrastructure/terraform/terraform.tfvars) to customize:

- `aws_region` - AWS region for deployment
- `environment` - Environment name (dev/staging/prod)
- `lambda_memory` - Lambda memory allocation (128-3008 MB, default: 512)
- `lambda_timeout` - Lambda timeout in seconds (default: 60)

### 4. Build Frontend

```bash
bash scripts/build-frontend.sh
```

This builds the Angular application to `frontend/dist/` directory.

### 5. Build Lambda Handler

```bash
bash scripts/build-lambda.sh
```

This compiles the .NET Lambda handler and creates `infrastructure/lambda/handler.zip` for deployment.

### 6. Deploy with Terraform

```bash
cd infrastructure/terraform
terraform init
terraform plan -var="openai_api_key=$OPENAI_API_KEY" -out=tfplan
terraform apply -var="openai_api_key=$OPENAI_API_KEY" "tfplan"
```

Or use the convenience script:

```bash
bash scripts/deploy.sh
```

This script:

1. Builds the frontend
2. Builds the Lambda handler
3. Runs `terraform init`, `plan`, and `apply`
4. Uploads frontend files to S3
5. Invalidates CloudFront cache

### 7. Verify Deployment

After Terraform completes, check the outputs:

```bash
terraform output
```

Key outputs:

- `frontend_url` - CloudFront URL for the Angular app
- `api_endpoint` - API Gateway URL for backend requests
- `api_health_check_url` - Health check endpoint

Test the health endpoint:

```bash
curl $(terraform output -raw api_health_check_url)
```

## Configuration

### Lambda Environment Variables

The Lambda function receives:

- `OPENAI_API_KEY_SECRET` - Secrets Manager secret name for OpenAI API key
- `ENVIRONMENT` - Environment name (dev/staging/prod)

### CORS Configuration

API Gateway allows all origins (`*`) for CORS requests. To restrict to specific origins, modify the integration responses in [`infrastructure/terraform/main.tf`](../infrastructure/terraform/main.tf):

```hcl
response_parameters = {
  "method.response.header.Access-Control-Allow-Origin" = "'https://example.com'"
}
```

### Customizing Lambda Handler

The Lambda handler is located at [`infrastructure/lambda/Function.cs`](../infrastructure/lambda/Function.cs). It:

1. Retrieves the OpenAI API key from Secrets Manager
2. Forwards HTTP requests to the backend
3. Returns responses with CORS headers

To modify:

1. Edit `infrastructure/lambda/Function.cs`
2. Run `bash scripts/build-lambda.sh`
3. Run `terraform apply` to redeploy

## Troubleshooting

### Check Lambda Logs

```bash
aws logs tail /aws/lambda/receipt-parser-backend-dev --follow
```

### Verify Secrets Manager Secret

```bash
aws secretsmanager get-secret-value --secret-id receipt-parser-openai-key-dev
```

### Test API Endpoint

```bash
# Get the API endpoint
API_URL=$(cd infrastructure/terraform && terraform output -raw api_endpoint)

# Test health check
curl "$API_URL/api/hello"

# Test with a name parameter
curl "$API_URL/api/hello?name=World"
```

### Sync Frontend Files

If frontend files are out of date:

```bash
S3_BUCKET=$(cd infrastructure/terraform && terraform output -raw s3_bucket_name)
aws s3 sync frontend/dist s3://$S3_BUCKET --delete
```

### Invalidate CloudFront Cache

```bash
CF_DIST=$(cd infrastructure/terraform && terraform output -raw cloudfront_distribution_id)
aws cloudfront create-invalidation --distribution-id $CF_DIST --paths "/*"
```

## Cost Optimization Tips

1. **Lambda Memory**: Lower memory reduces per-second cost but increases execution time. Start with 512 MB and adjust based on CloudWatch metrics.

2. **CloudFront**: Uses pay-per-request pricing. Monitor cache hit ratio in CloudFront console.

3. **Secrets Manager**: $0.40 per month per secret. Consider consolidating if deploying multiple environments.

4. **S3 Storage**: Minimal cost for small Angular builds (<1 MB typical). Enable S3 Intelligent-Tiering for automatic cost optimization.

## Cleanup

To delete all AWS resources:

```bash
cd infrastructure/terraform
terraform destroy -var="openai_api_key=$OPENAI_API_KEY"
```

⚠️ **Warning**: This will delete all resources including S3 bucket, Lambda function, API Gateway, and Secrets Manager secret.

## Updates and Redeployment

To update the application:

1. **Update Backend**: Modify backend code, rebuild Lambda, and run `terraform apply`

   ```bash
   bash scripts/build-lambda.sh
   cd infrastructure/terraform && terraform apply
   ```

2. **Update Frontend**: Modify Angular app and redeploy:

   ```bash
   bash scripts/build-frontend.sh
   S3_BUCKET=$(cd infrastructure/terraform && terraform output -raw s3_bucket_name)
   CF_DIST=$(cd infrastructure/terraform && terraform output -raw cloudfront_distribution_id)
   aws s3 sync frontend/dist s3://$S3_BUCKET --delete
   aws cloudfront create-invalidation --distribution-id $CF_DIST --paths "/*"
   ```

3. **Update Infrastructure**: Modify Terraform files and run:
   ```bash
   cd infrastructure/terraform
   terraform plan
   terraform apply
   ```

## Next Steps

- [x] Deploy to AWS
- [ ] Configure custom domain with Route 53 and ACM certificate
- [ ] Set up CI/CD pipeline (GitHub Actions, CodePipeline)
- [ ] Add monitoring and alerts (CloudWatch, SNS)
- [ ] Implement auto-scaling policies for Lambda
- [ ] Set up separate environments (staging, production)
