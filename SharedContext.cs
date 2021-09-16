using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.CommandsNext;
using DSharpPlus;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;

namespace Cabbage_Music
{
    public class SharedContext
    {
        public InteractionContext Interaction { get; set; }
        public CommandContext Command { get; set; }

        public DiscordChannel Channel { get; set; }
        public DiscordMember Member { get; set; }
        public DiscordClient Client { get; set; }
        public DiscordGuild Guild { get; set; }

        public async Task RespondAsync(string content = null, DiscordEmbed embed = null)
        {
            if (Command != null)
            {
                await Command.RespondAsync(content, embed);
            }
            if (Interaction != null)
            {
                var builder = new DiscordFollowupMessageBuilder();
                if (content != null) builder.WithContent(content);
                if (embed != null) builder.AddEmbed(embed);
                await Interaction.FollowUpAsync(builder);
            }
        }

        public async Task SendPaginatedResponseAsync(IEnumerable<Page> pages)
        {
            if (Command != null)
                await Channel.SendPaginatedMessageAsync(Member, pages);
            if (Interaction != null)
            {
                await Interaction.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sending queue."));
                await Channel.SendPaginatedMessageAsync(Member, pages);
            }
        }

        public SharedContext(CommandContext ctx)
        {
            this.Command = ctx;
            this.Channel = ctx.Channel;
            this.Member = ctx.Member;
            this.Guild = ctx.Guild;
            this.Client = ctx.Client;
            this.Interaction = null;
        }
        public SharedContext(InteractionContext ctx)
        {
            this.Interaction = ctx;
            this.Channel = ctx.Channel;
            this.Member = ctx.Guild.Members[ctx.Member.Id];
            this.Guild = ctx.Guild;
            this.Client = ctx.Client;
            this.Command = null;
        }

        public static implicit operator SharedContext(CommandContext ctx)
        {
            return new SharedContext(ctx);
        }
        
        public static implicit operator SharedContext(InteractionContext ctx)
        {
            return new SharedContext(ctx);
        }
    }
}
