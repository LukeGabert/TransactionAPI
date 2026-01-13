using Microsoft.EntityFrameworkCore;
using TransactionAPI.Data;
using TransactionAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework Core with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for OpenAI API calls and register AnomalyDetectionService
builder.Services.AddHttpClient<TransactionAPI.Services.AnomalyDetectionService>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is created (for development only)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS (must be before UseHttpsRedirection and endpoints)
app.UseCors();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Risk Reports API endpoints
app.MapGet("/api/risk-reports", async (ApplicationDbContext db) =>
{
    try
    {
        var riskReports = await db.RiskReports
            .Include(r => r.Transaction)
            .OrderByDescending(r => r.ReportID)
            .Select(r => new
            {
                reportID = r.ReportID,
                transactionID = r.TransactionID,
                riskLevel = r.RiskLevel.ToString(),
                detectedAnomaly = r.DetectedAnomaly,
                recommendedMitigation = r.RecommendedMitigation,
                transaction = r.Transaction != null ? new
                {
                    transactionID = r.Transaction.TransactionID,
                    accountID = r.Transaction.AccountID,
                    amount = r.Transaction.Amount,
                    merchant = r.Transaction.Merchant,
                    category = r.Transaction.Category,
                    timestamp = r.Transaction.Timestamp,
                    location = r.Transaction.Location
                } : (object?)null
            })
            .ToListAsync();

        return Results.Ok(riskReports);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error fetching risk reports",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
})
.WithName("GetRiskReports")
.WithTags("Risk Reports")
.Produces<List<object>>(StatusCodes.Status200OK)
.WithOpenApi();

app.MapGet("/api/risk-reports/{id}", async (int id, ApplicationDbContext db) =>
{
    var riskReport = await db.RiskReports
        .Include(r => r.Transaction)
        .FirstOrDefaultAsync(r => r.ReportID == id);

    if (riskReport == null)
    {
        return Results.NotFound(new { message = "Risk report not found" });
    }

    var result = new
    {
        reportID = riskReport.ReportID,
        transactionID = riskReport.TransactionID,
        riskLevel = riskReport.RiskLevel.ToString(),
        detectedAnomaly = riskReport.DetectedAnomaly,
        recommendedMitigation = riskReport.RecommendedMitigation,
        transaction = riskReport.Transaction != null ? new
        {
            transactionID = riskReport.Transaction.TransactionID,
            accountID = riskReport.Transaction.AccountID,
            amount = riskReport.Transaction.Amount,
            merchant = riskReport.Transaction.Merchant,
            category = riskReport.Transaction.Category,
            timestamp = riskReport.Transaction.Timestamp,
            location = riskReport.Transaction.Location
        } : (object?)null
    };

    return Results.Ok(result);
})
.WithName("GetRiskReportById")
.WithTags("Risk Reports")
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapPut("/api/risk-reports/{id}/resolve", async (int id, ApplicationDbContext db) =>
{
    var riskReport = await db.RiskReports.FindAsync(id);

    if (riskReport == null)
    {
        return Results.NotFound(new { message = "Risk report not found" });
    }

    // Remove the risk report to mark it as resolved/mitigated
    db.RiskReports.Remove(riskReport);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Risk report marked as resolved", reportID = id });
})
.WithName("ResolveRiskReport")
.WithTags("Risk Reports")
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

// Scan Transactions endpoint - triggers AI analysis
app.MapPost("/api/transactions/scan", async (
    TransactionAPI.Services.AnomalyDetectionService anomalyDetectionService,
    ApplicationDbContext db,
    ILogger<Program> logger,
    IServiceProvider serviceProvider) =>
{
    try
    {
        logger.LogInformation("Starting transaction scan for anomaly detection...");
        
        // Check if database has transactions, if not, import from CSV
        var transactionCount = await db.Transactions.CountAsync();
        if (transactionCount == 0)
        {
            logger.LogInformation("No transactions found in database. Importing from CSV...");
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "transactions.csv");
            var importLogger = serviceProvider.GetRequiredService<ILogger<TransactionAPI.Services.CsvImportService>>();
            var importService = new TransactionAPI.Services.CsvImportService(db, importLogger);
            var importedCount = await importService.ImportTransactionsFromCsvAsync(csvPath);
            logger.LogInformation($"Imported {importedCount} transactions from CSV");
        }
        
        const int batchSize = 50;
        
        // Get total transactions in database
        var totalTransactions = await db.Transactions.CountAsync();
        
        // Get all transaction IDs in order
        var allTransactionIds = await db.Transactions
            .OrderBy(t => t.TransactionID)
            .Select(t => t.TransactionID)
            .ToListAsync();
        
        // Get all transaction IDs that have risk reports (these have definitely been analyzed)
        var analyzedTransactionIds = (await db.RiskReports
            .Select(r => r.TransactionID)
            .Distinct()
            .ToListAsync()).ToHashSet();
        
        // Find the highest index of any analyzed transaction
        // Since we process in batches of 50 in order, find the highest position
        int skipCount = 0;
        if (analyzedTransactionIds.Any())
        {
            // Find the index of the highest analyzed transaction ID
            int highestAnalyzedIndex = -1;
            for (int i = 0; i < allTransactionIds.Count; i++)
            {
                if (analyzedTransactionIds.Contains(allTransactionIds[i]))
                {
                    highestAnalyzedIndex = i;
                }
            }
            
            // Round up to the next batch boundary
            skipCount = highestAnalyzedIndex >= 0 
                ? ((highestAnalyzedIndex / batchSize) + 1) * batchSize
                : 0;
        }
        
        // Ensure we don't exceed total transactions
        skipCount = Math.Min(skipCount, totalTransactions);
        
        // Check if we've analyzed all transactions
        if (skipCount >= totalTransactions)
        {
            return Results.Ok(new 
            { 
                message = "All transactions have already been analyzed",
                reportsCreated = 0,
                transactionsAnalyzed = 0,
                totalAnalyzed = totalTransactions,
                totalTransactions = totalTransactions
            });
        }
        
        var reportsCreated = await anomalyDetectionService.AnalyzeTransactionsAsync(batchSize: batchSize, skip: skipCount);
        var actualAnalyzed = Math.Min(batchSize, totalTransactions - skipCount);
        var newAnalyzedCount = skipCount + actualAnalyzed;
        
        return Results.Ok(new 
        { 
            message = "Transaction scan completed successfully",
            reportsCreated = reportsCreated,
            transactionsAnalyzed = actualAnalyzed,
            totalAnalyzed = newAnalyzedCount,
            totalTransactions = totalTransactions
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during transaction scan");
        return Results.Problem(
            title: "Error scanning transactions",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
})
.WithName("ScanTransactions")
.WithTags("Transactions")
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi();

// Get transaction statistics endpoint
app.MapGet("/api/transactions/stats", async (ApplicationDbContext db) =>
{
    try
    {
        var totalTransactions = await db.Transactions.CountAsync();
        return Results.Ok(new { totalTransactions });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error fetching transaction statistics",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
})
.WithName("GetTransactionStats")
.WithTags("Transactions")
.Produces<object>(StatusCodes.Status200OK)
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
