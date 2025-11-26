const ConsumerV2ApiClient = require('../v2-client');
const SchemaValidator = require('../schema-validator');

describe('SF 18.1 Consumer - V2 Contracts', () => {
  let client;

  beforeAll(() => {
    client = new ConsumerV2ApiClient();
  });

  describe('GET /users?apiVersion=2 (v2 format)', () => {
    it('should return 200 status code', async () => {
      const response = await client.getUsers();
      expect(response.status).toBe(200);
    });

    it('should return users array', async () => {
      const response = await client.getUsers();
      expect(response.data).toHaveProperty('users');
      expect(Array.isArray(response.data.users)).toBe(true);
    });

    it('should return v2 schema: id, name, email, role', async () => {
      const response = await client.getUsers();
      const { users } = response.data;

      expect(users.length).toBeGreaterThan(0);

      const userV2Schema = {
        type: 'object',
        required: ['id', 'name', 'email', 'role'],
        properties: {
          id: { type: 'number' },
          name: { type: 'string' },
          email: { type: 'string', format: 'email' },
          role: { 
            type: 'string',
            enum: ['admin', 'user', 'guest']
          }
        }
      };

      const validation = SchemaValidator.validateArray(users, userV2Schema);
      expect(validation.valid).toBe(true);
      expect(validation.errors).toEqual([]);
    });

    it('should include all required v2 fields', async () => {
      const response = await client.getUsers();
      const { users } = response.data;

      users.forEach(user => {
        expect(user).toHaveProperty('id');
        expect(user).toHaveProperty('name');
        expect(user).toHaveProperty('email');
        expect(user).toHaveProperty('role');
      });
    });

    it('should have valid email format in v2 response', async () => {
      const response = await client.getUsers();
      const { users } = response.data;

      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

      users.forEach(user => {
        expect(user.email).toMatch(emailRegex);
      });
    });

    it('should have valid role values in v2 response', async () => {
      const response = await client.getUsers();
      const { users } = response.data;

      const validRoles = ['admin', 'user', 'guest'];

      users.forEach(user => {
        expect(validRoles).toContain(user.role);
      });
    });

    it('should respect limit parameter in v2', async () => {
      const response = await client.getUsers(2);
      const { users } = response.data;

      expect(users.length).toBeLessThanOrEqual(2);
    });

    it('should have valid response headers', async () => {
      const response = await client.getUsers();
      expect(response.headers['content-type']).toContain('application/json');
    });
  });

  describe('Backward Compatibility Check', () => {
    it('v2 response should include v1 fields (backward compatible)', async () => {
      const response = await client.getUsers();
      const { users } = response.data;

      users.forEach(user => {
        // V1 fields must be present
        expect(user).toHaveProperty('id');
        expect(user).toHaveProperty('name');
      });
    });
  });
});
