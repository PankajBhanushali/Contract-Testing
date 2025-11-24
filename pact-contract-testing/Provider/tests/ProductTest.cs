using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace tests
{
    public class ProductTest
    {
        private string _pactServiceUri = "http://127.0.0.1:9001";
        private readonly ITestOutputHelper _output;

        public ProductTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EnsureProviderApiHonoursPactWithConsumer()
        {
            // Arrange
            var config = new PactVerifierConfig
            {
                Outputters = new List<IOutput>
                {
                    new XunitOutput(_output)
                }
            };

            using (var _webHost = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>().UseUrls(_pactServiceUri).Build())
            {
                _webHost.Start();

                //Act / Assert
                IPactVerifier pactVerifier = new PactVerifier("ProductService", config);
                pactVerifier.WithHttpEndpoint(new Uri(_pactServiceUri))
                .WithUriSource(new Uri("http://localhost:9292/pacts/provider/ProductService/consumer/ApiClient/latest"))
                .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
                .Verify();
            }
        }
    }
}