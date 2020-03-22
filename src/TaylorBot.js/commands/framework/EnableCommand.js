'use strict';

const UserGroups = require('../../client/UserGroups.js');
const Command = require('../Command.js');
const CommandError = require('../CommandError.js');

class EnableCommandCommand extends Command {
    constructor() {
        super({
            name: 'enablecommand',
            aliases: ['ec'],
            group: 'framework',
            description: 'Enables a disabled command globally.',
            minimumGroup: UserGroups.Master,
            examples: ['avatar', 'uinfo'],
            guarded: true,

            args: [
                {
                    key: 'command',
                    label: 'command',
                    type: 'command',
                    prompt: 'What command would you like to enable?'
                }
            ]
        });
    }

    async run({ message, client }, { command }) {
        const isDisabled = await client.master.registry.commands.insertOrGetIsCommandDisabled(command);

        if (!isDisabled) {
            throw new CommandError(`Command '${command.name}' is already enabled.`);
        }

        await command.enableCommand();
        return client.sendEmbedSuccess(message.channel, `Successfully enabled '${command.name}' globally.`);
    }
}

module.exports = EnableCommandCommand;