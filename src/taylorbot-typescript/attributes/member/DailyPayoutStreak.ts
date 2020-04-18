import { MemberAttribute } from '../MemberAttribute';
import { DatabaseDriver } from '../../database/DatabaseDriver';
import { GuildMember, Guild } from 'discord.js';
import { DailyStreakPresenter } from '../member-presenters/DailyStreakPresenter';

class DailyPayoutStreakMemberAttribute extends MemberAttribute {
    constructor() {
        super({
            id: 'dailypayoutstreak',
            aliases: ['dailystreak', 'dstreak'],
            description: 'daily payout streak',
            columnName: 'streak_count',
            presenter: DailyStreakPresenter
        });
    }

    retrieve(database: DatabaseDriver, member: GuildMember): Promise<any> {
        return database.guildMembers.getRankedForeignStatFor(member, 'users', 'daily_payouts', this.columnName);
    }

    rank(database: DatabaseDriver, guild: Guild, entries: number): Promise<any[]> {
        return database.guildMembers.getRankedForeignStat(guild, entries, 'users', 'daily_payouts', this.columnName);
    }
}

export = DailyPayoutStreakMemberAttribute;