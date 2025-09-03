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
                using (var scope = services.CreateScope())
                {
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    await productService.CleanOldRecordsAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning old records.");
            }

            await Task.Delay(TimeSpan.FromHours(int.Parse(configuration["SupplierSettings:UpdateIntervalHours"])), stoppingToken);
        }
    }
}