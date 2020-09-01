﻿using Dapper;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaylorBot.Net.Commands.Discord.Program.Taypoints.Domain;
using TaylorBot.Net.Core.Infrastructure;
using TaylorBot.Net.Core.Snowflake;

namespace TaylorBot.Net.Commands.Discord.Program.Taypoints.Infrastructure
{
    public class TaypointWillPostgresRepository : ITaypointWillRepository
    {
        private readonly PostgresConnectionFactory _postgresConnectionFactory;

        public TaypointWillPostgresRepository(PostgresConnectionFactory postgresConnectionFactory)
        {
            _postgresConnectionFactory = postgresConnectionFactory;
        }

        private class WillGetDto
        {
            public string beneficiary_user_id { get; set; } = null!;
            public string username { get; set; } = null!;
        }

        public async ValueTask<Will?> GetWillAsync(IUser owner)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            var willDto = await connection.QuerySingleOrDefaultAsync<WillGetDto?>(
                @"SELECT beneficiary_user_id, username
                FROM users.taypoint_wills INNER JOIN users.users ON taypoint_wills.beneficiary_user_id = users.user_id
                WHERE owner_user_id = @UserId;",
                new
                {
                    UserId = owner.Id.ToString()
                }
            );

            return willDto != null ? new Will(
                beneficiaryUserId: new SnowflakeId(willDto.beneficiary_user_id),
                beneficiaryUsername: willDto.username
            ) : null;
        }

        private class WillAddDto
        {
            public string beneficiary_user_id { get; set; } = null!;
            public string username { get; set; } = null!;
        }

        public async ValueTask<IWillAddResult> AddWillAsync(IUser owner, IUser beneficiary)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            var willAddDto = await connection.QuerySingleAsync<WillAddDto>(
                @"WITH insert_will AS (
                    INSERT INTO users.taypoint_wills (owner_user_id, beneficiary_user_id) VALUES (@OwnerId, @BeneficiaryId)
                    ON CONFLICT (owner_user_id) DO UPDATE SET
                        owner_user_id = users.taypoint_wills.owner_user_id
                    RETURNING beneficiary_user_id
                )
                SELECT beneficiary_user_id, username
                FROM users.users, insert_will
                WHERE user_id = insert_will.beneficiary_user_id;",
                new
                {
                    OwnerId = owner.Id.ToString(),
                    BeneficiaryId = beneficiary.Id.ToString()
                }
            );

            if (willAddDto.beneficiary_user_id == beneficiary.Id.ToString())
            {
                return new WillAddedResult();
            }
            else
            {
                return new WillNotAddedResult(
                    currentBeneficiaryId: new SnowflakeId(willAddDto.beneficiary_user_id),
                    currentBeneficiaryUsername: willAddDto.username
                );
            }
        }

        private class WillRemoveDto
        {
            public string beneficiary_user_id { get; set; } = null!;
            public string username { get; set; } = null!;
        }

        public async ValueTask<IWillRemoveResult> RemoveWillWithOwnerAsync(IUser owner)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            var willRemoveDto = await connection.QuerySingleOrDefaultAsync<WillRemoveDto>(
                @"WITH delete_will AS (
                    DELETE FROM users.taypoint_wills
                    WHERE owner_user_id = @OwnerId
                    RETURNING beneficiary_user_id
                )
                SELECT beneficiary_user_id, username
                FROM users.users, delete_will
                WHERE user_id = delete_will.beneficiary_user_id;",
                new
                {
                    OwnerId = owner.Id.ToString()
                }
            );

            if (willRemoveDto == null)
            {
                return new WillNotRemovedResult();
            }
            else
            {
                return new WillRemovedResult(
                    removedBeneficiaryId: new SnowflakeId(willRemoveDto.beneficiary_user_id),
                    removedBeneficiaryUsername: willRemoveDto.username
                );
            }
        }

        private class WillWithBeneficiaryDto
        {
            public string user_id { get; set; } = null!;
            public DateTimeOffset max_last_spoke_at { get; set; }
            public string owner_username { get; set; } = null!;
        }

        public async ValueTask<IReadOnlyCollection<WillOwner>> GetWillsWithBeneficiaryAsync(IUser beneficiary)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            var willDtos = await connection.QueryAsync<WillWithBeneficiaryDto>(
                @"SELECT DISTINCT ON (user_id) user_id, last_spoke_at AS max_last_spoke_at, owner_username
                FROM guilds.guild_members INNER JOIN (
                    SELECT owner_user_id, username AS owner_username
                    FROM users.taypoint_wills INNER JOIN users.users ON taypoint_wills.owner_user_id = users.user_id
                    WHERE beneficiary_user_id = @UserId
                ) AS wills ON guild_members.user_id = wills.owner_user_id
                WHERE last_spoke_at IS NOT NULL AND user_id = owner_user_id
                ORDER BY user_id, last_spoke_at DESC;",
                new
                {
                    UserId = beneficiary.Id.ToString()
                }
            );

            return willDtos.Select(w => new WillOwner(
                ownerUserId: new SnowflakeId(w.user_id),
                ownerUsername: w.owner_username,
                ownerLatestSpokeAt: w.max_last_spoke_at
            )).ToList();
        }

        private class TransferDto
        {
            public string user_id { get; set; } = null!;
            public string username { get; set; } = null!;
            public long taypoint_count { get; set; }
            public long original_taypoint_count { get; set; }
        }

        public async ValueTask<IReadOnlyCollection<Transfer>> TransferAllPointsAsync(IReadOnlyCollection<SnowflakeId> fromUserIds, IUser toUser)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            var transferDtos = await connection.QueryAsync<TransferDto>(
                @"WITH
                old_u AS (
                    SELECT user_id, taypoint_count FROM users.users
                    WHERE user_id = @ReceiverId OR user_id = ANY(@FromUserIds) FOR UPDATE
                ),
                sum_gifters AS (
                    SELECT SUM(taypoint_count) AS sum_taypoints FROM old_u
                    WHERE user_id <> @ReceiverId
                )
                UPDATE users.users AS u
                SET taypoint_count = (CASE
                    WHEN u.user_id = @ReceiverId
                    THEN u.taypoint_count + sum_gifters.sum_taypoints
                    ELSE 0
                END)
                FROM old_u, sum_gifters
                WHERE u.user_id = old_u.user_id
                RETURNING u.user_id, u.username, u.taypoint_count, old_u.taypoint_count AS original_taypoint_count;",
                new
                {
                    ReceiverId = toUser.Id.ToString(),
                    FromUserIds = fromUserIds.Select(u => u.Id.ToString()).ToList()
                }
            );

            return transferDtos.Select(t => new Transfer(
                userId: new SnowflakeId(t.user_id),
                username: t.username,
                taypointCount: t.taypoint_count,
                originalTaypointCount: t.original_taypoint_count
            )).ToList();
        }

        public async ValueTask RemoveWillsWithBeneficiaryAsync(IReadOnlyCollection<SnowflakeId> ownerUserIds, IUser beneficiary)
        {
            using var connection = _postgresConnectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                @"DELETE FROM users.taypoint_wills
                WHERE beneficiary_user_id = @BeneficiaryUserId AND owner_user_id = ANY(@OwnerUserIds);",
                new
                {
                    BeneficiaryUserId = beneficiary.Id.ToString(),
                    OwnerUserIds = ownerUserIds.Select(u => u.Id.ToString()).ToList()
                }
            );
        }
    }
}
