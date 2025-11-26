const axios = require('axios');

const BASE_URL = 'http://localhost:5001';

/**
 * Consumer API Client
 * SF 17.1 - Uses v1 endpoint
 */
class ConsumerV1ApiClient {
  constructor(baseUrl = BASE_URL) {
    this.client = axios.create({
      baseURL: baseUrl,
      validateStatus: () => true // Don't throw on any status
    });
  }

  /**
   * Get all users - V1 format
   * Contract: GET /users (no apiVersion param)
   */
  async getUsers(limit = 50) {
    const response = await this.client.get('/users', {
      params: { limit }
    });

    return {
      status: response.status,
      data: response.data,
      headers: response.headers
    };
  }
}

module.exports = ConsumerV1ApiClient;
