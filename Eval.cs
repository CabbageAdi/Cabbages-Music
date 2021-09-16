using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cabbage_Music
{
    public class Eval
    {
        [Command("eval")]
        [Aliases("evalcs", "cseval", "roslyn")]
        [Description("Evaluates C# code.")]
        [Hidden]
        [RequireOwner]
        [Priority(1)]
        public async Task EvalCS(CommandContext ctx)
        {
            if (ctx.Message.ReferencedMessage is null) await EvalCS(ctx, ctx.RawArgumentString);
            else
            {
                string code = ctx.Message.ReferencedMessage.Content;
                if (code.Contains(ctx.Prefix))
                {
                    int index = code.IndexOf(' ');
                    code = code[++index..];
                }
                await EvalCS(ctx, code);
            }
        }


        [Command("eval")]
        [Priority(0)]
        public async Task EvalCS(CommandContext ctx, [RemainingText] string code)
        {
            DiscordMessage msg;

            int cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            int cs2 = code.LastIndexOf("```", StringComparison.Ordinal);

            if (cs1 is -1 || cs2 is -1)
            {
                cs1 = 0;
                cs2 = code.Length;
            }

            string cs = code.Substring(cs1, cs2 - cs1);

            msg = await ctx.RespondAsync("", new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor("#FF007F"))
                    .WithDescription("Evaluating...")
                    .Build())
                .ConfigureAwait(false);

            try
            {
                var globals = new TestVariables(ctx.Message, ctx.Client, ctx);

                var sopts = ScriptOptions.Default;
                sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text",
                    "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Entities",
                    "DSharpPlus.CommandsNext",
                    "Microsoft.Extensions.Logging", "System.IO", "System.Net.Http");
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location));

                sopts = sopts.WithReferences(asm);
                Script<object> script = CSharpScript.Create(cs, sopts, typeof(TestVariables));
                script.Compile();
                ScriptState<object> result = await script.RunAsync(globals).ConfigureAwait(false);
                if (result?.ReturnValue is not null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                    await msg.ModifyAsync(new DiscordEmbedBuilder
                    {
                        Title = "Evaluation Result",
                        Description = result.ReturnValue.ToString().Length < 4096 ? result.ReturnValue.ToString() : "Writing to console",
                        Color = new DiscordColor("#007FFF")
                    }.Build())
                        .ConfigureAwait(false);
                else
                    await msg.ModifyAsync(new DiscordEmbedBuilder
                    {
                        Title = "Evaluation Successful",
                        Description = "No result was returned.",
                        Color = new DiscordColor("#007FFF")
                    }.Build())
                        .ConfigureAwait(false);
                if (result?.ReturnValue is not null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()) && result.ReturnValue.ToString().Length > 4096)
                    ctx.Client.Logger.LogInformation($"Evaluation result:\n{result.ReturnValue}");
            }
            catch (Exception ex)
            {
                var errString = $"**{ex.GetType()}**: {ex.Message}\n{Formatter.Sanitize(ex.StackTrace)}";
                await msg.ModifyAsync(new DiscordEmbedBuilder
                {
                    Title = "Evaluation Failure",
                    Description = errString.Length < 4096 ? errString : "Writing to console",
                    Color = new DiscordColor("#FF0000")
                }.Build())
                    .ConfigureAwait(false);

                if (errString.Length > 4096)
                    ctx.Client.Logger.LogInformation($"Evaluation result:\n{errString}");
            }

        }

        public record TestVariables
        {

            public TestVariables(DiscordMessage msg, DiscordClient client, CommandContext ctx)
            {
                Client = client;
                this.ctx = ctx;
                Message = msg;
                Channel = msg.Channel;
                Guild = Channel.Guild;
                User = Message.Author;

                if (Guild != null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            public DiscordMessage Message { get; }
            public DiscordChannel Channel { get; }
            public DiscordGuild Guild { get; }
            public DiscordUser User { get; }
            public DiscordMember Member { get; }
            public CommandContext ctx { get; }

            public DiscordClient Client { get; }
        }
    }
}
