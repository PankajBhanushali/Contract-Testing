const express = require('express');
const app = express();
const PORT = 5001;

// Mock database
const users = [
  { id: 1, name: 'John Doe', email: 'john@company.com', role: 'admin' },
  { id: 2, name: 'Jane Smith', email: 'jane@company.com', role: 'user' },
  { id: 3, name: 'Bob Johnson', email: 'bob@company.com', role: 'guest' }
];

app.use(express.json());

// Middleware for OpenAPI validation
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.path}${req.url.includes('?') ? '?' + req.url.split('?')[1] : ''}`);
  next();
});

/**
 * GET /users
 * Returns users in v1 or v2 format based on apiVersion query parameter
 */
app.get('/users', (req, res) => {
  const apiVersion = req.query.apiVersion || '1';
  const limit = Math.min(parseInt(req.query.limit) || 50, 100);

  // Validate apiVersion
  if (!['1', '2'].includes(apiVersion)) {
    return res.status(400).json({
      code: 'INVALID_PARAMETER',
      message: 'Invalid apiVersion parameter. Use "1" or "2"'
    });
  }

  // Get limited users
  const limitedUsers = users.slice(0, limit);

  if (apiVersion === '2') {
    // V2 response: Include all fields
    return res.json({
      users: limitedUsers.map(user => ({
        id: user.id,
        name: user.name,
        email: user.email,
        role: user.role
      }))
    });
  }

  // V1 response: Only id and name
  res.json({
    users: limitedUsers.map(user => ({
      id: user.id,
      name: user.name
    }))
  });
});

/**
 * Health check endpoint
 */
app.get('/health', (req, res) => {
  res.json({ status: 'healthy' });
});

/**
 * Start server
 */
app.listen(PORT, '0.0.0.0', () => {
  console.log(`✓ Provider API running on http://localhost:${PORT}`);
  console.log(`✓ API Documentation: http://localhost:${PORT}/api-docs`);
  console.log(`✓ Ready to accept requests!`);
}).on('error', (err) => {
  console.error('Server error:', err);
  process.exit(1);
});
