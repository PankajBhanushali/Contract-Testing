using System;
using System.Threading.Tasks;

namespace Consumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new ApiClient(new Uri("http://localhost:5001"));
            
            Console.WriteLine("**Retrieving product list**");
            var allProductsResponse = await client.GetAllProducts();
            Console.WriteLine($"Response.Code={allProductsResponse.StatusCode}, Response.Body={await allProductsResponse.Content.ReadAsStringAsync()}");
            Console.WriteLine();

            Console.WriteLine("**Retrieving product with id=10");
            var productResponse = await client.GetProduct(10);
            Console.WriteLine($"Response.Code={productResponse.StatusCode}, Response.Body={await productResponse.Content.ReadAsStringAsync()}");
        }
    }
}
