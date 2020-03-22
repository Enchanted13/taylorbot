'use strict';

const pgp = require('pg-promise')({
    capSQL: true
});

const PostgreSQLConfig = require('../config/postgresql.json');

const UserDAO = require('./dao/UserDAO.js');

const GuildRepository = require('./repositories/GuildRepository.js');
const UserRepository = require('./repositories/UserRepository.js');
const GuildMemberRepository = require('./repositories/GuildMemberRepository.js');
const UsernameRepository = require('./repositories/UsernameRepository.js');
const GuildNameRepository = require('./repositories/GuildNameRepository.js');
const InstagramCheckerRepository = require('./repositories/InstagramCheckerRepository.js');
const GuildCommandRepository = require('./repositories/GuildCommandRepository.js');
const CommandRepository = require('./repositories/CommandRepository.js');
const UserGroupRepository = require('./repositories/UserGroupRepository.js');
const RoleGroupRepository = require('./repositories/RoleGroupRepository.js');
const SpecialRoleRepository = require('./repositories/SpecialRoleRepository.js');
const ReminderRepository = require('./repositories/ReminderRepository.js');
const TextChannelRepository = require('./repositories/TextChannelRepository.js');
const AttributeRepository = require('./repositories/AttributeRepository.js');
const TextAttributeRepository = require('./repositories/TextAttributeRepository.js');
const IntegerAttributeRepository = require('./repositories/IntegerAttributeRepository.js');
const LocationAttributeRepository = require('./repositories/LocationAttributeRepository.js');
const RollStatsRepository = require('./repositories/RollStatsRepository.js');
const RpsStatsRepository = require('./repositories/RpsStatsRepository.js');
const GambleStatsRepository = require('./repositories/GambleStatsRepository.js');
const DailyPayoutRepository = require('./repositories/DailyPayoutRepository.js');
const ChannelCommandRepository = require('./repositories/ChannelCommandRepository.js');
const HeistStatsRepository = require('./repositories/HeistStatsRepository.js');
const BirthdayAttributeRepository = require('./repositories/BirthdayAttributeRepository.js');
const ProRepository = require('./repositories/ProRepository.js');

class DatabaseDriver {
    constructor() {
        this._db = pgp(PostgreSQLConfig);
        this._helpers = pgp.helpers;

        this._usersDAO = new UserDAO();

        this.guilds = new GuildRepository(this._db);
        this.users = new UserRepository(this._db, this._usersDAO);
        this.guildMembers = new GuildMemberRepository(this._db, this._helpers);
        this.usernames = new UsernameRepository(this._db);
        this.guildNames = new GuildNameRepository(this._db);
        this.instagramCheckers = new InstagramCheckerRepository(this._db);
        this.guildCommands = new GuildCommandRepository(this._db);
        this.commands = new CommandRepository(this._db, this._helpers);
        this.userGroups = new UserGroupRepository(this._db, this._helpers);
        this.roleGroups = new RoleGroupRepository(this._db);
        this.specialRoles = new SpecialRoleRepository(this._db);
        this.reminders = new ReminderRepository(this._db);
        this.textChannels = new TextChannelRepository(this._db);
        this.attributes = new AttributeRepository(this._db, this._helpers);
        this.textAttributes = new TextAttributeRepository(this._db);
        this.integerAttributes = new IntegerAttributeRepository(this._db);
        this.locationAttributes = new LocationAttributeRepository(this._db);
        this.rollStats = new RollStatsRepository(this._db, this._usersDAO);
        this.rpsStats = new RpsStatsRepository(this._db, this._usersDAO);
        this.gambleStats = new GambleStatsRepository(this._db, this._usersDAO);
        this.dailyPayouts = new DailyPayoutRepository(this._db, this._usersDAO);
        this.channelCommands = new ChannelCommandRepository(this._db);
        this.heistStats = new HeistStatsRepository(this._db, this._usersDAO);
        this.birthdays = new BirthdayAttributeRepository(this._db);
        this.pros = new ProRepository(this._db);
    }
}

module.exports = DatabaseDriver;