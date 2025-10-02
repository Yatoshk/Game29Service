using System.Net;
using System.Text.RegularExpressions;
using Game29Prices.data;
using Game29Prices.Entities;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace Game29Prices.Services;

public class ProductParserService(
    IDbContextFactory<ApplicationContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<ProductParserService> logger,
    IConfiguration configuration)
    : IProductParserService
{
    public async Task ParseAndStoreProductAsync()
{
    try
    {
        logger.LogInformation("Starting price parsing from supplier...");

        await using var context = contextFactory.CreateDbContext();
        
        // Параллельный парсинг с пакетной записью
        await ParseAndStoreInBatchesAsync(context);
        
        logger.LogInformation("Price parsing completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred while parsing prices from supplier");
    }
}

private async Task ParseAndStoreInBatchesAsync(ApplicationContext context)
{
    var httpClient = httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

    try
    {
        var supplierUrl = configuration["SupplierSettings:BaseUrlGetNumberOfPages"];
        var htmlContent = await httpClient.GetStringAsync(supplierUrl);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        int pagesCount = ParsePagesCount(htmlDoc);
        logger.LogInformation($"Parsing count pages from: {supplierUrl}, count of pages {pagesCount}");

        const int batchSize = 50; 
        var currentBatch = new List<ProductPrice>();
        var totalProcessed = 0;

        // Параллельная обработка страниц
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(1, pagesCount),
            parallelOptions,
            async (pageNumber, cancellationToken) =>
            {
                try
                {
                    string pageUrl = configuration["SupplierSettings:BaseUrlPages"]?.Replace("{i}", pageNumber.ToString()) ?? string.Empty;
                    
                    var pageHtmlContent = await httpClient.GetStringAsync(pageUrl);
                    var pageHtmlDoc = new HtmlDocument();
                    pageHtmlDoc.LoadHtml(pageHtmlContent);
                    
                    List<string> productLinks = ParseProductLinks(pageHtmlDoc);
                    
                    foreach (var productLink in productLinks)
                    {
                        try
                        {
                            string productUrl = configuration["SupplierSettings:BaseUrl"] + productLink;
                            
                            var productHtmlContent = await httpClient.GetStringAsync(productUrl);
                            var productHtmlDoc = new HtmlDocument();
                            productHtmlDoc.LoadHtml(productHtmlContent);

                            var productPrices = ParseProduct(productHtmlDoc);
                            
                            lock (currentBatch)
                            {
                                currentBatch.AddRange(productPrices);
                                totalProcessed += productPrices.Count;

                                if (currentBatch.Count >= batchSize)
                                {
                                    SaveBatchToDatabase(context, currentBatch);
                                    currentBatch = new List<ProductPrice>();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Error parsing product {ProductLink}", productLink);
                        }
                    }

                    logger.LogInformation("Processed page {PageNumber}/{TotalPages}, total products: {Total}",
                        pageNumber, pagesCount, totalProcessed);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error processing page {PageNumber}", pageNumber);
                }
            });

        if (currentBatch.Any())
        {
            SaveBatchToDatabase(context, currentBatch);
        }

        logger.LogInformation("Parsed and stored {Count} products successfully", totalProcessed);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error parsing HTML content from supplier");
    }
}

private void SaveBatchToDatabase(ApplicationContext context, List<ProductPrice> batch)
{
    try
    {
        context.ProductPrices.AddRange(batch);
        context.SaveChanges();
        logger.LogInformation("Saved batch of {BatchSize} products to database", batch.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error saving batch to database");
    }
}

    private int ParsePagesCount(HtmlDocument htmlDoc)
    {
        try
        {
            var nextElement = htmlDoc.DocumentNode.SelectSingleNode("//li[@class='next']");
            var previousPageElement = nextElement.SelectSingleNode("./preceding-sibling::li[1]/a");
            string pageNumber = previousPageElement.InnerText.Trim();
            
            return int.Parse(pageNumber);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to determine last page number, using default (1)");
        }
        
        return 0;
    }

    private List<string> ParseProductLinks(HtmlDocument htmlDoc)
    {
        var productLinks = new List<string>();
    
        try
        {
            var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'row') and contains(@style, 'border: 2px solid #898989')]");
        
            foreach (var productNode in productNodes)
            {
                try
                {
                    var linkNode = productNode.SelectSingleNode(".//a[contains(@href, '/item')]");
                    var href = linkNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href) && href.StartsWith("/item"))
                    {
                        productLinks.Add(href);
                        logger.LogDebug("Found product link: {Link}", href);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error processing product node");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing product links");
        }
    
        return productLinks;
    }
    private List<ProductPrice> ParseProduct(HtmlDocument htmlDoc)
    {
        
        var prices = new List<ProductPrice>();

        try
        {
            ParseProductHeader(htmlDoc,  out var product, out var code);
            ParseProductCategories(htmlDoc,  out var category, out var subcategory);
            prices.Add(new ProductPrice
            {
                Category = category,
                Subcategory = subcategory,
                Product = product,
                Code = code,
                Price = ParseProductPrice(htmlDoc) ?? 0,
                PriceDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error parsing product node");
        }
        return prices;
    }
    
    private decimal? ParseProductPrice(HtmlDocument htmlDoc)
    {
        try
        {
            var priceDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@style='font-size: 26px;' and contains(., '&nbsp;')]/following-sibling::div[@style='font-size: 34px;']/span[1]");

            string price = priceDiv.InnerText.Trim().Replace(" ", "");
            return decimal.Parse(price);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error parsing product price");
            return null;
        }
    }
    
    private void ParseProductHeader(HtmlDocument htmlDoc,  out string product, out string code)
    {
        product = "";
        code = "";
        try
        {
            var headerBlock = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'tovar-header')]");
        
           
            var nameElement = headerBlock.SelectSingleNode(".//h2[@itemprop='name']");
            product = CleanText(nameElement.InnerText);
            
            
            var codeElement = headerBlock.SelectSingleNode(".//div[contains(., 'Код:')]");
            code = ExtractCode(codeElement.InnerText) ?? "";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error parsing product header");
        }
        
    }
    
    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
    
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private string? ExtractCode(string text)
    {
        var match = Regex.Match(text, @"Код:\s*(\S+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
    
    
    private void ParseProductCategories(HtmlDocument htmlDoc, out string category, out string subcategory)
    {
        category = "";
        subcategory = "";
        
        try
        {
            var categoryLinks = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'breadcrumbs-main')]/a[contains(@href, 'category=')]");
            
            if (categoryLinks.Count >= 3)
            {
                category = CleanCategoryName(categoryLinks[1].InnerText);
                
                subcategory = CleanCategoryName(categoryLinks[2].InnerText);
                
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error parsing categories, using fallback");
            
        }
    }
    
    
    private string CleanCategoryName(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return categoryName;
    
        var cleaned = WebUtility.HtmlDecode(categoryName)
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace("\t", "")
            .Trim();
    
        if (cleaned.StartsWith("Game29-"))
        {
            cleaned = cleaned.Substring(7).Trim();
        }
    
        return cleaned;
    }
}