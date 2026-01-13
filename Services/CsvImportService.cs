using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TransactionAPI.Data;
using TransactionAPI.Models;

namespace TransactionAPI.Services;

public class CsvImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(ApplicationDbContext context, ILogger<CsvImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ImportTransactionsFromCsvAsync(string csvFilePath)
    {
        var importedCount = 0;
        var transactions = new List<Transaction>();

        if (!File.Exists(csvFilePath))
        {
            _logger.LogWarning($"CSV file not found: {csvFilePath}");
            return 0;
        }

        var lines = await File.ReadAllLinesAsync(csvFilePath);
        
        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                // Parse CSV line (handling quoted values)
                var values = ParseCsvLine(line);
                
                if (values.Length < 7) continue;

                var transaction = new Transaction
                {
                    TransactionID = values[0].Trim(),
                    AccountID = values[1].Trim(),
                    Amount = decimal.Parse(values[2].Trim(), CultureInfo.InvariantCulture),
                    Merchant = values[3].Trim(),
                    Category = values[4].Trim(),
                    Timestamp = DateTime.Parse(values[5].Trim()),
                    Location = values[6].Trim().Trim('"')
                };

                // Check if transaction already exists
                var exists = await _context.Transactions.AnyAsync(t => t.TransactionID == transaction.TransactionID);
                if (!exists)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error parsing line {i + 1}: {line}");
            }
        }

        if (transactions.Any())
        {
            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
            importedCount = transactions.Count;
            _logger.LogInformation($"Imported {importedCount} transactions from CSV");
        }

        return importedCount;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var insideQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == ',' && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        values.Add(current.ToString()); // Add last value

        return values.ToArray();
    }
}
