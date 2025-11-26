namespace SpecmaticProvider.Models;

/// <summary>
/// Represents a product in the system
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
    public bool InStock { get; set; }
    public int Quantity { get; set; }
    public string Category { get; set; } = "Other";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a product
/// </summary>
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? Discount { get; set; }
    public string Category { get; set; } = "Other";
}

/// <summary>
/// Request model for updating a product
/// </summary>
public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public decimal? Discount { get; set; }
    public bool? InStock { get; set; }
    public int? Quantity { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Validation error response
/// </summary>
public class ValidationErrorResponse
{
    public string Code { get; set; } = "VALIDATION_ERROR";
    public string Message { get; set; } = "Validation failed";
    public List<ValidationError> Errors { get; set; } = [];
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
}
