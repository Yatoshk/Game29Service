namespace Game29Prices.Services;

public class UpdateService(
    IServiceProvider services,
    ILogger<CleanupService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Price Update Service is starting.");

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting price update");
            
                using (var scope = services.CreateScope())
                {
                    var parserService = scope.ServiceProvider.GetRequiredService<IProductParserService>();
                    await parserService.ParseAndStoreProductAsync();
                }
            
                logger.LogInformation("Price update completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating prices.");
            }

            var nextRunTime = GetNextRunTime(int.Parse(configuration["SupplierSettings:UpdateHours"]), 0);
            var delay = nextRunTime - DateTime.Now;
        
            logger.LogInformation("Next price update scheduled at: {NextRunTime}", nextRunTime);
        
            await Task.Delay(delay, stoppingToken);
        }
    }

    private DateTime GetNextRunTime(int targetHour, int targetMinute)
    {
        var now = DateTime.Now;
        var todayTarget = new DateTime(now.Year, now.Month, now.Day, targetHour, targetMinute, 0);
    
        if (now > todayTarget)
        {
            return todayTarget.AddDays(1);
        }
    
        return todayTarget;
    }
}