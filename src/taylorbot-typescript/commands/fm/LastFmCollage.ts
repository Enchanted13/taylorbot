import { Command } from '../Command';
import { CommandMessageContext } from '../CommandMessageContext';

class LastFmCollageCommand extends Command {
    constructor() {
        super({
            name: 'lastfmcollage',
            aliases: ['fmcollage', 'fmc'],
            group: 'fm',
            description: 'This command is obsolete and will be removed in a future version. Please use **/lastfm collage** instead.',
            examples: [''],

            args: [
                {
                    key: 'args',
                    label: 'args',
                    type: 'any-text',
                    prompt: 'What arguments would you like to use?'
                }
            ]
        });
    }

    async run({ message, client, messageContext }: CommandMessageContext, { args }: { args: string }): Promise<void> {
        await client.sendEmbedError(
            message.channel,
            `This command is obsolete and will be removed in a future version. Please use **/lastfm collage** instead.`
        );
    }
}

export = LastFmCollageCommand;
