import express from 'express';
import cors from 'cors';

const app = express();
const PORT = 8080;

// Middleware
app.use(cors());
app.use(express.json());

// In-memory database
let products = [
  {
    id: 1,
    name: 'Widget',
    description: 'A useful widget',
    price: 19.99,
    inStock: true
  },
  {
    id: 2,
    name: 'Gadget',
    description: 'A handy gadget',
    price: 29.99,
    inStock: true
  }
];

let nextId = 3;

// Middleware to validate request body
const validateCreateRequest = (req, res, next) => {
  const { name, price } = req.body;
  
  if (!name || typeof name !== 'string' || name.trim().length === 0) {
    return res.status(400).json({
      code: 'INVALID_NAME',
      message: 'Product name is required and must be a non-empty string'
    });
  }

  if (price === undefined || typeof price !== 'number' || price < 0) {
    return res.status(400).json({
      code: 'INVALID_PRICE',
      message: 'Product price is required and must be a positive number'
    });
  }

  next();
};

// GET /api/products - Get all products
app.get('/api/products', (req, res) => {
  res.json(products);
});

// POST /api/products - Create new product
app.post('/api/products', validateCreateRequest, (req, res) => {
  const { name, description, price } = req.body;

  const newProduct = {
    id: nextId++,
    name: name.trim(),
    description: description || null,
    price: parseFloat(price),
    inStock: true
  };

  products.push(newProduct);
  res.status(201).json(newProduct);
});

// GET /api/products/:id - Get product by ID
app.get('/api/products/:id', (req, res) => {
  const productId = parseInt(req.params.id, 10);
  const product = products.find(p => p.id === productId);

  if (!product) {
    return res.status(404).json({
      code: 'NOT_FOUND',
      message: `Product with id ${productId} not found`
    });
  }

  res.json(product);
});

// PUT /api/products/:id - Update product
app.put('/api/products/:id', (req, res) => {
  const productId = parseInt(req.params.id, 10);
  const product = products.find(p => p.id === productId);

  if (!product) {
    return res.status(404).json({
      code: 'NOT_FOUND',
      message: `Product with id ${productId} not found`
    });
  }

  // Update fields if provided
  if (req.body.name !== undefined) {
    if (typeof req.body.name !== 'string' || req.body.name.trim().length === 0) {
      return res.status(400).json({
        code: 'INVALID_NAME',
        message: 'Product name must be a non-empty string'
      });
    }
    product.name = req.body.name.trim();
  }

  if (req.body.description !== undefined) {
    product.description = req.body.description;
  }

  if (req.body.price !== undefined) {
    if (typeof req.body.price !== 'number' || req.body.price < 0) {
      return res.status(400).json({
        code: 'INVALID_PRICE',
        message: 'Product price must be a positive number'
      });
    }
    product.price = parseFloat(req.body.price);
  }

  res.json(product);
});

// DELETE /api/products/:id - Delete product
app.delete('/api/products/:id', (req, res) => {
  const productId = parseInt(req.params.id, 10);
  const index = products.findIndex(p => p.id === productId);

  if (index === -1) {
    return res.status(404).json({
      code: 'NOT_FOUND',
      message: `Product with id ${productId} not found`
    });
  }

  products.splice(index, 1);
  res.status(204).send();
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ status: 'ok' });
});

// Error handling middleware
app.use((err, req, res, next) => {
  console.error('Error:', err);
  res.status(500).json({
    code: 'INTERNAL_ERROR',
    message: 'Internal server error'
  });
});

// Start server
app.listen(PORT, () => {
  console.log(`Provider API running at http://localhost:${PORT}`);
  console.log(`OpenAPI Spec available at http://localhost:${PORT}/api-docs`);
});

export default app;
