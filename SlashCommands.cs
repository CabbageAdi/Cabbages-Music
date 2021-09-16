using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SpotifyAPI.Web;
using DSharpPlus;

namespace Cabbage_Music
{
    public class SlashCommands : ApplicationCommandModule
    {
        public SpotifyClient Spotify { get; set; }
        public Random Rand { get; set; }

        public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            return true;
        }

        [SlashCommand("Play","Plays a song, Youtube playlist or Spotify playlist on Youtube")]
        public Task Play(InteractionContext ctx, [Option("song", "The name or link of the song to play, or a link to a playlist")] string song, [Option("Shuffle", "If loading a playlist, whether to shuffle it.")] bool shuffle = false)
            => PlayCommand.Play(ctx, song, Spotify, shuffle, Rand);


        [SlashCommand("Join","Joins the voice chat")]
        public Task Join(InteractionContext ctx, [Option("channel", "The voice channel to join")] DiscordChannel channel = null)
            => Music.Join(ctx, channel);

        [SlashCommand("Leave", "Leaves the voice chat")]
        public Task Leave(InteractionContext ctx)
            => Music.Leave(ctx);

        [SlashCommand("Queue", "Take a look at your song queue")]
        public Task Queue(InteractionContext ctx)
            => Music.Queue(ctx);

        [SlashCommand("remove", "Removes a track from the queue")]
        public Task Remove(InteractionContext ctx, [Option("index", "The index in the queue of the song to remove")] long index)
            => Music.Remove(ctx, index);

        [SlashCommand("Pause", "Pauses playback")]
        public Task Pause(InteractionContext ctx)
            => Music.Pause(ctx);

        [SlashCommand("Resume", "Resumes playback")]
        public Task Resume(InteractionContext ctx)
            => Music.Resume(ctx);

        [SlashCommand("Volume", "Adjusts the volume, takes input from 0 to 100")]
        public Task Volume(InteractionContext ctx, [Option("volume", "The new volume")] long vol)
            => Music.Volume(ctx, vol);

        [SlashCommand("Song", "Gives information about the currently playing song")]
        public Task Song(InteractionContext ctx)
            => Music.Song(ctx);

        [SlashCommand("bassboost","B A S S")]
        public Task Bass(InteractionContext ctx)
            => Music.BB(ctx);

        [SlashCommand("Skip", "Skips to the next song in the queue or stops playback")]
        public Task Skip(InteractionContext ctx)
            => Music.Skip(ctx);

        [SlashCommand("reset", "Resets all equalization")]
        public Task Reset(InteractionContext ctx)
            => Music.Reset(ctx);

        [SlashCommand("shuffle", "Shuffles all the songs in the queue")]
        public Task Shuffle(InteractionContext ctx)
            => Music.Shuffle(ctx, Rand);

        [SlashCommand("seek", "Skips ahead the specified amount of seconds (defaults to 15)")]
        public Task Seek(InteractionContext ctx, [Option("seconds", "The amount of seconds to skip")] double seconds = 15)
            => Music.Seek(ctx, seconds);

        [SlashCommand("loop", "Loops the currently playing song")]
        public Task Loop(InteractionContext ctx)
            => Music.Loop(ctx);

        [SlashCommand("help", "Displays the help command")]
        public Task Help(InteractionContext ctx, [Option("command", "The specific command to show help for.")] string command = null)
            => Other.Help(ctx, command);

        [SlashCommand("invite", "Generates an invite for the bot")]
        public Task Invite(InteractionContext ctx)
            => Other.Invite(ctx);
    }
}
