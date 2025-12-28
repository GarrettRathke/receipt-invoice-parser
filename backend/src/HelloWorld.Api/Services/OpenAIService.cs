using HelloWorld.Api.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;

namespace HelloWorld.Api.Services;

public class OpenAIService : IOpenAIService
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIService> _logger;
    private readonly OpenAIClient _openAIClient;

    public OpenAIService(IOptions<OpenAISettings> settings, ILogger<OpenAIService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _openAIClient = new OpenAIClient(_settings.ApiKey);
    }

    public async Task<ReceiptExtractionResponse> ExtractReceiptDataAsync(IFormFile imageFile)
    {
        try
        {
            _logger.LogInformation("Processing receipt image: {FileName}", imageFile.FileName);

            // Check if OpenAI API key is configured
            if (string.IsNullOrEmpty(_settings.ApiKey) || _settings.ApiKey == "your-openai-api-key-here")
            {
                _logger.LogWarning("OpenAI API key not configured, returning mock data");
                return await GenerateMockDataAsync();
            }

            // Convert image to base64
            var base64Image = await ConvertToBase64Async(imageFile);
            var mimeType = GetMimeType(imageFile.ContentType);

            // Create the vision chat completion request
            var messages = new List<ChatMessage>
            {
                new UserChatMessage(new List<ChatMessageContentPart>
                {
                    ChatMessageContentPart.CreateTextPart(CreateExtractionPrompt()),
                    ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(Convert.FromBase64String(base64Image)), mimeType)
                })
            };

            var chatRequest = new ChatCompletionOptions
            {
                MaxTokens = _settings.MaxTokens,
                Temperature = (float)_settings.Temperature,
            };

            foreach (var message in messages)
            {
                chatRequest.Messages.Add(message);
            }

            _logger.LogInformation("Sending request to OpenAI Vision API");

            // Call OpenAI API
            var response = await _openAIClient.GetChatClient(_settings.Model).CompleteChatAsync(chatRequest);

            if (response?.Value?.Content?.Count > 0)
            {
                var content = response.Value.Content[0].Text;
                var extractedData = ParseExtractionResponse(content);

                _logger.LogInformation("Successfully extracted data from receipt");

                return new ReceiptExtractionResponse(
                    extractedData,
                    "Success",
                    DateTime.UtcNow
                );
            }

            _logger.LogWarning("No content received from OpenAI API");
            return new ReceiptExtractionResponse(
                new Dictionary<string, object> { ["message"] = "No content extracted from image" },
                "Error",
                DateTime.UtcNow,
                "No content received from OpenAI API"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting receipt data");
            return new ReceiptExtractionResponse(
                new Dictionary<string, object>(),
                "Error",
                DateTime.UtcNow,
                ex.Message
            );
        }
    }

    private async Task<ReceiptExtractionResponse> GenerateMockDataAsync()
    {
        var mockData = new Dictionary<string, object>
        {
            ["business_name"] = "Demo Coffee Shop",
            ["date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["time"] = DateTime.Now.ToString("HH:mm"),
            ["total"] = "15.47",
            ["item_1"] = "Large Coffee - $4.50",
            ["item_2"] = "Blueberry Muffin - $3.25",
            ["item_3"] = "Sandwich - $7.72",
            ["subtotal"] = "15.47",
            ["tax"] = "0.00",
            ["payment_method"] = "Credit Card",
            ["receipt_number"] = "12345",
            ["note"] = "Mock data - configure OpenAI API key for real extraction"
        };

        return new ReceiptExtractionResponse(
            mockData,
            "Success",
            DateTime.UtcNow
        );
    }

    private static string GetMimeType(string contentType)
    {
        return contentType switch
        {
            "image/png" => "image/png",
            "image/jpeg" => "image/jpeg",
            "image/jpg" => "image/jpeg",
            _ => "image/jpeg"
        };
    }

    private async Task<string> ConvertToBase64Async(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        return Convert.ToBase64String(bytes);
    }

    private string CreateExtractionPrompt()
    {
        return """
        Analyze this receipt or invoice image and extract ALL visible text data into a JSON format with flexible key-value pairs. 
        
        Extract everything you can see including:
        - Business/vendor information (name, address, phone, etc.)
        - Transaction details (date, time, receipt number, etc.)
        - All line items with descriptions and prices
        - Subtotal, tax, tips, total amounts
        - Any other visible text or numbers
        
        For unclear or partial text, include your best interpretation. Choose the most probable option for ambiguous text.
        
        Return ONLY a valid JSON object with string keys and values (numbers should be strings). Example format:
        {
          "business_name": "Store Name",
          "date": "2024-01-15",
          "total": "25.99",
          "item_1": "Coffee - $4.50",
          "item_2": "Sandwich - $12.99",
          "tax": "2.15",
          "address": "123 Main St"
        }
        """;
    }

    private Dictionary<string, object> ParseExtractionResponse(string response)
    {
        try
        {
            // Clean up the response to extract just the JSON
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart);
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                return result ?? new Dictionary<string, object>();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse OpenAI response as JSON: {Response}", response);
        }

        // Fallback: return raw response as single key-value
        return new Dictionary<string, object> { ["raw_response"] = response };
    }
}
