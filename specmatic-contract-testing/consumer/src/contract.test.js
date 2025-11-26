/**
 * Specmatic Contract Tests - Consumer Side
 * These tests validate that the consumer correctly implements the contract
 */

import { describe, it, expect, beforeAll, afterAll } from '@jest/globals';
import { ProductApiClient } from '../src/api-client.js';

describe('Product API Consumer Contract Tests', () => {
  let client;

  beforeAll(() => {
    client = new ProductApiClient('http://localhost:8080');
  });

  describe('GET /api/products', () => {
    it('should return an array of products', async () => {
      const products = await client.getAllProducts();
      expect(Array.isArray(products)).toBe(true);
    });

    it('should return products with required fields', async () => {
      const products = await client.getAllProducts();
      expect(products.length).toBeGreaterThan(0);

      products.forEach(product => {
        expect(product).toHaveProperty('id');
        expect(product).toHaveProperty('name');
        expect(product).toHaveProperty('price');
        expect(product).toHaveProperty('inStock');
      });
    });

    it('should return products with correct data types', async () => {
      const products = await client.getAllProducts();
      expect(products.length).toBeGreaterThan(0);

      const product = products[0];
      expect(typeof product.id).toBe('number');
      expect(typeof product.name).toBe('string');
      expect(typeof product.price).toBe('number');
      expect(typeof product.inStock).toBe('boolean');
    });

    it('should return products with valid price values', async () => {
      const products = await client.getAllProducts();
      expect(products.length).toBeGreaterThan(0);

      products.forEach(product => {
        expect(product.price).toBeGreaterThanOrEqual(0);
      });
    });
  });

  describe('GET /api/products/{id}', () => {
    it('should return a single product when given valid ID', async () => {
      const product = await client.getProductById(1);
      expect(product).toHaveProperty('id', 1);
      expect(product).toHaveProperty('name');
      expect(product).toHaveProperty('price');
    });

    it('should return product with correct schema', async () => {
      const product = await client.getProductById(1);
      expect(typeof product.id).toBe('number');
      expect(typeof product.name).toBe('string');
      expect(typeof product.price).toBe('number');
      expect(typeof product.inStock).toBe('boolean');
    });

    it('should throw error when product not found', async () => {
      await expect(client.getProductById(99999)).rejects.toThrow('not found');
    });
  });

  describe('POST /api/products', () => {
    it('should create a new product with valid data', async () => {
      const newProduct = {
        name: 'Test Product',
        description: 'Test description',
        price: 29.99
      };

      const created = await client.createProduct(newProduct);
      expect(created).toHaveProperty('id');
      expect(created.name).toBe('Test Product');
      expect(created.price).toBe(29.99);
      expect(created.inStock).toBe(true);
    });

    it('should return product with all required fields', async () => {
      const newProduct = {
        name: 'Another Product',
        price: 39.99
      };

      const created = await client.createProduct(newProduct);
      expect(created).toHaveProperty('id');
      expect(created).toHaveProperty('name');
      expect(created).toHaveProperty('price');
      expect(created).toHaveProperty('inStock');
    });

    it('should reject product without name', async () => {
      const invalidProduct = {
        price: 19.99
      };

      await expect(client.createProduct(invalidProduct))
        .rejects.toThrow();
    });

    it('should reject product with invalid price', async () => {
      const invalidProduct = {
        name: 'Test',
        price: -10
      };

      await expect(client.createProduct(invalidProduct))
        .rejects.toThrow();
    });
  });

  describe('PUT /api/products/{id}', () => {
    let testProductId;

    beforeAll(async () => {
      const created = await client.createProduct({
        name: 'Product to Update',
        price: 25.00
      });
      testProductId = created.id;
    });

    it('should update product with valid data', async () => {
      const updates = {
        name: 'Updated Product',
        price: 35.00
      };

      const updated = await client.updateProduct(testProductId, updates);
      expect(updated.name).toBe('Updated Product');
      expect(updated.price).toBe(35.00);
    });

    it('should return updated product with all fields', async () => {
      const updates = {
        description: 'New description'
      };

      const updated = await client.updateProduct(testProductId, updates);
      expect(updated).toHaveProperty('id');
      expect(updated).toHaveProperty('name');
      expect(updated).toHaveProperty('price');
      expect(updated).toHaveProperty('inStock');
    });

    it('should throw error when updating non-existent product', async () => {
      await expect(client.updateProduct(99999, { name: 'Test' }))
        .rejects.toThrow('not found');
    });
  });

  describe('DELETE /api/products/{id}', () => {
    it('should delete an existing product', async () => {
      // Create a product first
      const created = await client.createProduct({
        name: 'Product to Delete',
        price: 15.00
      });

      // Delete it
      await client.deleteProduct(created.id);

      // Verify it's gone
      await expect(client.getProductById(created.id))
        .rejects.toThrow('not found');
    });

    it('should throw error when deleting non-existent product', async () => {
      await expect(client.deleteProduct(99999))
        .rejects.toThrow('not found');
    });
  });
});
