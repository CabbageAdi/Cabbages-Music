using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using System;

namespace Cabbage_Music
{
    public class Other : BaseCommandModule
    {
        [Command("invite"), Description("Generates an invite for the bot")]
        public async Task Invite(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Description = $"click [here](https://discord.com/api/oauth2/authorize?client_id={ctx.Client.CurrentApplication.Id}&permissions=274940152896&scope=bot%20applications.commands) to add the bot to your server."
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("help"), Description("Displays the help command")]
        public async Task Help(CommandContext ctx, string command = null)
        {
            if(command == null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Help",
                    Description = "Use c! before every command\n"
                };
                string[] commandlist = new string[] { };
                foreach(var comm in ctx.Client.GetCommandsNext().RegisteredCommands.Values)
                {
                    if(!commandlist.Contains($"`{comm.Name}` - {comm.Description}"))
                    {
                        commandlist = commandlist.Append($"`{comm.Name}` - {comm.Description}").ToArray();
                    }
                }
                embed.AddField("Commands", string.Join('\n', commandlist));
                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                var cnx = ctx.Client.GetCommandsNext();
                if(cnx.FindCommand(command, out _) == null)
                {
                    await ctx.RespondAsync("This command doesn't exist!");
                    return;
                }
                var comm = cnx.FindCommand(command, out _);
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Help",
                    Description = $"`{comm.Name}` - {comm.Description}"
                };
                string args = "";
                foreach(var arg in comm.Overloads.First().Arguments)
                {
                    args += arg.Name + " ";
                }
                embed.AddField("Usage", $"c!{comm.Name} {args}");
                await ctx.RespondAsync(embed: embed);
            }
        }

        [Command("ping"), Description("Responds with the latency in ms")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync($"The ping is {ctx.Client.Ping}ms");
        }
    }
}
