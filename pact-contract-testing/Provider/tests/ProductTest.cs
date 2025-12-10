using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using System;
using System.Collections.Generic;
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
                var brokerUrl = Environment.GetEnvironmentVariable("BROKER_URL")
                                ?? Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL");
                var providerName = "ProductService";
                var consumerName = Environment.GetEnvironmentVariable("PACT_CONSUMER_NAME") ?? "ApiClient";

                var pactVerifier = new PactVerifier(providerName, config)
                    .WithHttpEndpoint(new Uri(_pactServiceUri));

                if (!string.IsNullOrWhiteSpace(brokerUrl))
                {
                    var pactUrl = $"{brokerUrl}/pacts/provider/{providerName}/consumer/{consumerName}/latest";
                    var source = pactVerifier.WithUriSource(new Uri(pactUrl));
                    source.Verify();
                    return;
                }
                else
                {
                    var pactUrl = Environment.GetEnvironmentVariable("PACT_URI")
                                   ?? $"http://puvsfpactserver.tiger01-dev.ba.lab.local:9292/pacts/provider/{providerName}/consumer/{consumerName}/latest";
                    var source = pactVerifier.WithUriSource(new Uri(pactUrl));
                    source.Verify();
                    return;
                }
            }
        }
    }
}