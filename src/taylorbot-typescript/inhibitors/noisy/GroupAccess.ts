import { Permissions, Message, GuildMember, User } from 'discord.js';

import { NoisyInhibitor } from '../NoisyInhibitor';
import UserGroups = require('../../client/UserGroups.js');
import DISCORD_CONFIG = require('../../config/config.json');
import { Registry } from '../../client/registry/Registry';
import { GuildRegistry } from '../../client/registry/GuildRegistry';
import GroupRegistry = require('../../client/registry/GroupRegistry');
import { CachedCommand } from '../../client/registry/CachedCommand';
import { CommandMessageContext } from '../../commands/CommandMessageContext';
import { MessageContext } from '../../structures/MessageContext';

class GroupAccessInhibitor extends NoisyInhibitor {
    getBlockedMessage(messageContext: MessageContext, command: CachedCommand): Promise<{ log: string; ui: string } | null> {
        const { author, message, client } = messageContext;
        const { registry } = client.master;
        const { minimumGroup } = command.command;
        if (GroupAccessInhibitor.groupHasAccess(minimumGroup.accessLevel, author, message, registry)) {
            return Promise.resolve(null);
        }

        let blockMessage = `You can't use \`${command.name}\` because it requires you to be part of the '${minimumGroup.name}' group.`;

        if (!minimumGroup.isSpecial) {
            blockMessage += `\nTo assign a role to a group, use \`${new CommandMessageContext(messageContext, registry.commands.getCommand('setrolegroup')).usage()}\`.`;
        }

        return Promise.resolve({
            ui: blockMessage,
            log: `They don't have the minimum group '${minimumGroup.name}'.`
        });
    }

    static groupHasAccess(minimumGroupLevel: number, author: User, message: Message, registry: Registry): boolean {
        const { accessLevel } = author.id === DISCORD_CONFIG.MASTER_ID ? UserGroups.Master : UserGroups.Everyone;

        if (accessLevel >= minimumGroupLevel)
            return true;

        const { member } = message;

        if (member) {
            if (member.guild.ownerID === member.id && UserGroups.GuildOwners.accessLevel >= minimumGroupLevel)
                return true;

            if (UserGroups.GuildManagers.accessLevel >= minimumGroupLevel && member.hasPermission(Permissions.FLAGS.MANAGE_GUILD))
                return true;

            if (GroupAccessInhibitor.roleGroupHasAccess(minimumGroupLevel, member, registry.guilds, registry.groups))
                return true;
        }

        return false;
    }

    static roleGroupHasAccess(minimumGroupLevel: number, member: GuildMember, guilds: GuildRegistry, groups: GroupRegistry): boolean {
        const guildRoles = guilds.get(member.guild.id).roleGroups;
        const ownedGroups = member.roles.map(role => guildRoles[role.id]).filter(g => !!g);

        for (const group of ownedGroups) {
            const { accessLevel } = groups.get(group);
            if (accessLevel >= minimumGroupLevel)
                return true;
        }

        return false;
    }
}

export = GroupAccessInhibitor;
