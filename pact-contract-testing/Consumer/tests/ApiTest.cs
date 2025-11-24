using System.IO;
using PactNet;
using Xunit.Abstractions;
using Xunit;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Consumer;
using PactNet.Matchers;
using System.Threading.Tasks;

namespace tests
{
    public class ApiTest
    {
        private readonly IPactBuilderV3 pact;
        private readonly List<object> products;

        public ApiTest(ITestOutputHelper output)
        {
            products = new List<object>()
            {
                new { id = 9, type = "CREDIT_CARD", name = "GEM Visa", version = "v2" },
                new { id = 10, type = "CREDIT_CARD", name = "28 Degrees", version = "v1" }
            };

            var config = new PactConfig
            {
                PactDir = Path.Join("..", "..", "..", "..", "..", "pacts"),
                Outputters = new[] { new XUnitOutput(output) },
                LogLevel = PactLogLevel.Debug
            };

            pact = Pact.V3("ApiClient", "ProductService", config).WithHttpInteractions();
        }

        [Fact]
        public async Task GetAllProducts()
        {
            pact.UponReceiving("A valid request for all products")
                .Given("products exist")
                .WithRequest(HttpMethod.Get, "/api/products")
                .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new TypeMatcher(products));

            await pact.VerifyAsync(async ctx => {
                var apiClient = new ApiClient(ctx.MockServerUri);
                var response = await apiClient.GetAllProducts(includeAuth: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetProduct()
        {
            pact.UponReceiving("A valid request for a product")
                .Given("product with ID 10 exists")
                .WithRequest(HttpMethod.Get, "/api/products/10")
                .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new TypeMatcher(products[1]));

            await pact.VerifyAsync(async ctx => {
                var apiClient = new ApiClient(ctx.MockServerUri);
                var response = await apiClient.GetProduct(10, includeAuth: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task NoProductsExist()
        {
            pact.UponReceiving("A valid request for all products")
                .Given("no products exist")
                .WithRequest(HttpMethod.Get, "/api/products")
                .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new TypeMatcher(new List<object>()));

            await pact.VerifyAsync(async ctx => {
                var apiClient = new ApiClient(ctx.MockServerUri);
                var response = await apiClient.GetAllProducts(includeAuth: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task ProductDoesNotExist()
        {
            pact.UponReceiving("A valid request for a product")
                .Given("product with ID 11 does not exist")
                .WithRequest(HttpMethod.Get, "/api/products/11")
                .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
            .WillRespond()
                .WithStatus(HttpStatusCode.NotFound);

            await pact.VerifyAsync(async ctx => {
                var apiClient = new ApiClient(ctx.MockServerUri);
                var response = await apiClient.GetProduct(11, includeAuth: true);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetProductMissingAuthHeader()
        {
            pact.UponReceiving("A valid request for a product")
                .Given("No auth token is provided")
                .WithRequest(HttpMethod.Get, "/api/products/10")
            .WillRespond()
                .WithStatus(HttpStatusCode.Unauthorized);

            await pact.VerifyAsync(async ctx => {
                var apiClient = new ApiClient(ctx.MockServerUri);
                var response = await apiClient.GetProduct(10); // no auth header
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            });
        }
    }
}