using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Consumer.OAuth
{
    /// <summary>
    /// OAuth-enabled Product API client with automatic token acquisition and refresh.
    /// Handles JWT Bearer token management and injects tokens into all API requests.
    /// </summary>
    public class ProductApiOAuthClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenUrl;

        // Token cache
        private string? _cachedToken;
        private DateTime _tokenExpiryTime;
        private readonly object _tokenLock = new object();

        // Default scopes for this client
        private readonly string[] _defaultScopes = { "read:products", "write:products" };

        public ProductApiOAuthClient(
            HttpClient httpClient,
            string baseUrl = "http://localhost:5000",
            string tokenUrl = "http://localhost:5000/oauth/token",
            string clientId = "test-client",
            string clientSecret = "test-secret")
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
            _tokenUrl = tokenUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tokenExpiryTime = DateTime.MinValue;
        }

        /// <summary>
        /// Gets a valid OAuth token, using cached token if not expired, otherwise acquires a new one.
        /// </summary>
        public async Task<string> GetTokenAsync(params string[] scopes)
        {
            lock (_tokenLock)
            {
                // Return cached token if still valid (with 30-second buffer)
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddSeconds(30) < _tokenExpiryTime)
                {
                    return _cachedToken;
                }
            }

            // Token expired or doesn't exist - acquire a new one
            return await AcquireNewTokenAsync(scopes.Length > 0 ? scopes : _defaultScopes);
        }

        /// <summary>
        /// Acquires a new token from the OAuth token endpoint.
        /// </summary>
        private async Task<string> AcquireNewTokenAsync(string[] scopes)
        {
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "scope", string.Join(" ", scopes) }
            });

            try
            {
                var response = await _httpClient.PostAsync(_tokenUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Failed to acquire token. Status: {response.StatusCode}, Response: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using (var jsonDoc = JsonDocument.Parse(responseContent))
                {
                    var root = jsonDoc.RootElement;

                    if (!root.TryGetProperty("access_token", out var tokenElement))
                    {
                        throw new InvalidOperationException("Token response missing 'access_token' field");
                    }

                    var token = tokenElement.GetString();
                    var expiresIn = 3600; // Default to 1 hour

                    if (root.TryGetProperty("expires_in", out var expiresElement) && expiresElement.ValueKind == JsonValueKind.Number)
                    {
                        expiresIn = expiresElement.GetInt32();
                    }

                    lock (_tokenLock)
                    {
                        _cachedToken = token;
                        _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresIn);
                    }

                    return token ?? throw new InvalidOperationException("Token is null");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"OAuth token acquisition failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears the cached token, forcing a new token to be acquired on next request.
        /// </summary>
        public void ClearToken()
        {
            lock (_tokenLock)
            {
                _cachedToken = null;
                _tokenExpiryTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Makes an HTTP request with OAuth Bearer token automatically injected.
        /// </summary>
        private async Task<HttpResponseMessage> MakeAuthorizedRequestAsync(
            HttpMethod method,
            string endpoint,
            HttpContent? content = null,
            params string[] scopes)
        {
            var token = await GetTokenAsync(scopes.Length > 0 ? scopes : _defaultScopes);
            var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (content != null)
            {
                request.Content = content;
            }

            return await _httpClient.SendAsync(request);
        }

        #region GET Operations

        public async Task<HttpResponseMessage> GetAllProductsAsync()
        {
            return await MakeAuthorizedRequestAsync(HttpMethod.Get, "/api/products", scopes: "read:products");
        }

        public async Task<HttpResponseMessage> GetProductAsync(int id)
        {
            return await MakeAuthorizedRequestAsync(HttpMethod.Get, $"/api/products/{id}", scopes: "read:products");
        }

        public async Task<HttpResponseMessage> GetHealthAsync()
        {
            // Health endpoint doesn't require OAuth
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
            return response;
        }

        #endregion

        #region POST Operations

        public async Task<HttpResponseMessage> CreateProductAsync(string jsonContent)
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            return await MakeAuthorizedRequestAsync(HttpMethod.Post, "/api/products", content, scopes: "write:products");
        }

        #endregion

        #region PUT Operations

        public async Task<HttpResponseMessage> UpdateProductAsync(int id, string jsonContent)
        {
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            return await MakeAuthorizedRequestAsync(HttpMethod.Put, $"/api/products/{id}", content, scopes: "write:products");
        }

        #endregion

        #region DELETE Operations

        public async Task<HttpResponseMessage> DeleteProductAsync(int id)
        {
            return await MakeAuthorizedRequestAsync(HttpMethod.Delete, $"/api/products/{id}", scopes: "write:products");
        }

        #endregion

        #region Test Helper Methods

        /// <summary>
        /// Makes a request WITHOUT adding OAuth token (for testing unauthorized scenarios).
        /// </summary>
        public async Task<HttpResponseMessage> GetAllProductsWithoutTokenAsync()
        {
            return await _httpClient.GetAsync($"{_baseUrl}/api/products");
        }

        /// <summary>
        /// Makes a request with a specific authorization header (for testing invalid tokens).
        /// </summary>
        public async Task<HttpResponseMessage> GetAllProductsWithCustomAuthAsync(string authHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/products");
            request.Headers.Add("Authorization", authHeader);
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Gets a token with specific scopes for testing scope restrictions.
        /// </summary>
        public async Task<string> GetTokenWithSpecificScopesAsync(params string[] scopes)
        {
            return await AcquireNewTokenAsync(scopes.Length > 0 ? scopes : _defaultScopes);
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
