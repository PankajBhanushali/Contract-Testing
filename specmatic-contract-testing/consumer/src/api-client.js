/**
 * Product API Client
 * Consumes the Product Service API according to the OpenAPI spec
 */

export class ProductApiClient {
  constructor(baseUrl = 'http://localhost:8080') {
    this.baseUrl = baseUrl;
  }

  async getAllProducts() {
    const response = await fetch(`${this.baseUrl}/api/products`);
    if (!response.ok) {
      throw new Error(`Failed to get products: ${response.statusText}`);
    }
    return response.json();
  }

  async getProductById(id) {
    const response = await fetch(`${this.baseUrl}/api/products/${id}`);
    if (response.status === 404) {
      throw new Error(`Product with id ${id} not found`);
    }
    if (!response.ok) {
      throw new Error(`Failed to get product: ${response.statusText}`);
    }
    return response.json();
  }

  async createProduct(product) {
    const response = await fetch(`${this.baseUrl}/api/products`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(product)
    });

    if (response.status === 400) {
      const error = await response.json();
      throw new Error(`Invalid product: ${error.message}`);
    }

    if (!response.ok) {
      throw new Error(`Failed to create product: ${response.statusText}`);
    }

    return response.json();
  }

  async updateProduct(id, updates) {
    const response = await fetch(`${this.baseUrl}/api/products/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(updates)
    });

    if (response.status === 404) {
      throw new Error(`Product with id ${id} not found`);
    }

    if (response.status === 400) {
      const error = await response.json();
      throw new Error(`Invalid update: ${error.message}`);
    }

    if (!response.ok) {
      throw new Error(`Failed to update product: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteProduct(id) {
    const response = await fetch(`${this.baseUrl}/api/products/${id}`, {
      method: 'DELETE'
    });

    if (response.status === 404) {
      throw new Error(`Product with id ${id} not found`);
    }

    if (!response.ok) {
      throw new Error(`Failed to delete product: ${response.statusText}`);
    }
  }
}

export default ProductApiClient;
