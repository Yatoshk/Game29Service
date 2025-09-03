using System;
using System.Collections.Generic;
using Game29Prices.Entities;
using Microsoft.EntityFrameworkCore;

namespace Game29Prices.data;

public partial class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) => Database.EnsureCreated();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.LogTo(Console.WriteLine);
    }    
    
    public DbSet<ProductPrice> ProductPrices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductPrice>(entity =>
        {
            entity.ToTable("ProductPrices");
            
            modelBuilder.Entity<ProductPrice>().HasKey(u => u.Id);

            entity.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(u => u.Category).HasColumnName("category");
            entity.Property(u => u.Subcategory).HasColumnName("subcategory");
            entity.Property(u => u.Product).HasColumnName("product");
            entity.Property(u => u.Code).HasColumnName("code");
            entity.Property(u => u.Price).HasColumnName("price");
            entity.Property(u => u.PriceDate).HasColumnName("price_data");
        
            entity.HasIndex(u => u.Category);
            
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
