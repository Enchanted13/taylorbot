'use strict';

const Inhibitor = require('../structures/Inhibitor.js');
const Log = require('../tools/Logger.js');
const Format = require('../modules/DiscordFormatter.js');

class CooldownInhibitor extends Inhibitor {
    shouldBeBlocked({ message, client }, command) {
        const { author } = message;
        const { lastCommand, lastAnswered } = client.master.registry.users.get(author.id);

        if (lastAnswered < lastCommand) {
            Log.verbose(`Command '${command.name}' can't be used by ${Format.user(author)} because they have not been answered. LastAnswered:${lastAnswered}, LastCommand:${lastCommand}.`);
            return true;
        }

        return false;
    }
}

module.exports = CooldownInhibitor;