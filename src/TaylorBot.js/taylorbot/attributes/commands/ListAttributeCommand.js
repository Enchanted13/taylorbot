'use strict';

const Command = require('../../commands/Command.js');

class ListAttributeCommand extends Command {
    constructor(attribute) {
        super({
            name: `list${attribute.id}`,
            aliases: attribute.aliases.map(a => `list${a}`),
            group: 'attributes',
            description: `Gets the list of the ${attribute.description} of users in the current server.`,
            examples: [''],
            guildOnly: true,

            args: []
        });
        this.attribute = attribute;
    }

    async run(commandContext) {
        const { guild, channel } = commandContext.message;

        const pageMessage = await this.attribute.listCommand(commandContext, guild);

        return pageMessage.send(channel);
    }
}

module.exports = ListAttributeCommand;