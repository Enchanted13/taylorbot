'use strict';

const RoleArgumentType = require('./Role.js');

class RoleOrOwnedArgumentType extends RoleArgumentType {
    get id() {
        return 'role-or-owned';
    }

    canBeEmpty({ message }) {
        return message.member ? true : false;
    }

    default({ message }) {
        return message.member.roles.random();
    }
}

module.exports = RoleOrOwnedArgumentType;