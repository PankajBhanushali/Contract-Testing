namespace SpecmaticProvider.Services;

using SpecmaticProvider.Models;

/// <summary>
/// In-memory product repository for demonstration
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(int skip = 0, int take = 10);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(CreateProductRequest request);
    Task<Product?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private static readonly List<Product> Products = [];
    private static int _nextId = 1;

    public ProductRepository()
    {
        // Initialize with sample data
        if (Products.Count == 0)
        {
            Products.AddRange(new[]
            {
                new Product
                {
                    Id = _nextId++,
                    Name = "Laptop",
                    Description = "High-performance laptop with 16GB RAM",
                    Price = 999.99m,
                    Discount = 10.5m,
                    InStock = true,
                    Quantity = 5,
                    Category = "Electronics",
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Product
                {
                    Id = _nextId++,
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse",
                    Price = 29.99m,
                    Discount = null,
                    InStock = true,
                    Quantity = 50,
                    Category = "Electronics",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Product
                {
                    Id = _nextId++,
                    Name = "USB-C Cable",
                    Description = "High-speed USB-C charging and data cable",
                    Price = 14.99m,
                    Discount = 5m,
                    InStock = true,
                    Quantity = 100,
                    Category = "Electronics",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            });
        }
    }

    public Task<IEnumerable<Product>> GetAllAsync(int skip = 0, int take = 10)
    {
        var result = Products
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult<IEnumerable<Product>>(result);
    }

    public Task<Product?> GetByIdAsync(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = _nextId++,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Discount = request.Discount,
            InStock = true,
            Quantity = 0,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };
        
        Products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
            return Task.FromResult<Product?>(null);

        if (!string.IsNullOrEmpty(request.Name))
            product.Name = request.Name;
        
        if (request.Description != null)
            product.Description = request.Description;
        
        if (request.Price.HasValue)
            product.Price = request.Price.Value;
        
        product.Discount = request.Discount ?? product.Discount;
        
        if (request.InStock.HasValue)
            product.InStock = request.InStock.Value;
        
        if (request.Quantity.HasValue)
            product.Quantity = request.Quantity.Value;
        
        if (!string.IsNullOrEmpty(request.Category))
            product.Category = request.Category;

        return Task.FromResult<Product?>(product);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
            return Task.FromResult(false);

        Products.Remove(product);
        return Task.FromResult(true);
    }
}
