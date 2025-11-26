const axios = require('axios');

async function test() {
  try {
    console.log('Testing connection to http://localhost:5001/users');
    const response = await axios.get('http://localhost:5001/users', {
      timeout: 5000
    });
    console.log('✓ Connection successful!');
    console.log('Status:', response.status);
    console.log('Response:', JSON.stringify(response.data, null, 2));
  } catch (error) {
    console.error('✗ Connection failed:', error.message);
    if (error.code === 'ECONNREFUSED') {
      console.error('  Provider is not running on port 5001');
    }
  }
}

test();
