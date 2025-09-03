using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Game29Prices.Entities;

public class ProductPrice
{
    public string Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Subcategory { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime PriceDate { get; init; } = DateTime.UtcNow;
}