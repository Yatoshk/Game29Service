using ClosedXML.Excel;
using Game29Prices.data;
using Game29Prices.Entities;

namespace Game29Prices.Services;

public class CreateTableService(IProductService productService, ILogger<CreateTableService> logger)
    : ICreateTableService
{
    public async Task<byte[]> CreateTableAsync()
    {
        try 
        {
            logger.LogInformation("Starting Excel file creation...");

            var products = await productService.GetAllProducts();

            using (var workbook = new XLWorkbook())
            using (var memoryStream = new MemoryStream())
            {
                if (products.Any())
                {
                    var categories = products.GroupBy(p => p.Category);
                    foreach (var categoryGroup in categories)
                    {
                        var categoryProductsWithPrices = await GetProductsWithPricesAsync(categoryGroup.ToList());
                        CreateCategorySheet(workbook, categoryGroup.Key, categoryProductsWithPrices);
                    }
                
                    logger.LogInformation("Created report with {Count} products", products.Count);
                }
                else
                {
                    var worksheet = workbook.Worksheets.Add("Info");
                    worksheet.Cell(1, 1).Value = "No products found in database";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    logger.LogWarning("No products found for Excel report");
                }

                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray(); 
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Excel file");
            return []; 
        }
    }

    private async Task<List<ProductWithPrices>> GetProductsWithPricesAsync(List<ProductPrice> products)
    {
        var result = new List<ProductWithPrices>();
        var uniqueProducts = products
            .GroupBy(p => p.Code)
            .Select(g => g.First()) 
            .ToList();
        
        foreach (var product in uniqueProducts)
        {
            var prices = await productService.GetAllPricesProductByCodeAsync(product.Code);
            var orderedPrices = prices.OrderByDescending(p => p.PriceDate).ToList();
            
            result.Add(new ProductWithPrices
            {
                Product = product,
                LastPrice = orderedPrices.Count >= 1 ? orderedPrices[0].Price : product.Price,
                PreviousPrice = orderedPrices.Count >= 2 ? orderedPrices[1].Price : product.Price,
                LastPriceDate = orderedPrices.Count >= 1 ? orderedPrices[0].PriceDate : product.PriceDate,
                PreviousPriceDate = orderedPrices.Count >= 2 ? orderedPrices[1].PriceDate : product.PriceDate
            });
        }
        
        return result;
    }
    
    private void CreateCategorySheet(IXLWorkbook workbook, string categoryName, List<ProductWithPrices> categoryProducts)
    {
        var sheetName = CleanSheetName(categoryName);
        if (string.IsNullOrEmpty(sheetName)) sheetName = "Без категории";
        if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

        var worksheet = workbook.Worksheets.Add(sheetName);

        worksheet.Cell(1, 1).Value = $"Категория: {categoryName}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 8).Merge();

        var headers = new[]
        {
            "Подкатегория", "Название товара", "Код", "Старая Цена", "Дата изменения", "Новая Цена", "Дата изменения", "Изменение цены"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(3, i + 1).Value = headers[i];
            worksheet.Cell(3, i + 1).Style.Font.Bold = true;
            worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        
        for (int i = 0; i < categoryProducts.Count; i++)
        {
            var productWithPrices = categoryProducts[i];
            var product = productWithPrices.Product;
            var row = i + 4;

            worksheet.Cell(row, 1).Value = product.Subcategory;
            worksheet.Cell(row, 2).Value = product.Product;
            worksheet.Cell(row, 3).Value = product.Code;
            worksheet.Cell(row, 4).Value = productWithPrices.PreviousPrice;
            worksheet.Cell(row, 5).Value = productWithPrices.PreviousPriceDate;
            worksheet.Cell(row, 6).Value = productWithPrices.LastPrice;
            worksheet.Cell(row, 7).Value = productWithPrices.LastPriceDate;
            worksheet.Cell(row, 8).Value = productWithPrices.LastPrice - productWithPrices.PreviousPrice;
            
            if (productWithPrices.LastPrice != productWithPrices.PreviousPrice)
            {
                var changeCell = worksheet.Cell(row, 8);
                if (productWithPrices.LastPrice > productWithPrices.PreviousPrice)
                {
                    changeCell.Style.Font.FontColor = XLColor.Red;
                }
                else
                {
                    changeCell.Style.Font.FontColor = XLColor.Green;
                }
            }
        }

        worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00 руб.";
        worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00 руб.";
        worksheet.Column(8).Style.NumberFormat.Format = "#,##0.00 руб.";
        worksheet.Column(5).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        worksheet.Column(7).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        
        worksheet.Columns().AdjustToContents();
        worksheet.Range(3, 1, categoryProducts.Count + 3, headers.Length).SetAutoFilter();
        worksheet.SheetView.FreezeRows(3);

        logger.LogDebug("Created sheet for category: {Category} with {Count} products", 
            categoryName, categoryProducts.Count);
    }

    private string CleanSheetName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var invalidChars = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        foreach (var c in invalidChars)
        {
            name = name.Replace(c, ' ');
        }

        return name.Trim();
    }
    private class ProductWithPrices
    {
        public ProductPrice Product { get; set; }
        public decimal LastPrice { get; set; }
        public decimal PreviousPrice { get; set; }
        public DateTime LastPriceDate { get; set; }
        public DateTime PreviousPriceDate { get; set; }
    }
}