'use strict';

const fs = require('fs/promises');
const path = require('path');

const { GlobalPaths } = require('globalobjects');

const commandsPath = GlobalPaths.commandsFolderPath;

class CommandLoader {
    static async loadAll() {
        const directories = await fs.readdir(commandsPath);

        const files = [];

        for (const directory of directories.map(d => path.join(commandsPath, d))) {
            const dirFiles = await fs.readdir(directory);
            dirFiles
                .map(f => path.join(directory, f))
                .forEach(f => files.push(f));
        }

        return files
            .map(path.parse)
            .filter(file => file.ext === '.js')
            .map(path.format)
            .map(filepath => {
                const Command = require(filepath);
                return new Command();
            });
    }
}

module.exports = CommandLoader;