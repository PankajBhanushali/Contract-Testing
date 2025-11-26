/**
 * JSON Schema Validator
 * Simple schema validation utility
 */
class SchemaValidator {
  /**
   * Validate data against schema
   * @param {Object} data - Data to validate
   * @param {Object} schema - JSON schema
   * @returns {Object} - { valid: boolean, errors: string[] }
   */
  static validate(data, schema) {
    const errors = [];

    if (!data || !schema) {
      return { valid: false, errors: ['Data or schema is missing'] };
    }

    // Check required fields
    if (schema.required) {
      schema.required.forEach(field => {
        if (!(field in data)) {
          errors.push(`Required field missing: ${field}`);
        }
      });
    }

    // Check type of each property
    if (schema.properties) {
      Object.keys(schema.properties).forEach(key => {
        if (key in data) {
          const propSchema = schema.properties[key];
          const value = data[key];

          if (propSchema.type) {
            const actualType = Array.isArray(value) ? 'array' : typeof value;
            if (actualType !== propSchema.type) {
              errors.push(`Field '${key}': expected ${propSchema.type}, got ${actualType}`);
            }
          }

          // Enum validation
          if (propSchema.enum && !propSchema.enum.includes(value)) {
            errors.push(`Field '${key}': value must be one of ${propSchema.enum.join(', ')}`);
          }

          // Format validation (email)
          if (propSchema.format === 'email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
              errors.push(`Field '${key}': invalid email format`);
            }
          }
        }
      });
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Validate array items against schema
   */
  static validateArray(arr, itemSchema) {
    const errors = [];

    if (!Array.isArray(arr)) {
      return { valid: false, errors: ['Expected array'] };
    }

    arr.forEach((item, index) => {
      const result = this.validate(item, itemSchema);
      if (!result.valid) {
        errors.push(`Item [${index}]: ${result.errors.join(', ')}`);
      }
    });

    return {
      valid: errors.length === 0,
      errors
    };
  }
}

module.exports = SchemaValidator;
