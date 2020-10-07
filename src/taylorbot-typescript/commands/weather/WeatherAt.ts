import { Command } from '../Command';
import { DarkSkyModule } from '../../modules/darksky/DarkSkyModule';
import { DarkSkyEmbedModule } from '../../modules/darksky/DarkSkyEmbedModule';
import { CommandMessageContext } from '../CommandMessageContext';

class WeatherCommand extends Command {
    constructor() {
        super({
            name: 'weatherat',
            group: 'Weather 🌦',
            description: `Get current weather forecast for a location. Weather data is [Powered by Dark Sky](https://darksky.net/poweredby/).`,
            examples: ['Nashville, USA'],
            maxDailyUseCount: 10,

            args: [
                {
                    key: 'place',
                    label: 'location',
                    type: 'google-place',
                    prompt: 'What location would you like to see the weather for?'
                }
            ]
        });
    }

    async run({ message: { channel }, client, author }: CommandMessageContext, { place }: { place: any }): Promise<void> {
        const { geometry: { location: { lat, lng } }, formatted_address } = place;
        const { currently } = await DarkSkyModule.getCurrentForecast(lat, lng);

        await client.sendEmbed(channel,
            DarkSkyEmbedModule.dataPointToEmbed(currently, author, formatted_address)
        );
    }
}

export = WeatherCommand;