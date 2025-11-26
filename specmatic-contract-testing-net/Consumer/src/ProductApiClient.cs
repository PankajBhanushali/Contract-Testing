using System.Net;
using System.Text;
using System.Text.Json;
using SpecmaticConsumer.Models;

namespace SpecmaticConsumer.Client;

/// <summary>
/// HTTP client for interacting with the Product API
/// </summary>
public class ProductApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductApiClient(string baseUrl = "http://localhost:5000")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Get health status
    /// </summary>
    public async Task<string> GetHealthAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("status").GetString() ?? "unknown";
    }

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync(int skip = 0, int take = 10)
    {
        var url = $"{_baseUrl}/api/products?skip={skip}&take={take}";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to get products: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions) ?? [];
        return products;
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    public async Task<Product> GetProductByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/products/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Product with ID {id} not found");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to get product: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(content, _jsonOptions);
        return product ?? throw new InvalidOperationException("Failed to deserialize product");
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/products", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to create product: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(responseContent, _jsonOptions);
        return product ?? throw new InvalidOperationException("Failed to deserialize created product");
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    public async Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PutAsync($"{_baseUrl}/api/products/{id}", content);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Product with ID {id} not found");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to update product: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(responseContent, _jsonOptions);
        return product ?? throw new InvalidOperationException("Failed to deserialize updated product");
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    public async Task DeleteProductAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/products/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Product with ID {id} not found");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to delete product: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }
}
