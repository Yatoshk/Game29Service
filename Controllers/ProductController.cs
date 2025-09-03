using Game29Prices.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game29Prices.Controllers;

[ApiController]
public class ProductController(IProductService productService, ICreateTableService tableService, ILogger<ProductController> logger)
    : Controller
{

    [HttpGet("/products")]
    public async Task<ActionResult> GetAllProducts()
    {
        try
        {
            var products = await productService.GetAllProducts();
            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting categories");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("/products/code={code}")]
    public async Task<ActionResult> GetAllProducts(string code)
    {
        try
        {
            var products = await productService.GetAllPricesProductByCodeAsync(code);
            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting categories");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("/products/category={category}")]
    public async Task<ActionResult> GetProductsOnCategories(string category)
    {
        try
        {
            var categories = await productService.GetProductByCategoriesAsync(category);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting categories");
            return StatusCode(500, "Internal server error");
        }
    }
    [HttpGet("/download_table")]
    public async Task<IActionResult> DownloadTable()
    {
        try
        {
            var excelBytes = await tableService.CreateTableAsync();

            var fileName = $"products_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            logger.LogInformation("Excel file generated successfully, size: {Size} bytes", excelBytes.Length);

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Excel file");
            return StatusCode(500, "Internal server error");
        }
    }
}