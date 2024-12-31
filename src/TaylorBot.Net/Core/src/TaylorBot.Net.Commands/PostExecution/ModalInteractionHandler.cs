using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaylorBot.Net.Core.Client;
using static TaylorBot.Net.Commands.PostExecution.ModalSubmit;

namespace TaylorBot.Net.Commands.PostExecution;

public record ModalSubmit(
    string Id,
    string Token,
    InteractionCustomId CustomId,
    string UserId,
    string? GuildId,
    IReadOnlyList<TextInputSubmit> TextInputs,
    Interaction RawInteraction
) : IInteraction
{
    public record TextInputSubmit(string CustomId, string Value);
}

public record ModalComponentHandlerInfo(bool IsPrivateResponse);

public interface IModalComponentHandler
{
    ModalComponentHandlerInfo Info { get; }

    Task HandleAsync(ModalSubmit submit);
}

public interface IModalHandler : IModalComponentHandler
{
    abstract static string CustomIdName { get; }
}

public record ModalCallback(Func<ModalSubmit, ValueTask> SubmitAsync, bool IsPrivateResponse);

public class ModalInteractionHandler(IServiceProvider services, ILogger<ModalInteractionHandler> logger)
{
    private readonly Dictionary<string, ModalCallback> _callbacks = [];

    private InteractionResponseClient CreateInteractionClient() => services.GetRequiredService<InteractionResponseClient>();

    public async ValueTask HandleAsync(Interaction interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction.data);
        ArgumentNullException.ThrowIfNull(interaction.data.custom_id);
        ArgumentNullException.ThrowIfNull(interaction.data.components);

        ModalSubmit submit = new(
            interaction.id,
            interaction.token,
            new(interaction.data.custom_id),
            interaction.user != null ? interaction.user.id : interaction.member!.user.id,
            interaction.guild_id,
            interaction.data.components.Select(c => c.components![0]).Select(c => new TextInputSubmit(c.custom_id!, c.value!)).ToList(),
            interaction
        );

        var handler = services.GetKeyedService<IModalComponentHandler>(submit.CustomId.Name);
        if (handler != null)
        {
            await CreateInteractionClient().SendAckResponseWithLoadingMessageAsync(submit, handler.Info.IsPrivateResponse);

            // TODO: Try/catch with default modal error?
            await handler.HandleAsync(submit);
        }
        else if (_callbacks.TryGetValue(submit.CustomId.RawId, out var callback))
        {
            await CreateInteractionClient().SendAckResponseWithLoadingMessageAsync(submit, callback.IsPrivateResponse);

            await callback.SubmitAsync(submit);
        }
        else
        {
            logger.LogWarning("Modal create without callback: {Interaction}", interaction);
        }
    }

    public void AddCallback(string customId, ModalCallback callback)
    {
        _callbacks.Add(customId, callback);
    }

    public void RemoveCallback(string customId)
    {
        _callbacks.Remove(customId);
    }
}
