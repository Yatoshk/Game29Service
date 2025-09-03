//using Game29Prices.Data;

using Game29Prices.data;
using Game29Prices.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Game29Prices.Services;

public class ProductService(
    IDbContextFactory<ApplicationContext> contextFactory,
    ILogger<ProductService> logger,
    IConfiguration configuration)
    : IProductService
{
    public async Task<List<ProductPrice>> GetAllProducts()
    {
        using var context = contextFactory.CreateDbContext();
        return await context.ProductPrices
            .Where(p => p.PriceDate.Date == DateTime.UtcNow.Date)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Subcategory)
            .ThenBy(p => p.Product)
            .ToListAsync();
    }
    
    public async Task<List<ProductPrice>> GetProductByCategoriesAsync(string category)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.ProductPrices
            .Where(p => p.PriceDate.Date == DateTime.UtcNow.Date)
            .Where(p => p.Category == category)
            .OrderBy(p => p.Product)
            .ToListAsync();
    }

    public async Task<List<ProductPrice>> GetAllPricesProductByCodeAsync(string code)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.ProductPrices
            .Where(p => p.Code == code)
            .ToListAsync();
    }
    
    public async Task CleanOldRecordsAsync()
    {
        using var context = contextFactory.CreateDbContext();
        var cutoffDate = DateTime.UtcNow.AddDays(-int.Parse(configuration["SupplierSettings:KeepIntervalDays"]));
        var oldRecords = await context.ProductPrices
            .Where(p => p.PriceDate <= cutoffDate && p.PriceDate <= DateTime.UtcNow.AddDays(-2))
            .ToListAsync();
        
        if (oldRecords.Any())
        {
            context.ProductPrices.RemoveRange(oldRecords);
            await context.SaveChangesAsync();
            logger.LogInformation("Removed {Count} old records", oldRecords.Count);
        }
    }
}