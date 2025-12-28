using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReceiptParserLambda;

public class Function
{
    private readonly AmazonSecretsManagerClient _secretsClient;
    private readonly HttpClient _httpClient;
    private string? _openaiApiKey;

    public Function()
    {
        _secretsClient = new AmazonSecretsManagerClient();
        _httpClient = new HttpClient();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LambdaLogger.Log($"Request: {request.HttpMethod} {request.Path}");

        try
        {
            // Load OpenAI API key from Secrets Manager (cached)
            _openaiApiKey ??= await GetOpenAIApiKey();

            // Route requests to backend endpoint
            // For local development, use environment variable BACKEND_URL
            // For production, call the backend directly or implement business logic here
            var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") 
                ?? "http://localhost:8080"; // Fallback for testing

            var response = await ForwardRequest(request, backendUrl);
            return response;
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { error = "Internal server error", message = ex.Message }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
    }

    private async Task<string> GetOpenAIApiKey()
    {
        try
        {
            var secretName = Environment.GetEnvironmentVariable("OPENAI_API_KEY_SECRET");
            if (string.IsNullOrEmpty(secretName))
            {
                throw new InvalidOperationException("OPENAI_API_KEY_SECRET environment variable not set");
            }

            var request = new GetSecretValueRequest { SecretId = secretName };
            var response = await _secretsClient.GetSecretValueAsync(request);
            return response.SecretString;
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Failed to retrieve OpenAI API key: {ex.Message}");
            throw;
        }
    }

    private async Task<APIGatewayProxyResponse> ForwardRequest(APIGatewayProxyRequest request, string backendUrl)
    {
        try
        {
            // Extract proxy path
            var path = request.PathParameters?.ContainsKey("proxy") == true 
                ? $"/api/{request.PathParameters["proxy"]}"
                : "/api";

            // Build query string from QueryStringParameters
            var queryString = "";
            if (request.QueryStringParameters != null && request.QueryStringParameters.Count > 0)
            {
                var queryParts = new List<string>();
                foreach (var param in request.QueryStringParameters)
                {
                    queryParts.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
                }
                queryString = "?" + string.Join("&", queryParts);
            }

            var requestUri = new Uri($"{backendUrl}{path}{queryString}");
            LambdaLogger.Log($"Forwarding to: {requestUri}");

            // Create HTTP request
            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(request.HttpMethod),
                RequestUri = requestUri
            };

            // Copy headers
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    if (!IsRestrictedHeader(header.Key))
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            // Copy body for POST/PUT/PATCH
            if (!string.IsNullOrEmpty(request.Body) && 
                (request.HttpMethod == "POST" || request.HttpMethod == "PUT" || request.HttpMethod == "PATCH"))
            {
                var body = request.IsBase64Encoded 
                    ? Convert.FromBase64String(request.Body)
                    : Encoding.UTF8.GetBytes(request.Body);
                
                httpRequest.Content = new ByteArrayContent(body);
            }

            // Send request to backend
            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Body = responseBody,
                Headers = new Dictionary<string, string>
                {
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Methods", "*" },
                    { "Access-Control-Allow-Headers", "*" },
                    { "Content-Type", httpResponse.Content.Headers.ContentType?.ToString() ?? "application/json" }
                }
            };
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error forwarding request: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 502,
                Body = JsonSerializer.Serialize(new { error = "Bad Gateway", message = ex.Message }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
    }

    private static bool IsRestrictedHeader(string headerName)
    {
        var restricted = new[] { "host", "connection", "content-length", "transfer-encoding" };
        return restricted.Contains(headerName.ToLower());
    }
}
