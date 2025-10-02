namespace Game29Prices.Services;

public class CleanupService(
    IServiceProvider services,
    ILogger<CleanupService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting price clean");
                
                using (var scope = services.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    await productService.CleanOldRecordsAsync();
                }
                
                logger.LogInformation("Price clean completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning old records.");
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