using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TransactionAPI.Data;
using TransactionAPI.Models;

namespace TransactionAPI.Services;

public class AnomalyDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(
        ApplicationDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnomalyDetectionService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a batch of transactions for anomalies using OpenAI
    /// </summary>
    /// <param name="batchSize">Number of transactions to analyze (default: 50)</param>
    /// <param name="skip">Number of transactions to skip (for pagination)</param>
    /// <returns>Number of risk reports created</returns>
    public async Task<int> AnalyzeTransactionsAsync(int batchSize = 50, int skip = 0)
    {
        try
        {
            // Read transactions from database
            var transactions = await ReadTransactionsBatchAsync(batchSize, skip);
            
            if (!transactions.Any())
            {
                _logger.LogInformation("No transactions found to analyze.");
                return 0;
            }

            _logger.LogInformation($"Analyzing {transactions.Count} transactions for anomalies...");

            // Call OpenAI API
            var riskAssessments = await CallOpenAIAsync(transactions);

            // Save risk reports to database
            var reportsCreated = await SaveRiskReportsAsync(riskAssessments);

            _logger.LogInformation($"Created {reportsCreated} risk reports.");
            return reportsCreated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing transactions for anomalies");
            throw;
        }
    }

    /// <summary>
    /// Reads a batch of transactions from the database
    /// </summary>
    private async Task<List<Transaction>> ReadTransactionsBatchAsync(int batchSize, int skip)
    {
        return await _context.Transactions
            .OrderBy(t => t.TransactionID)
            .Skip(skip)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Calls OpenAI API to analyze transactions for anomalies
    /// </summary>
    private async Task<List<RiskAssessmentResponse>> CallOpenAIAsync(List<Transaction> transactions)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set it in appsettings.json under 'OpenAI:ApiKey'");
        }

        // Format transactions as JSON for the prompt
        var transactionsJson = FormatTransactionsForPrompt(transactions);

        // Create the prompt
        var prompt = CreatePrompt(transactionsJson);

        // Prepare the request
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a Forensic Financial Auditor with expertise in detecting fraudulent and suspicious financial transactions. Analyze transactions carefully and identify any anomalies using systematic forensic analysis."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            response_format = new { type = "json_object" },
            temperature = 0.3
        };

        // Make the API call
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"OpenAI API returned {response.StatusCode}: {errorContent}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException("OpenAI API rate limit exceeded. Please wait a few minutes and try again. You may have exceeded your API quota or request limit.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("OpenAI API key is invalid or expired. Please check your API key configuration.");
            }
            else
            {
                throw new InvalidOperationException($"OpenAI API error ({response.StatusCode}): {errorContent}");
            }
        }

        var responseContent = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
        
        if (responseContent?.Choices == null || !responseContent.Choices.Any())
        {
            throw new InvalidOperationException("OpenAI API returned an empty response");
        }

        var content = responseContent.Choices[0].Message?.Content;
        
        if (string.IsNullOrEmpty(content))
        {
            throw new InvalidOperationException("OpenAI API returned an empty content");
        }
        
        // Parse the JSON response
        return ParseRiskAssessmentResponse(content);
    }

    /// <summary>
    /// Formats transactions as JSON string for the prompt
    /// </summary>
    private string FormatTransactionsForPrompt(List<Transaction> transactions)
    {
        var transactionData = transactions.Select(t => new
        {
            TransactionID = t.TransactionID,
            AccountID = t.AccountID,
            Amount = t.Amount,
            Merchant = t.Merchant,
            Category = t.Category,
            Timestamp = t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            Location = t.Location
        });

        return JsonSerializer.Serialize(transactionData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Creates the prompt for OpenAI
    /// </summary>
    private string CreatePrompt(string transactionsJson)
    {
        return $@"You are a Forensic Financial Auditor. Analyze the following transactions and identify any that appear suspicious or anomalous using systematic forensic analysis.

Consider factors such as:
- Unusually high transaction amounts
- Rapid location changes (transactions from different countries within minutes)
- Repeated small transactions to the same vendor
- Transactions inconsistent with account patterns
- Other suspicious patterns

Here are the transactions to analyze:
{transactionsJson}

For each suspicious transaction, return a JSON object with the following structure:
{{
  ""suspiciousTransactions"": [
    {{
      ""TransactionID"": ""string"",
      ""RiskLevel"": ""Low"" | ""Medium"" | ""High"",
      ""MitigationStrategy"": ""string"",
      ""Reasoning"": ""string"",
      ""tldr"": ""string""
    }}
  ]
}}

RiskLevel should be one of: Low, Medium, or High.

MitigationStrategy should be a clear, concise recommendation such as:
- ""Flag for manual review""
- ""Temporarily freeze account""
- ""Request additional verification""
- ""Monitor account activity""
- Or other appropriate mitigation strategies

Reasoning must follow a Chain of Thought (CoT) pattern with three components (keep it concise and professional):
1. Observation: What specific data point looks odd?
2. Context: Why is this unusual for this specific account or category?
3. Risk: What is the potential impact (e.g., suspected account takeover, duplicate billing)?

Example Reasoning format: ""Observation: Transaction amount of $15,000 is 50x the account's average. Context: Account typically shows $50-200 grocery transactions. Risk: Potential account takeover or unauthorized large purchase.""

tldr should be a brief summary of the anomaly (maximum 5 words). Example: ""$15,000 transaction exceeds account average""

Only include transactions that you identify as suspicious. If no transactions are suspicious, return an empty array.

Return ONLY valid JSON, no additional text or explanation.";
    }

    /// <summary>
    /// Parses the JSON response from OpenAI
    /// </summary>
    private List<RiskAssessmentResponse> ParseRiskAssessmentResponse(string jsonContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<OpenAIRiskAssessmentResponse>(jsonContent, options);
            return response?.SuspiciousTransactions ?? new List<RiskAssessmentResponse>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI response JSON: {JsonContent}", jsonContent);
            throw new InvalidOperationException("Failed to parse OpenAI API response", ex);
        }
    }

    /// <summary>
    /// Saves risk reports to the database
    /// </summary>
    private async Task<int> SaveRiskReportsAsync(List<RiskAssessmentResponse> riskAssessments)
    {
        if (!riskAssessments.Any())
        {
            return 0;
        }

        var reportsToAdd = new List<RiskReport>();
        int updatedCount = 0;

        foreach (var assessment in riskAssessments)
        {
            // Check if transaction exists
            var transactionExists = await _context.Transactions
                .AnyAsync(t => t.TransactionID == assessment.TransactionID);

            if (!transactionExists)
            {
                _logger.LogWarning($"Transaction {assessment.TransactionID} not found in database. Skipping risk report.");
                continue;
            }

            // Check if risk report already exists for this transaction
            var existingReport = await _context.RiskReports
                .FirstOrDefaultAsync(r => r.TransactionID == assessment.TransactionID);

            if (existingReport != null)
            {
                // Update existing report
                existingReport.RiskLevel = ParseRiskLevel(assessment.RiskLevel);
                existingReport.DetectedAnomaly = $"Risk assessment: {assessment.RiskLevel}";
                existingReport.RecommendedMitigation = assessment.MitigationStrategy;
                existingReport.Reasoning = assessment.Reasoning;
                existingReport.TLDR = assessment.TLDR;
                _context.RiskReports.Update(existingReport);
                updatedCount++;
            }
            else
            {
                // Create new report
                var riskReport = new RiskReport
                {
                    TransactionID = assessment.TransactionID,
                    RiskLevel = ParseRiskLevel(assessment.RiskLevel),
                    DetectedAnomaly = $"Risk assessment: {assessment.RiskLevel}",
                    RecommendedMitigation = assessment.MitigationStrategy,
                    Reasoning = assessment.Reasoning,
                    TLDR = assessment.TLDR
                };
                reportsToAdd.Add(riskReport);
            }
        }

        if (reportsToAdd.Any())
        {
            await _context.RiskReports.AddRangeAsync(reportsToAdd);
        }

        await _context.SaveChangesAsync();
        return reportsToAdd.Count + updatedCount;
    }

    /// <summary>
    /// Parses risk level string to RiskLevel enum
    /// </summary>
    private RiskLevel ParseRiskLevel(string riskLevel)
    {
        return riskLevel.ToLower() switch
        {
            "low" => RiskLevel.Low,
            "medium" => RiskLevel.Medium,
            "high" => RiskLevel.High,
            _ => RiskLevel.Medium // Default to Medium if parsing fails
        };
    }

    #region DTOs for OpenAI API

    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }
    }

    private class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
    }

    private class OpenAIMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class OpenAIRiskAssessmentResponse
    {
        [JsonPropertyName("suspiciousTransactions")]
        public List<RiskAssessmentResponse>? SuspiciousTransactions { get; set; }
    }

    private class RiskAssessmentResponse
    {
        [JsonPropertyName("TransactionID")]
        public string TransactionID { get; set; } = string.Empty;

        [JsonPropertyName("RiskLevel")]
        public string RiskLevel { get; set; } = string.Empty;

        [JsonPropertyName("MitigationStrategy")]
        public string MitigationStrategy { get; set; } = string.Empty;

        [JsonPropertyName("Reasoning")]
        public string? Reasoning { get; set; }

        [JsonPropertyName("tldr")]
        public string? TLDR { get; set; }
    }

    #endregion
}
