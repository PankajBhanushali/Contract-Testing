using Microsoft.AspNetCore.Mvc;

namespace Provider.Controllers;

[ApiController]
[Route("api")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("products")]
    public IActionResult GetAllProducts()
    {
        var products = _repository.GetAll();
        return Ok(products);
    }

    [HttpGet("products/{id}")]
    public IActionResult GetProduct(int id)
    {
        var product = _repository.GetById(id);
        if (product == null)
            return NotFound();
        
        return Ok(product);
    }
}
