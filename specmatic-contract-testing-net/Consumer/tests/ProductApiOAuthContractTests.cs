using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Consumer.OAuth;
using FluentAssertions;
using Xunit;

namespace Consumer.Tests
{
    /// <summary>
    /// OAuth Contract Tests - Validates OAuth-secured API behavior.
    /// Tests OAuth token acquisition, injection, and authorization enforcement.
    /// </summary>
    public class ProductApiOAuthContractTests : IAsyncLifetime
    {
        private readonly HttpClient _httpClient;
        private ProductApiOAuthClient? _oauthClient;
        private const string BaseUrl = "http://localhost:5000";

        public ProductApiOAuthContractTests()
        {
            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            // Create OAuth client
            _oauthClient = new ProductApiOAuthClient(_httpClient, BaseUrl, $"{BaseUrl}/oauth/token");

            // Wait for provider to be ready
            int maxRetries = 30;
            while (maxRetries > 0)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{BaseUrl}/api/health");
                    if (response.IsSuccessStatusCode)
                        break;
                }
                catch { }

                maxRetries--;
                if (maxRetries > 0)
                    await Task.Delay(500);
            }

            if (maxRetries == 0)
                throw new InvalidOperationException("Provider API did not start in time");
        }

        public async Task DisposeAsync()
        {
            _oauthClient?.Dispose();
            _httpClient?.Dispose();
        }

        #region OAuth Token Endpoint Tests

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task OAuthTokenEndpoint_WithValidClientCredentials_ReturnsAccessToken()
        {
            // Arrange
            var tokenUrl = $"{BaseUrl}/oauth/token";
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", "test-client" },
                { "client_secret", "test-secret" },
                { "scope", "read:products write:products" }
            });

            // Act
            var response = await _httpClient.PostAsync(tokenUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                var root = jsonDoc.RootElement;
                root.TryGetProperty("access_token", out var token).Should().BeTrue();
                root.TryGetProperty("token_type", out var tokenType).Should().BeTrue();
                root.TryGetProperty("expires_in", out var expiresIn).Should().BeTrue();

                token.GetString().Should().NotBeNullOrEmpty();
                tokenType.GetString().Should().Be("Bearer");
                expiresIn.GetInt32().Should().BeGreaterThan(0);
            }
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task OAuthTokenEndpoint_WithNoScope_ReturnsTokenWithDefaultScopes()
        {
            // Arrange
            var tokenUrl = $"{BaseUrl}/oauth/token";
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", "test-client" },
                { "client_secret", "test-secret" }
                // No scope specified
            });

            // Act
            var response = await _httpClient.PostAsync(tokenUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseContent.Should().Contain("access_token");
        }

        #endregion

        #region Authorization Tests - Missing Token

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetAllProducts_WithoutToken_Returns401Unauthorized()
        {
            // Arrange & Act
            var response = await _oauthClient!.GetAllProductsWithoutTokenAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetProductById_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/products/1");
            // Intentionally not setting Authorization header

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task CreateProduct_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var productJson = JsonSerializer.Serialize(new
            {
                name = "Test Product",
                price = 99.99,
                category = "Electronics"
            });
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/products")
            {
                Content = new StringContent(productJson, System.Text.Encoding.UTF8, "application/json")
            };

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task UpdateProduct_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var productJson = JsonSerializer.Serialize(new { name = "Updated Product" });
            var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/api/products/1")
            {
                Content = new StringContent(productJson, System.Text.Encoding.UTF8, "application/json")
            };

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task DeleteProduct_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/api/products/1");

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests - Invalid Token

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetAllProducts_WithInvalidToken_Returns401Unauthorized()
        {
            // Arrange & Act
            var response = await _oauthClient!.GetAllProductsWithCustomAuthAsync("Bearer invalid-token-12345");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetAllProducts_WithMalformedAuthHeader_Returns401Unauthorized()
        {
            // Arrange & Act
            var response = await _oauthClient!.GetAllProductsWithCustomAuthAsync("InvalidTokenFormat");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authorization Tests - Valid Token

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetAllProducts_WithValidToken_Returns200Ok()
        {
            // Arrange & Act
            var response = await _oauthClient!.GetAllProductsAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<object>>(content);
            products.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetProductById_WithValidToken_Returns200Ok()
        {
            // Arrange - First create a product
            var createJson = JsonSerializer.Serialize(new
            {
                name = "OAuth Test Product",
                price = 299.99,
                category = "Electronics"
            });
            var createResponse = await _oauthClient!.CreateProductAsync(createJson);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdContent = await createResponse.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(createdContent);
            var productId = product.GetProperty("id").GetInt32();

            // Act
            var response = await _oauthClient!.GetProductAsync(productId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var retrievedProduct = JsonSerializer.Deserialize<JsonElement>(content);
            retrievedProduct.GetProperty("id").GetInt32().Should().Be(productId);
            retrievedProduct.GetProperty("name").GetString().Should().Be("OAuth Test Product");
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task CreateProduct_WithValidToken_Returns201Created()
        {
            // Arrange
            var productJson = JsonSerializer.Serialize(new
            {
                name = "New OAuth Product",
                price = 149.99,
                category = "Electronics",
                description = "Created with OAuth token"
            });

            // Act
            var response = await _oauthClient!.CreateProductAsync(productJson);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(content);
            product.GetProperty("name").GetString().Should().Be("New OAuth Product");
            product.GetProperty("price").GetDouble().Should().Be(149.99);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task UpdateProduct_WithValidToken_Returns200Ok()
        {
            // Arrange - Create a product first
            var createJson = JsonSerializer.Serialize(new
            {
                name = "Product to Update",
                price = 199.99,
                category = "Books"
            });
            var createResponse = await _oauthClient.CreateProductAsync(createJson);
            var createdContent = await createResponse.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(createdContent);
            var productId = product.GetProperty("id").GetInt32();

            // Act - Update it
            var updateJson = JsonSerializer.Serialize(new { name = "Updated via OAuth", price = 249.99 });
            var response = await _oauthClient.UpdateProductAsync(productId, updateJson);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var updatedProduct = JsonSerializer.Deserialize<JsonElement>(content);
            updatedProduct.GetProperty("name").GetString().Should().Be("Updated via OAuth");
            updatedProduct.GetProperty("price").GetDouble().Should().Be(249.99);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task DeleteProduct_WithValidToken_Returns204NoContent()
        {
            // Arrange - Create a product first
            var createJson = JsonSerializer.Serialize(new
            {
                name = "Product to Delete",
                price = 99.99,
                category = "Home"
            });
            var createResponse = await _oauthClient.CreateProductAsync(createJson);
            var createdContent = await createResponse.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<JsonElement>(createdContent);
            var productId = product.GetProperty("id").GetInt32();

            // Act - Delete it
            var response = await _oauthClient.DeleteProductAsync(productId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify it's deleted
            var getResponse = await _oauthClient.GetProductAsync(productId);
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Public Endpoint Tests (No OAuth Required)

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetHealth_WithoutToken_Returns200Ok()
        {
            // Arrange & Act
            var response = await _oauthClient!.GetHealthAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("status");
        }

        #endregion

        #region Token Caching Tests

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetToken_CacheTokenBetweenRequests_UsesOneTokenForMultipleRequests()
        {
            // Arrange
            var token1 = await _oauthClient!.GetTokenAsync("read:products");
            var token2 = await _oauthClient!.GetTokenAsync("read:products");

            // Assert - Both calls should return the same cached token
            token1.Should().Be(token2);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task ClearToken_ForcesNewTokenAcquisition_SubsequentRequestGetsNewToken()
        {
            // Arrange
            var token1 = await _oauthClient!.GetTokenAsync("read:products");
            _oauthClient!.ClearToken();
            var token2 = await _oauthClient!.GetTokenAsync("read:products");

            // Assert - After clearing, a new token should be acquired
            token1.Should().NotBe(token2);
        }

        #endregion

        #region Scope-Based Authorization Tests

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task GetProductWithReadScope_Returns200Ok()
        {
            // Arrange
            var token = await _oauthClient!.GetTokenWithSpecificScopesAsync("read:products");
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/products");
            request.Headers.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task CreateProductWithWriteScope_Returns201Created()
        {
            // Arrange
            var token = await _oauthClient!.GetTokenWithSpecificScopesAsync("write:products");
            var productJson = JsonSerializer.Serialize(new
            {
                name = "Product with Write Scope",
                price = 129.99,
                category = "Clothing"
            });
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/products")
            {
                Content = new StringContent(productJson, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        #endregion

        #region Contract Validation Tests

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task TokenResponse_MatchesOpenApiSchema_ContainsAllRequiredFields()
        {
            // Arrange
            var tokenUrl = $"{BaseUrl}/oauth/token";
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", "test-client" },
                { "client_secret", "test-secret" },
                { "scope", "read:products" }
            });

            // Act
            var response = await _httpClient.PostAsync(tokenUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Assert - Validate against OpenAPI schema
            tokenResponse.TryGetProperty("access_token", out var accessToken).Should().BeTrue();
            accessToken.ValueKind.Should().Be(JsonValueKind.String);
            accessToken.GetString().Should().NotBeNullOrEmpty();

            tokenResponse.TryGetProperty("token_type", out var tokenType).Should().BeTrue();
            tokenType.GetString().Should().Be("Bearer");

            tokenResponse.TryGetProperty("expires_in", out var expiresIn).Should().BeTrue();
            expiresIn.ValueKind.Should().Be(JsonValueKind.Number);
            expiresIn.GetInt32().Should().BeGreaterThan(0);
        }

        [Fact]
        [Trait("Category", "OAuth")]
        public async Task ApiResponse_For401Status_MatchesErrorSchema()
        {
            // Arrange & Act - Try without token
            var response = await _oauthClient!.GetAllProductsWithoutTokenAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var content = await response.Content.ReadAsStringAsync();
            // 401 responses may not always have a body, so we just validate status
            // If body exists, it should be valid JSON (error schema)
            if (!string.IsNullOrEmpty(content))
            {
                var error = JsonSerializer.Deserialize<JsonElement>(content);
                error.ValueKind.Should().NotBe(JsonValueKind.Undefined);
            }
        }

        #endregion
    }
}
