import { CachedCommand } from './CachedCommand';
import { CommandLoader } from '../../commands/CommandLoader';
import { AttributeLoader } from '../../attributes/AttributeLoader';
import { DatabaseDriver } from '../../database/DatabaseDriver';
import { RedisDriver } from '../../caching/RedisDriver';
import { Command } from '../../commands/Command';
import { Guild } from 'discord.js';

export class CommandRegistry {
    database: DatabaseDriver;
    redis: RedisDriver;
    #commandsCache = new Map<string, CachedCommand | string>();
    useCountCache = new Map<string, { count: number; errorCount: number }>();

    constructor(database: DatabaseDriver, redis: RedisDriver) {
        this.database = database;
        this.redis = redis;
    }

    async loadAll(): Promise<void> {
        const commands = [
            ...(await CommandLoader.loadAll()),
            ...(await AttributeLoader.loadUserAttributeCommands())
        ];

        commands.forEach(c => this.cacheCommand(c));
    }

    cacheCommand(command: Command): void {
        const key = command.name.toLowerCase();

        if (this.#commandsCache.has(key))
            throw new Error(`Command '${command.name}' is already cached.`);

        const cached = new CachedCommand(
            command.name,
            command
        );

        this.#commandsCache.set(key, cached);

        for (const alias of command.aliases) {
            const aliasKey = alias.toLowerCase();

            if (this.#commandsCache.has(aliasKey))
                throw new Error(`Command Key '${aliasKey}' is already cached when setting alias.`);

            this.#commandsCache.set(aliasKey, key);
        }
    }

    getCommand(name: string): CachedCommand {
        const cachedCommand = this.#commandsCache.get(name);

        if (!cachedCommand)
            throw new Error(`Command '${name}' isn't cached.`);

        if (typeof (cachedCommand) === 'string')
            throw new Error(`Command '${name}' is cached as an alias.`);

        return cachedCommand;
    }

    resolve(commandName: string): CachedCommand | undefined {
        const command = this.#commandsCache.get(commandName.toLowerCase());

        if (typeof (command) === 'string') {
            return this.getCommand(command);
        }

        return command;
    }

    addSuccessfulUseCount(command: CachedCommand): void {
        const useCount = this.useCountCache.get(command.name);
        if (!useCount) {
            this.useCountCache.set(command.name, { count: 1, errorCount: 0 });
        }
        else {
            useCount.count += 1;
        }
    }

    addUnhandledErrorCount(command: CachedCommand): void {
        const useCount = this.useCountCache.get(command.name);
        if (!useCount) {
            this.useCountCache.set(command.name, { count: 0, errorCount: 1 });
        }
        else {
            useCount.errorCount += 1;
        }
    }

    get disabledMessagesRedisKey(): string {
        return 'disabled-command-messages';
    }

    async insertOrGetCommandDisabledMessage(command: CachedCommand): Promise<string> {
        const message = await this.redis.hashGet(this.disabledMessagesRedisKey, command.name);

        if (message === null) {
            const { disabled_message } = await this.database.commands.insertOrGetCommandDisabledMessage(command);
            await this.redis.hashSet(this.disabledMessagesRedisKey, command.name, disabled_message);
            return disabled_message;
        }

        return message;
    }

    enabledGuildRedisKey(guild: Guild): string {
        return `enabled-commands:guild:${guild.id}`;
    }

    async getIsGuildCommandDisabled(guild: Guild, command: CachedCommand): Promise<boolean> {
        const isEnabled = await this.redis.hashGet(this.enabledGuildRedisKey(guild), command.name);

        if (isEnabled === null) {
            const { exists } = await this.database.guildCommands.getIsGuildCommandDisabled(guild, command);
            await this.redis.hashSet(this.enabledGuildRedisKey(guild), command.name, (!exists) ? '1' : '0');
            await this.redis.expire(this.enabledGuildRedisKey(guild), 6 * 60 * 60);
            return exists;
        }

        return isEnabled === '0';
    }
}
