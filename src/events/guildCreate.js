'use strict';

const { GlobalPaths } = require('globalobjects');

const EventHandler = require(GlobalPaths.EventHandler);
const Log = require(GlobalPaths.Logger);
const Format = require(GlobalPaths.DiscordFormatter);

class GuildCreate extends EventHandler {
    async handler(taylorbot, guild) {
        Log.info(`Joined guild ${Format.guild(guild)}.`);

        const { database, oldRegistry } = taylorbot;

        const members = await guild.members.fetch();
        const joinTime = members.get(taylorbot.user.id);

        if (!oldRegistry.guilds.has(guild.id)) {
            Log.info(`Adding new guild ${Format.guild(guild)}.`);
            await oldRegistry.guilds.addGuild(guild);
            await database.addGuildName(guild, joinTime);
        }
        else {
            const latestGuildName = await database.getLatestGuildName(guild);
            if (guild.name !== latestGuildName) {
                await database.addGuildName(guild, joinTime);
                Log.info(`Added new Guild Name for ${Format.guild(guild)}. Old Guild Name was ${latestGuildName.guild_name}.`);
            }
        }

        const guildMembers = await database.getAllGuildMembersInGuild(guild);
        let latestUsernames = await database.getLatestUsernames();

        for (const member of members.values()) {
            const { user } = member;
            if (!oldRegistry.users.has(member.id)) {
                Log.info(`Found new user ${Format.user(user)} in guild ${Format.guild(guild)}.`);
                await oldRegistry.users.addUser(user);
                await database.addGuildMember(member);
                await database.addUsername(user, joinTime);
            }
            else {
                if (!guildMembers.some(gm => gm.user_id === member.id)) {
                    await database.addGuildMember(member);
                    Log.info(`Added new member ${Format.member(member)}.`);
                }

                const latestUsername = latestUsernames.find(u => u.user_id === user.id);
                if (!latestUsername || latestUsername.username !== user.username) {
                    await database.addUsername(user, joinTime);
                    latestUsernames = latestUsernames.filter(u => u.user_id !== user.id);
                    latestUsernames.push({ 'user_id': user.id, 'username': user.username });
                    Log.info(`Added new username for ${Format.user(user)}.`);
                }
            }
        }
    }
}

module.exports = GuildCreate;