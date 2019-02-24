'use strict';

const Log = require('../../tools/Logger.js');
const Format = require('../../modules/DiscordFormatter.js');

class BirthdayAttributeRepository {
    constructor(db) {
        this._db = db;
    }

    async get(user) {
        try {
            return await this._db.oneOrNone(
                `SELECT birthday::text FROM attributes.birthdays WHERE user_id = $[user_id];`,
                {
                    user_id: user.id
                }
            );
        }
        catch (e) {
            Log.error(`Getting birthday attribute for user ${Format.user(user)}: ${e}`);
            throw e;
        }
    }

    async set(user, birthdayString) {
        try {
            return await this._db.one(
                `INSERT INTO attributes.birthdays (user_id, birthday)
                VALUES ($[user_id], date $[birthday_string])
                ON CONFLICT (user_id) DO UPDATE
                  SET birthday = excluded.birthday
                RETURNING user_id, birthday::text;`,
                {
                    user_id: user.id,
                    birthday_string: birthdayString
                }
            );
        }
        catch (e) {
            Log.error(`Setting birthday attribute to '${birthdayString}' for user ${Format.user(user)}: ${e}`);
            throw e;
        }
    }

    async clear(user) {
        try {
            return await this._db.oneOrNone(
                `DELETE FROM attributes.birthdays WHERE user_id = $[user_id] RETURNING user_id, birthday::text;`,
                {
                    user_id: user.id
                }
            );
        }
        catch (e) {
            Log.error(`Clearing birthday attribute for user ${Format.user(user)}: ${e}`);
            throw e;
        }
    }

    async getUpcomingInGuild(guild, limit) {
        try {
            return await this._db.any(
                `SELECT user_id,
                CASE
                    WHEN normalized_birthday < CURRENT_DATE THEN make_date(
                        date_part('year', normalized_birthday)::int + 1,
                        date_part('month', normalized_birthday)::int,
                        date_part('day', normalized_birthday)::int
                    )::text
                    ELSE normalized_birthday::text
                END 
                AS next_birthday
                FROM attributes.birthdays, make_date(
                    date_part('year', CURRENT_DATE)::int,
                    date_part('month', birthday)::int,
                    date_part('day', birthday)::int
                ) AS normalized_birthday
                WHERE user_id IN (
                   SELECT user_id
                   FROM guilds.guild_members
                   WHERE guild_id = $[guild_id] AND alive = TRUE
                )
                ORDER BY next_birthday
                LIMIT $[limit];`,
                {
                    guild_id: guild.id,
                    limit
                }
            );
        }
        catch (e) {
            Log.error(`Getting ${limit} upcoming birthdays for guild ${Format.guild(guild)}: ${e}`);
            throw e;
        }
    }
}

module.exports = BirthdayAttributeRepository;