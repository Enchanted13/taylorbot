'use strict';

const { Paths } = require('globalobjects');

const UserGroups = require(Paths.UserGroups);
const Format = require(Paths.DiscordFormatter);
const Command = require(Paths.Command);
const EmbedUtil = require(Paths.EmbedUtil);

class SetRoleGroupCommand extends Command {
    constructor() {
        super({
            name: 'setrolegroup',
            aliases: ['srg'],
            group: 'admin',
            memberName: 'setrolegroup',
            description: 'Attaches a user group to a role.',
            examples: ['setrolegroup @admins Moderators', 'srg owners Moderators'],
            guildOnly: true,

            args: [
                {
                    key: 'role',
                    label: 'role',
                    type: 'role',
                    prompt: 'What role would you like to attach a group to?'
                },
                {
                    key: 'group',
                    label: 'group',
                    type: 'user-group',
                    prompt: 'What group would you like to attach to the role?'
                }
            ]
        }, UserGroups.Master);
    }

    async run({ message, client }, { role, group }) {
        const { roleGroups } = client.master.oldRegistry;
        if (roleGroups.getRoleGroup(role, group)) {
            return client.sendEmbed(message.channel,
                EmbedUtil.error(`User Group '${group.name}' is already attached to Role ${Format.role(role, '#name (`#id`)')}.`));
        }
        else {
            await roleGroups.addRoleGroup(role, group);
            return client.sendEmbed(message.channel,
                EmbedUtil.success(`Attached User Group '${group.name}' to Role ${Format.role(role, '#name (`#id`)')}.`));
        }
    }
}

module.exports = SetRoleGroupCommand;