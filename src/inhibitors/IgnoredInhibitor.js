'use strict';

const { Paths } = require('globalobjects');

const Inhibitor = require(Paths.Inhibitor);
const Log = require(Paths.Logger);
const Format = require(Paths.DiscordFormatter);
const TimeUtil = require(Paths.TimeUtil);

class IgnoredInhibitor extends Inhibitor {
    shouldBeBlocked({ message, client }, command) {
        const { author } = message;

        const commandTime = Date.now();
        const { ignoreUntil } = client.master.registry.users.get(author.id);

        if (commandTime < ignoreUntil) {
            Log.verbose(`Command '${command.name}' can't be used by ${Format.user(author)} because they are ignored until ${TimeUtil.formatLog(ignoreUntil)}.`);
            return true;
        }

        return false;
    }
}

module.exports = IgnoredInhibitor;