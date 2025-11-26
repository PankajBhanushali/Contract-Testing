const axios = require('axios');

const BASE_URL = 'http://localhost:5001';

/**
 * Consumer API Client
 * SF 18.1 - Uses v2 endpoint
 */
class ConsumerV2ApiClient {
  constructor(baseUrl = BASE_URL) {
    this.client = axios.create({
      baseURL: baseUrl,
      validateStatus: () => true
    });
  }

  /**
   * Get all users - V2 format
   * Contract: GET /users?apiVersion=2
   */
  async getUsers(limit = 50) {
    const response = await this.client.get('/users', {
      params: { 
        apiVersion: '2',
        limit 
      }
    });

    return {
      status: response.status,
      data: response.data,
      headers: response.headers
    };
  }
}

module.exports = ConsumerV2ApiClient;
