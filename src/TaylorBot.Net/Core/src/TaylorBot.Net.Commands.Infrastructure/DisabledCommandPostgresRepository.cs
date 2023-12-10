﻿using Dapper;
using TaylorBot.Net.Commands.Preconditions;
using TaylorBot.Net.Core.Infrastructure;

namespace TaylorBot.Net.Commands.Infrastructure;

public class DisabledCommandPostgresRepository(PostgresConnectionFactory postgresConnectionFactory) : IDisabledCommandRepository
{
    public async ValueTask<string> InsertOrGetCommandDisabledMessageAsync(CommandMetadata command)
    {
        await using var connection = postgresConnectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<string>(
            """
            INSERT INTO commands.commands (name, aliases, module_name) VALUES (@CommandName, @Aliases, @ModuleName)
            ON CONFLICT (name) DO UPDATE SET
                aliases = excluded.aliases,
                module_name = excluded.module_name
            RETURNING disabled_message;
            """,
            new
            {
                CommandName = command.Name,
                Aliases = command.Aliases ?? Array.Empty<string>(),
                ModuleName = command.ModuleName ?? string.Empty
            }
        );
    }

    public async ValueTask EnableGloballyAsync(string commandName)
    {
        await using var connection = postgresConnectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            "UPDATE commands.commands SET disabled_message = '' WHERE name = @CommandName;",
            new
            {
                CommandName = commandName
            }
        );
    }

    public async ValueTask<string> DisableGloballyAsync(string commandName, string disabledMessage)
    {
        await using var connection = postgresConnectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<string>(
            """
            UPDATE commands.commands SET disabled_message = @DisabledMessage WHERE name = @CommandName
            RETURNING disabled_message;
            """,
            new
            {
                CommandName = commandName,
                DisabledMessage = disabledMessage
            }
        );
    }
}
