'use strict';

const { MessageEmbed } = require('discord.js');

const Loggable = require('./Loggable.js');
const DiscordEmbedFormatter = require('../DiscordEmbedFormatter.js');

class GuildMemberJoinedLoggable extends Loggable {
    constructor(member) {
        super();
        this.member = member;
    }

    toEmbed() {
        const { user, joinedAt } = this.member;
        const avatarURL = DiscordEmbedFormatter.getAvatarURL(user);

        return new MessageEmbed()
            .setAuthor(`${user.tag} (${user.id})`, avatarURL, avatarURL)
            .setColor('#43b581')
            .setFooter('User joined')
            .setTimestamp(joinedAt);
    }
}

module.exports = GuildMemberJoinedLoggable;