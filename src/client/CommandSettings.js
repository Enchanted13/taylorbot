'use strict';

const { GlobalPaths } = require('globalobjects');

const Log = require(GlobalPaths.Logger);
const CommandLoader = require(GlobalPaths.CommandLoader);

class CommandSettings extends Map {
    constructor(database) {
        super();
        this.database = database;
    }

    async loadAll() {
        const fileCommands = await CommandLoader.loadAll();
        let commands = await this.database.getAllCommands();

        const fileCommandNames = Object.keys(fileCommands);

        const databaseCommandsNotInFiles = commands.filter(n =>
            !fileCommandNames.some(fc => fc.name === n)
        );
        if (databaseCommandsNotInFiles.length > 0)
            throw new Error(`Found database commands not in files: ${databaseCommandsNotInFiles.join(',')}.`);

        const fileCommandsNotInDatabase = fileCommandNames.filter(name =>
            !commands.some(c => c.name === name)
        );

        if (fileCommandsNotInDatabase.length > 0) {
            Log.info(`Found new file commands ${fileCommandsNotInDatabase.join(',')}. Adding to database.`);

            await this.database.addCommands(
                fileCommandsNotInDatabase.map(name => {
                    return { 'name': name };
                })
            );

            commands = await this.database.getAllCommands();
        }

        fileCommandNames.forEach(name => {
            const { alternateNames, ...command } = fileCommands[name];
            this.cacheCommand(name, command);
            alternateNames.forEach(alt => this.cacheAlternateName(alt, name));
        });

        commands.forEach(c => this.cacheDatabaseCommand(c));

        const guildCommands = await this.database.getAllGuildCommands();
        guildCommands.forEach(gc => this.cacheGuildCommand(gc));
    }

    cacheCommand(name, fileCommand) {
        if (this.has(name))
            Log.warn(`Caching command ${name}, was already cached, overwriting.`);

        this.set(name, fileCommand);
    }

    cacheAlternateName(alternateName, commandName) {
        if (!this.has(commandName))
            throw new Error(`Can't cache alternate name of command ${commandName}, because it is not cached.`);

        if (this.has(alternateName))
            throw new Error(`Can't cache alternate name ${alternateName} for ${commandName} because it is already cached.`);

        this.set(alternateName, commandName);
    }

    cacheDatabaseCommand(databaseCommand) {
        const command = this.get(databaseCommand.name);
        if (command === undefined)
            throw new Error(`Caching database command ${databaseCommand.name}, command was not already cached.`);

        command.enabled = databaseCommand.enabled;
        command.disabledIn = {};

        this.set(databaseCommand.name, command);
    }

    cacheGuildCommand(databaseGuildCommand) {
        const command = this.get(databaseGuildCommand.command_name);
        if (command === undefined)
            throw new Error(`Caching guild command ${databaseGuildCommand.command_name} (${databaseGuildCommand.guild_id}), command was not already cached.`);

        command.disabledIn[databaseGuildCommand.guild_id] = true;

        this.set(databaseGuildCommand.command_name, command);
    }

    getCommand(name) {
        let command = this.get(name);
        if (typeof (command) === 'string')
            return this.get(command);

        return command;
    }
}

module.exports = CommandSettings;