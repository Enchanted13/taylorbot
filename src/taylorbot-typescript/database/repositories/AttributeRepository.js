'use strict';

const Log = require('../../tools/Logger.js');

class AttributeRepository {
    constructor(db, helpers) {
        this._db = db;
        this._helpers = helpers;
        this._columnSet = new this._helpers.ColumnSet(['attribute_id'], {
            table: new this._helpers.TableName('attributes', 'attributes')
        });
    }

    async getAll() {
        try {
            return await this._db.any('SELECT * FROM attributes.attributes;');
        }
        catch (e) {
            Log.error(`Getting all attributes: ${e}`);
            throw e;
        }
    }

    async addAll(databaseAttributes) {
        try {
            return await this._db.any(
                `${this._helpers.insert(databaseAttributes, this._columnSet)} RETURNING *;`,
            );
        }
        catch (e) {
            Log.error(`Adding attributes: ${e}`);
            throw e;
        }
    }
}

module.exports = AttributeRepository;