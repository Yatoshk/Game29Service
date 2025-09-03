using Game29Prices.Entities;

namespace Game29Prices.Services;

public interface IProductService
{

    Task<List<ProductPrice>> GetAllProducts();
    Task<List<ProductPrice>> GetProductByCategoriesAsync(string category);
    Task<List<ProductPrice>> GetAllPricesProductByCodeAsync(string code);
    Task CleanOldRecordsAsync();
}