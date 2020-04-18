import Log = require('../../tools/Logger.js');
import * as pgPromise from 'pg-promise';
import { CachedCommand } from '../../client/registry/CachedCommand';

export type DatabaseCommand = { name: string; module_name: string; enabled: boolean };

export class CommandRepository {
    #db: pgPromise.IDatabase<unknown>;
    constructor(db: pgPromise.IDatabase<unknown>) {
        this.#db = db;
    }

    async setEnabled(commandName: string, enabled: boolean): Promise<{ enabled: boolean }> {
        try {
            return await this.#db.one(
                'UPDATE commands.commands SET enabled = $[enabled] WHERE name = $[name] RETURNING *;',
                {
                    enabled: enabled,
                    name: commandName
                }
            );
        }
        catch (e) {
            Log.error(`Setting command '${commandName}' enabled to '${enabled}': ${e}`);
            throw e;
        }
    }

    async addUseCount(commandNames: string[], useCount: number, errorCount: number): Promise<void> {
        try {
            await this.#db.none(
                `UPDATE commands.commands SET
                    successful_use_count = successful_use_count + $[use_count],
                    unhandled_error_count = unhandled_error_count + $[error_count]
                WHERE name IN ($[names:csv]);`,
                {
                    use_count: useCount,
                    error_count: errorCount,
                    names: commandNames
                }
            );
        }
        catch (e) {
            Log.error(`Adding ${useCount} use count and ${errorCount} error count to command '${commandNames.join()}': ${e}`);
            throw e;
        }
    }

    async insertOrGetIsCommandDisabled(command: CachedCommand): Promise<{ enabled: boolean }> {
        try {
            return await this.#db.one(
                `INSERT INTO commands.commands (name, aliases, module_name) VALUES ($[command_name], $[aliases], $[module_name])
                ON CONFLICT (name) DO UPDATE SET
                    aliases = excluded.aliases,
                    module_name = excluded.module_name
                RETURNING enabled;`,
                {
                    command_name: command.name,
                    aliases: command.command.aliases,
                    module_name: command.command.group
                }
            );
        }
        catch (e) {
            Log.error(`Inserting or getting is disabled for ${command.name}: ${e}`);
            throw e;
        }
    }

    async getCommand(nameOrAlias: string): Promise<DatabaseCommand | null> {
        try {
            return await this.#db.oneOrNone(
                `SELECT name, enabled, module_name FROM commands.commands WHERE name = $[name_or_alias] OR $[name_or_alias] = ANY(aliases);`,
                {
                    name_or_alias: nameOrAlias
                }
            );
        }
        catch (e) {
            Log.error(`Getting command name for ${nameOrAlias}: ${e}`);
            throw e;
        }
    }

    async getCommandsFromModuleName(moduleName: string): Promise<DatabaseCommand[]> {
        try {
            return await this.#db.manyOrNone(
                `SELECT name, enabled, module_name FROM commands.commands WHERE LOWER(module_name) = $[module_name];`,
                {
                    module_name: moduleName
                }
            );
        }
        catch (e) {
            Log.error(`Getting command names from module name ${moduleName}: ${e}`);
            throw e;
        }
    }
}