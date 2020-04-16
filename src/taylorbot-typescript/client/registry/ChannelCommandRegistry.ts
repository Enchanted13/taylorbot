import { DatabaseDriver } from '../../database/DatabaseDriver';
import RedisDriver = require('../../caching/RedisDriver.js');
import { TextChannel } from 'discord.js';
import { DatabaseCommand } from '../../database/repositories/CommandRepository';
import { CachedCommand } from './CachedCommand';

export class ChannelCommandRegistry {
    readonly #database: DatabaseDriver;
    readonly #redis: RedisDriver;

    constructor(database: DatabaseDriver, redis: RedisDriver) {
        this.#database = database;
        this.#redis = redis;
    }

    key(guildId: string, channelId: string): string {
        return `enabled-commands:guild:${guildId}:channel:${channelId}`;
    }

    async isCommandDisabledInChannel(guildTextChannel: TextChannel, command: CachedCommand): Promise<boolean> {
        const key = this.key(guildTextChannel.guild.id, guildTextChannel.id);
        const isEnabled = await this.#redis.hashGet(key, command.name);

        if (isEnabled === null) {
            const { exists } = await this.#database.channelCommands.getIsCommandDisabledInChannel(guildTextChannel, command);
            await this.#redis.hashSet(key, command.name, (!exists) ? 1 : 0);
            await this.#redis.expire(key, 6 * 60 * 60);
            return exists;
        }

        return isEnabled === '0';
    }

    async disableCommandInChannel(guildTextChannel: TextChannel, command: DatabaseCommand): Promise<void> {
        await this.#database.channelCommands.disableCommandInChannel(guildTextChannel, command);
        await this.#redis.hashSet(
            this.key(guildTextChannel.guild.id, guildTextChannel.id),
            command.name,
            0
        );
    }

    async enableCommandInChannel(guildTextChannel: TextChannel, command: DatabaseCommand): Promise<void> {
        await this.#database.channelCommands.enableCommandInChannel(guildTextChannel, command);
        await this.#redis.hashSet(
            this.key(guildTextChannel.guild.id, guildTextChannel.id),
            command.name,
            1
        );
    }
}
