'use strict';

const UserGroups = require('../../client/UserGroups.js');
const Command = require('../Command.js');
const CommandError = require('../CommandError.js');

class DisableServerCommandCommand extends Command {
    constructor() {
        super({
            name: 'disableservercommand',
            aliases: ['disableguildcommand', 'dgc', 'dsc'],
            group: 'admin',
            description: 'Disables an enabled command in a server.',
            minimumGroup: UserGroups.Moderators,
            examples: ['avatar', 'uinfo'],
            guildOnly: true,
            guarded: true,

            args: [
                {
                    key: 'command',
                    label: 'command',
                    type: 'command',
                    prompt: 'What command would you like to disable?'
                },
                {
                    key: 'guild',
                    label: 'server',
                    type: 'guild-or-current',
                    prompt: 'What server would you like to disable the command in?'
                }
            ]
        });
    }

    async run({ message, client }, { command, guild }) {
        const isDisabled = await client.master.registry.commands.getIsGuildCommandDisabled(guild, command);

        if (isDisabled) {
            throw new CommandError(`Command \`${command.name}\` is already disabled in ${guild.name}.`);
        }

        if (command.command.minimumGroup === UserGroups.Master) {
            throw new CommandError(`Can't disable \`${command.name}\` because it's a Master command.`);
        }

        if (command.command.guarded) {
            throw new CommandError(`Can't disable \`${command.name}\` because it's guarded.`);
        }

        await command.disableIn(guild);
        return client.sendEmbedSuccess(message.channel, `Successfully disabled \`${command.name}\` in ${guild.name}.`);
    }
}

module.exports = DisableServerCommandCommand;