import { Command } from '../Command';
import { DiscordEmbedFormatter } from '../../modules/discord/DiscordEmbedFormatter';
import { Log } from '../../tools/Logger';
import { StringUtil } from '../../modules/util/StringUtil';
import { TimeUtil } from '../../modules/util/TimeUtil';
import { RandomModule } from '../../modules/random/RandomModule';
import { UnsafeRandomModule } from '../../modules/random/UnsafeRandomModule';
import { Format } from '../../modules/discord/DiscordFormatter';
import { BankRepository } from '../../modules/heist/BankRepository';
import { FailureReasonRepository } from '../../modules/heist/FailureReasonRepository';
import { CommandMessageContext } from '../CommandMessageContext';
import { TaypointAmount } from '../../modules/points/TaypointAmount';

const HEIST_DELAY_MINUTES = 2;

class HeistCommand extends Command {
    constructor() {
        super({
            name: 'heist',
            group: 'Points 💰',
            description: 'Start or join a taypoints bank heist in the current channel! The more points you invest into the heist, the more you get if the heist succeeds!',
            examples: ['13', 'all'],
            maxDailyUseCount: 150,
            guildOnly: true,

            args: [
                {
                    key: 'amount',
                    label: 'taypoints',
                    type: 'taypoint-amount',
                    prompt: 'How much taypoints do you want to invest into the heist?'
                }
            ]
        });
    }

    async run(commandContext: CommandMessageContext, { amount }: { amount: TaypointAmount }): Promise<void> {
        const { message: { channel, guild }, client, author } = commandContext;
        const { heists } = client.master.registry;

        const { created, updated } = await heists.enterHeist(author, guild!, amount, HEIST_DELAY_MINUTES);

        const embed = DiscordEmbedFormatter.baseUserSuccessEmbed(author);

        if (created) {
            embed.setDescription([
                `Heist started by ${author}! The more people, the higher the rewards! 🤑`,
                `To join, use \`${commandContext.usage()}\` to invest points into the heist! 🕵️‍`,
                `The heist begins in ${StringUtil.plural(HEIST_DELAY_MINUTES, 'minute', '**')}. ⏰`,
            ].join('\n'));

            TimeUtil.waitMinutes(HEIST_DELAY_MINUTES).then(async () => {
                try {
                    const heisters = await heists.completeHeist(guild!);
                    const bank = BankRepository.retrieveBank(heisters.length)!;

                    const roll = await RandomModule.getRandIntInclusive(1, 100);
                    const won = roll >= bank.minimumRollForSuccess;

                    const results = await (won ?
                        client.master.database.heistStats.winHeist(heisters, bank.payoutMultiplier) :
                        client.master.database.heistStats.loseHeist(heisters)
                    );

                    const randomHeister = UnsafeRandomModule.randomInArray((results as Record<string, any>[]));

                    await client.sendEmbed(channel, DiscordEmbedFormatter
                        .baseUserHeader(author)
                        .setColor(won ? '#43b581' : '#f04747')
                        .setTitle(won ? 'The heist was a success!' : 'The heist was a failure!')
                        .setDescription(StringUtil.shrinkString([
                            `The **${results.length}** person crew heads to the **${bank.bankName}**.`,
                            won ?
                                `All thanks to <@${randomHeister.user_id}>, the heist went perfectly. 💯\n` :
                                `The cops busted the crew because ${FailureReasonRepository.retrieveRandomReason().replace('{user}', `<@${randomHeister.user_id}>`)}. 👮\n`,
                            ...(results as Record<string, any>[]).map(({ user_id, gambled_count, final_count, payout_count }) =>
                                `<@${user_id}> Invested ${StringUtil.plural(
                                    gambled_count, 'taypoint', '**'
                                )}${won ? `, made a profit of **${StringUtil.formatNumberString(payout_count)}**` : ``
                                }, now has ${StringUtil.formatNumberString(final_count)}. ${won ? '💰' : '💸'}`
                            )
                        ].join('\n'), 2000, '...'))
                    );
                }
                catch (e) {
                    Log.error(`Resolving heist ${Format.channel(channel)}: ${e}`);
                }
            });
        }
        else {
            embed.setDescription([
                `${author}${updated ? `'s investment for the heist has been updated` : ` joined the heist`}! 🕵️‍`,
                'Get more people to join and rob a bigger bank! 💵'
            ].join('\n'));
        }

        await client.sendEmbed(channel, embed);
    }
}

export = HeistCommand;
