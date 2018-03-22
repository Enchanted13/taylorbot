'use strict';

const fs = require('fs');
const path = require('path');

const { GlobalPaths } = require('globalobjects');

const watchersPath = GlobalPaths.watchersFolderPath;

const requireWatcher = watcherName => require(path.join(watchersPath, watcherName));

class MessageWatcherFeeder {
    constructor() {
        this._watchers = new Map();
        this._loadAll();
    }

    _loadAll() {
        const files = fs.readdirSync(watchersPath);
        files.forEach(filename => {
            const filePath = path.parse(filename);
            if (filePath.ext === '.js') {
                const watcher = requireWatcher(filePath.base);
                if (watcher.enabled)
                    this._watchers.set(filePath.name, watcher);
            }
        });
    }

    feedAll(client, message) {
        this._watchers.forEach(watcher => {
            watcher.messageHandler(client, message);
        });
    }
}

module.exports = MessageWatcherFeeder;