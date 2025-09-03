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
                using (var scope = services.CreateScope())
                {
                    var parserService = scope.ServiceProvider.GetRequiredService<IProductParserService>();
                    await parserService.ParseAndStoreProductAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating prices.");
            }

            await Task.Delay(TimeSpan.FromHours(int.Parse(configuration["SupplierSettings:UpdateIntervalHours"])), stoppingToken);
        }
    }
}