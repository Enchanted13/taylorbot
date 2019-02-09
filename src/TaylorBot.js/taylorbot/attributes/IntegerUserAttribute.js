'use strict';

const SettableUserAttribute = require('./SettableUserAttribute.js');
const SimplePresentor = require('./user-presentors/SimplePresentor.js');

class IntegerUserAttribute extends SettableUserAttribute {
    constructor(options) {
        if (options.presentor === undefined)
            options.presentor = SimplePresentor;
        super(options);
        if (new.target === IntegerUserAttribute) {
            throw new Error(`Can't instantiate abstract ${this.constructor.name} class.`);
        }
    }

    retrieve(database, user) {
        return database.integerAttributes.get(this.id, user);
    }

    set(database, user, value) {
        return database.integerAttributes.set(this.id, user, value.toString());
    }

    clear(database, user) {
        return database.integerAttributes.clear(this.id, user);
    }

    formatValue(attribute) {
        return attribute.integer_value.toString();
    }
}

module.exports = IntegerUserAttribute;