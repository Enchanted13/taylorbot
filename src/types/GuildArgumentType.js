'use strict';

const { GlobalPaths } = require('globalobjects');

const ArgumentType = require(GlobalPaths.ArgumentType);

class GuildArgumentType extends ArgumentType {
    constructor() {
        super('guild');
    }

    async parse(val, msg) {
        const matches = val.match(/^([0-9]+)$/);
        if (matches) {
            const guild = msg.client.guilds.resolve(matches[1]);
            if (guild) {
                const member = await guild.members.fetch(msg.author);
                if (member) {
                    return guild;
                }
            }
        }

        const search = val.toLowerCase();
        const guilds = msg.client.guilds.filterArray(GuildArgumentType.guildFilterInexact(search));
        if (guilds.length === 0)
            return null;
        if (guilds.length === 1) {
            const guild = guilds[0];
            const member = await guild.members.fetch(msg.author);
            return member ? guild : null;
        }

        const exactGuilds = guilds.filter(GuildArgumentType.guildFilterExact(search));
        if (exactGuilds.length > 0) {
            for (const guild of exactGuilds) {
                const member = await guild.members.fetch(msg.author);
                if (member)
                    return guild;
            }
        }

        return null;
    }

    static guildFilterExact(search) {
        return guild => guild.name.toLowerCase() === search;
    }

    static guildFilterInexact(search) {
        return guild => guild.name.toLowerCase().includes(search);
    }
}

module.exports = GuildArgumentType;
