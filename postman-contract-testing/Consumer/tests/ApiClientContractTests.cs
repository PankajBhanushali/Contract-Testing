using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Consumer;
using Xunit;

namespace ConsumerTests
{
    public class ApiClientContractTests
    {
        private readonly ApiClient _client = new(new Uri("http://localhost:5001"));

        [Fact]
        public async Task GetAllProducts_ShouldReturn200WithProductArray()
        {
            // Act
            var response = await _client.GetAllProducts();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(content);

            Assert.NotNull(products);
            Assert.NotEmpty(products);
        }

        [Fact]
        public async Task GetAllProducts_ResponseShouldContainRequiredFields()
        {
            // Act
            var response = await _client.GetAllProducts();
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(content);

            // Assert - Each product must have: id, name, type, version
            Assert.NotNull(products);
            foreach (var product in products)
            {
                Assert.True(product.TryGetProperty("id", out _), "Product must have 'id' field");
                Assert.True(product.TryGetProperty("name", out _), "Product must have 'name' field");
                Assert.True(product.TryGetProperty("type", out _), "Product must have 'type' field");
                Assert.True(product.TryGetProperty("version", out _), "Product must have 'version' field");
            }
        }

        [Fact]
        public async Task GetAllProducts_ProductFieldsShouldHaveCorrectTypes()
        {
            // Act
            var response = await _client.GetAllProducts();
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(content);

            // Assert
            Assert.NotNull(products);
            foreach (var product in products)
            {
                // ID should be a number
                product.TryGetProperty("id", out var id);
                Assert.Equal(JsonValueKind.Number, id.ValueKind);

                // Name should be a string
                product.TryGetProperty("name", out var name);
                Assert.Equal(JsonValueKind.String, name.ValueKind);

                // Type should be a string
                product.TryGetProperty("type", out var type);
                Assert.Equal(JsonValueKind.String, type.ValueKind);

                // Version should be a string
                product.TryGetProperty("version", out var version);
                Assert.Equal(JsonValueKind.String, version.ValueKind);
            }
        }

        [Fact]
        public async Task GetProduct_WithValidId_ShouldReturn200WithProduct()
        {
            // Act
            var response = await _client.GetProduct(10);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(content);

            Assert.Equal(JsonValueKind.Object, product.ValueKind);
        }

        [Fact]
        public async Task GetProduct_WithValidId_ShouldReturnCorrectProduct()
        {
            // Act
            var response = await _client.GetProduct(10);
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(content);

            // Assert - Product 10 is "28 Degrees"
            product.TryGetProperty("id", out var id);
            Assert.Equal(10, id.GetInt32());

            product.TryGetProperty("name", out var name);
            Assert.Equal("28 Degrees", name.GetString());
        }

        [Fact]
        public async Task GetProduct_WithValidId_ResponseShouldContainRequiredFields()
        {
            // Act
            var response = await _client.GetProduct(10);
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(content);

            // Assert - Must have id, name, type, version
            Assert.True(product.TryGetProperty("id", out _), "Product must have 'id' field");
            Assert.True(product.TryGetProperty("name", out _), "Product must have 'name' field");
            Assert.True(product.TryGetProperty("type", out _), "Product must have 'type' field");
            Assert.True(product.TryGetProperty("version", out _), "Product must have 'version' field");
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ShouldReturn404()
        {
            // Act
            var response = await _client.GetProduct(999);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ShouldReturnNotFoundResponse()
        {
            // Act
            var response = await _client.GetProduct(999);
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Should have some error response
            Assert.NotEmpty(content);
            
            // Try to parse as JSON - should be valid JSON error response
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal(JsonValueKind.Object, errorResponse.ValueKind);
        }

        [Fact]
        public async Task GetAllProducts_ShouldContainExpectedProducts()
        {
            // Act
            var response = await _client.GetAllProducts();
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<JsonElement>>(content);

            // Assert - Should contain products with IDs 9 and 10
            Assert.NotNull(products);
            var ids = products.Select(p =>
            {
                p.TryGetProperty("id", out var id);
                return id.GetInt32();
            }).ToList();

            Assert.Contains(9, ids);
            Assert.Contains(10, ids);
        }
    }
}
