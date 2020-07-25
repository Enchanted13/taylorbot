import Command = require('../Command');
import { CommandMessageContext } from '../CommandMessageContext';

class SetLastFmCommand extends Command {
    constructor() {
        super({
            name: `setlastfm`,
            aliases: ['setfm'],
            group: 'attributes',
            description: `This command is obsolete and will be removed in a future version. Please use \`lastfm set\` instead.`,
            examples: ['taylor'],

            args: [
                {
                    key: 'lastFmUsername',
                    label: 'username',
                    type: 'text',
                    prompt: `What do you want to set your Last.fm username to?`
                }
            ]
        });
    }

    async run(commandContext: CommandMessageContext, { lastFmUsername }: { lastFmUsername: string }): Promise<void> {
        await commandContext.client.sendEmbedError(
            commandContext.message.channel,
            `This command is obsolete and will be removed in a future version. Please use \`${commandContext.messageContext.prefix}lastfm set ${lastFmUsername}\` instead.`
        );
    }
}

export = SetLastFmCommand;
