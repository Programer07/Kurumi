using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Music
{
    public class Music : ModuleBase
    {
        private const int NORMAL_QUEUE = 5;
        private const int VOTE_QUEUE = 10;
        private const int DONATE_QUEUE = 20;
        public static ConcurrentDictionary<ulong, SongSelect> Selecting = new ConcurrentDictionary<ulong, SongSelect>();

        [Command("play")]
        [Alias("addqueue", "queue")]
        [RequireContext(ContextType.Guild)]
        public async Task Play([Remainder, Optional]string Input)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Input == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_url_null"]);
                    return;
                }
                await Context.Message.DeleteAsync();
                //Get what type the input is. YT, Coundcloud, keyword
                var inputType = GetInputType(Input);
                ISongInfo Song = null;
                switch (inputType)
                {
                    case InputType.Youtube:
                        if(!YoutubeService.ValidVideo(Input)) //Validate Url
                        {
                            await Context.Channel.SendEmbedAsync(lang["music_invalid_youtube_url"]);
                            return;
                        }
                        Song = new YoutubeSongInfo(Input, Context.User);
                        break;
                    case InputType.Soundcloud:
                        if (!SoundCloudService.ValidUrl(Input)) //Validate Url
                        {
                            await Context.Channel.SendEmbedAsync(lang["music_invalid_soundcloud_url"]);
                            return;
                        }
                        Song = new SoundCloudSongInfo(Input, Context.User);
                        break;
                    case InputType.Keyword:
                        if(Selecting.ContainsKey(Context.Guild.Id)) //Only one select/guild
                        {
                            await Context.Channel.SendEmbedAsync(lang["music_vote_already_started"]);
                            return;
                        }

                        List<ISongInfo> searchResult = await YoutubeService.Search(Input, Context.User);
                        if(searchResult.Count == 0) //Search didn't return any video
                        {
                            await Context.Channel.SendEmbedAsync(lang["music_not_found"]);
                            return;
                        }

                        //Format and send the results
                        string resultString = string.Empty;
                        for (int i = 0; i < searchResult.Count && i < 10; i++)
                        {
                            resultString += $"{i + 1}) **{searchResult[i].Title}**\n";
                        }
                        await Context.Channel.SendEmbedAsync(resultString, Title: lang["music_result", "KEYWORD", Input], Footer: lang["music_result_footer"]);

                        //Wait for user pick
                        int Timer = 600; //60 sec
                        var s = new SongSelect() { User = Context.User.Id, Selected = -1 };
                        Selecting.TryAdd(Context.Guild.Id, s);

                        WaitForUser:
                        while(Timer != 0 && s.Selected == -1)
                        {
                            await Task.Delay(100);
                            Timer--;
                        }

                        if(Timer == 0 || s.Selected == -2) //User didn't select
                        {
                            Selecting.TryRemove(Context.Guild.Id, out _);
                            return;
                        }
                        else
                        {
                            var Pick = s.Selected;
                            if(Pick < 1 || Pick >= searchResult.Count)
                            {
                                await Context.Channel.SendEmbedAsync(lang["music_invalid_id"]);
                                goto WaitForUser;
                            }
                            Selecting.TryRemove(Context.Guild.Id, out _);
                            Song = searchResult[Pick - 1];
                        }

                        break;
                }

                float Volume = (GuildConfigDatabase.GetOrFake(Context.Guild.Id).Volume) / 100F;

                //Check if the user is in a voice channel
                if ((Context.User as IVoiceState).VoiceChannel == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_user_not_in_voice"]);
                    return;
                }

                //Get or create a music player
                var player = GetMusicPlayer(Context.Guild.Id);

                if (player == null) //Play the song
                    RegisterMusicPlayer(Context.Guild.Id, new MusicPlayer(Context, (Context.User as IVoiceState).VoiceChannel, Volume, Song));
                else //Queue the song
                {
                    if(!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel)) //Ignore commands from users who are not in the same vc
                    {
                        await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                        return;
                    }
                    if(player.Queue.Count == QueueLimit())
                    {
                        switch (player.Queue.Count)
                        {
                            case 5:
                                await Context.Channel.SendEmbedAsync(lang["music_max_queue"]);
                                return;
                            case 10:
                                await Context.Channel.SendEmbedAsync(lang["music_max_voted_queue"]);
                                return;
                            case 20:
                                await Context.Channel.SendEmbedAsync(lang["music_max_premium_queue"]);
                                return;
                        }
                    }

                    //Queue song
                    player.Queue.Add(Song);

                    await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                        .WithDescription(lang["music_added_queue", "TITLE", Song.Title])
                        .WithThumbnailUrl(Song.ThumbnailUrl)
                        .AddField(lang["music_duration"], Song.FormatedDuration, true)
                        .AddField(lang["music_volume"], $"-", true)
                        .AddField(lang["music_requested"], $"{Context.User.ToString()}", true)
                        .AddField(lang["music_position"], $"{player.Queue.Count}/{QueueLimit()}", true)
                        .WithColor(Config.EmbedColor));
                }
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Play", null, ex), Context);
            }
        }

        [Command("pause")]
        [RequireContext(ContextType.Guild)]
        public async Task Pause()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null)
                    await Context.Channel.SendEmbedAsync(lang["music_no_music"]);
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    var pauseState = player.Pause();
                    await Context.Channel.SendEmbedAsync(pauseState ? lang["music_paused"] : lang["music_resumed"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Pause", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Pause", null, ex), Context);
            }
        }

        [Command("skip")]
        [RequireContext(ContextType.Guild)]
        public async Task Skip()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);

                if (player == null || player.Queue.Count == 0)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_queue_empty"]);
                }
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    int usercount = ((Context.User as IVoiceState).VoiceChannel as SocketVoiceChannel).Users.Count;
                    if (usercount > 2) //If there are more then 2 users in the voice channel, at least 50% has to agree to skip
                    {
                        if (usercount % 2 != 0) //"Rounding up"
                            usercount++;
                        usercount /= 2;

                        if (!(player.SkipVotes + 1 >= usercount)) //If the vote of the user is enought to skip, don't send the message
                        {
                            player.SkipVotes++;
                            await Context.Channel.SendEmbedAsync(lang["music_skip_status", "VOTES", player.SkipVotes, "NEEDEDVOTES", usercount.ToString()]);
                            return;
                        }
                    }
                    player.SkipVotes = 0; //Reset votes
                    player.Next(); //Play next song
                }

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Skip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Skip", null, ex), Context);
            }
        }

        [Command("forceskip")]
        [Alias("fskip")]
        [RequireContext(ContextType.Guild)]
        public async Task ForceSkip()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);

                if (player == null || player.Queue.Count == 0)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_queue_empty"]);
                }
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    player.Next(); //Play next song
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Skip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Skip", null, ex), Context);
            }
        }

        [Command("leave")]
        [RequireContext(ContextType.Guild)]
        public async Task Leave()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);

                if (player == null)
                    await Context.Channel.SendEmbedAsync(lang["music_not_in_voice"]);
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                else
                {
                    player.Leave();
                    await Context.Channel.SendEmbedAsync(lang["music_left_voice"]);
                }

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Leave", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Leave", null, ex), Context);
            }
        }

        [Command("listenmoe")]
        [Alias("lm")]
        [RequireContext(ContextType.Guild)]
        public async Task PlayListenMoe()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player != null)
                    await Context.Channel.SendEmbedAsync(lang["music_already_playing"]);
                else
                {
                    //Check if the user is in a voice channel
                    if ((Context.User as IVoiceState).VoiceChannel == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["music_user_not_in_voice"]);
                        return;
                    }

                    float Volume = (GuildConfigDatabase.GetOrFake(Context.Guild.Id).Volume) / 100F;
                    RegisterMusicPlayer(Context.Guild.Id, new MusicPlayer(Context, (Context.User as IVoiceState).VoiceChannel, Volume, new ListenMoeSongInfo(Context.User)));
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ListenMoe", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ListenMoe", null, ex), Context);
            }
        }

        [Command("nowplaying")]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null)
                    await Context.Channel.SendEmbedAsync(lang["music_no_music"]);
                else
                {
                    ISongInfo Song = player.Song;
                    await Context.Channel.SendEmbedAsync(lang["music_now_playing_desc", "TITLE", Song.Title, 
                                                              "REQUESTED",  Song.User.Username], Title: lang["music_now_playing"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "NowPlaying", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "NowPlaying", null, ex), Context);
            }
        }

        [Command("volume")]
        [RequireContext(ContextType.Guild)]
        public async Task Volume([Remainder, Optional]string Vol)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);

                //Send volume
                if (Vol == null)
                {
                    var g = GuildConfigDatabase.Get(Context.Guild.Id);
                    await Context.Channel.SendEmbedAsync(lang["music_volume_display", "VOLUME", g?.Volume ?? 100]);
                    return;
                }

                if (!float.TryParse(Vol, out float volume))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_invalid_vol"]);
                    return;
                }

                //Check if the volume is in the correct range
                if (volume > 200)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_volume_too_high"]);
                    return;
                }
                else if (volume < 1)
                {
                    await Context.Channel.SendEmbedAsync(lang["music_volume_too_low"]);
                    return;
                }

                //Check voice channel
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player != null && !player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                    return;
                }

                //Set volume in guild database
                var guild = GuildConfigDatabase.GetOrCreate(Context.Guild.Id);
                guild.Volume = volume;

                //Set player volume
                if (player != null)
                    player.Volume = volume / 100;

                //Pick emoji and send message
                string emoji = null;
                if (volume <= 30)
                    emoji = ":speaker:";
                else if (volume <= 65)
                    emoji = ":sound:";
                else
                    emoji = ":loud_sound:";
                await Context.Channel.SendEmbedAsync(emoji + lang["music_volume_set", "VOLUME", volume.ToString()]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Volume", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Volume", null, ex), Context);
            }
        }

        [Command("shuffle")]
        [RequireContext(ContextType.Guild)]
        public async Task Shuffle()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null)
                    await Context.Channel.SendEmbedAsync(lang["music_no_music"]);
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    if(player.Queue.Count < 3)
                    {
                        await Context.Channel.SendEmbedAsync(lang["music_queue_short"]);
                        return;
                    }

                    player.Random = !player.Random;
                    if (player.Random)
                        await Context.Channel.SendEmbedAsync(lang["music_shuffle_on"]);
                    else
                        await Context.Channel.SendEmbedAsync(lang["music_shuffle_off"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Shuffle", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Shuffle", null, ex), Context);
            }
        }

        [Command("repeat")]
        [RequireContext(ContextType.Guild)]
        public async Task Repeat()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null)
                    await Context.Channel.SendEmbedAsync(lang["music_no_music"]);
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    player.Repeat = !player.Repeat;
                    if (player.Repeat)
                        await Context.Channel.SendEmbedAsync(lang["music_repeat_on"]);
                    else
                        await Context.Channel.SendEmbedAsync(lang["music_repeat_off"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Repeat", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Repeat", null, ex), Context);
            }
        }

        [Command("listqueue")]
        [Alias("lq")]
        [RequireContext(ContextType.Guild)]
        public async Task ListQueue()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null || player.Queue.Count == 0)
                    await Context.Channel.SendEmbedAsync(lang["music_queue_empty"]);
                else
                {
                    string queueString = string.Empty;
                    for (int i = 0; i < player.Queue.Count; i++)
                    {
                        var c = player.Queue[i];
                        queueString += $"{i + 1}) **{c.Title}** [Link]({c.StreamUrl})\n";
                    }
                    await Context.Channel.SendEmbedAsync(queueString, lang["music_queue_list", "QCOUNT", player.Queue.Count, "QMAX", QueueLimit()]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ListQueue", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ListQueue", null, ex), Context);
            }
        }

        [Command("removequeue")]
        [Alias("rq")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveQueue([Remainder, Optional]string Input)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var player = GetMusicPlayer(Context.Guild.Id);
                if (player == null || player.Queue.Count == 0)
                    await Context.Channel.SendEmbedAsync(lang["music_queue_empty"]);
                else if (!player.VoiceMatch((Context.User as IVoiceState).VoiceChannel))
                {
                    await Context.Channel.SendEmbedAsync(lang["music_diff_vchannel"]);
                }
                else
                {
                    //Get the song (and the index of the song)
                    var song = player.GetSong(Input);
                    int i = 0;
                    if (song == null && (!int.TryParse(Input, out i) || i > player.Queue.Count || i < 1))
                    {
                        await Context.Channel.SendEmbedAsync(lang["music_not_in_queue"]);
                        return;
                    }
                    else if (song == null)
                    {
                        i--;
                        song = player.Queue[i];
                    }
                    else if (song != null)
                        i = player.Queue.IndexOf(song);

                    player.Dequeue(i);
                    await Context.Channel.SendEmbedAsync(lang["music_removed", "TITLE", song.Title]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RemoveQueue", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RemoveQueue", null, ex), Context);
            }
        }

        public static ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers = new ConcurrentDictionary<ulong, MusicPlayer>();
        private MusicPlayer GetMusicPlayer(ulong Guild)
        {
            if (MusicPlayers.ContainsKey(Guild))
                return MusicPlayers[Guild];
            return null;
        }
        private void RegisterMusicPlayer(ulong Guild, MusicPlayer mp) 
            => MusicPlayers.TryAdd(Guild, mp);

        private int QueueLimit()
        {
            if (KurumiData.Get().PremiumServers.Contains(Context.Guild.Id.ToString()))
                return DONATE_QUEUE;
            else if (DiscordBotlist.UserVoted(Context.Guild.OwnerId).Result)
                return VOTE_QUEUE;
            else
                return NORMAL_QUEUE;
        }
        private InputType GetInputType(string Url)
        {
            if (YoutubeService.VideoId(Url) != null)
                return InputType.Youtube;
            else if (SoundCloudService.SoundCloudUrl(Url))
                return InputType.Soundcloud;
            else
                return InputType.Keyword;
        }
        private enum InputType
        {
            Youtube,
            Soundcloud,
            Keyword
        }
        public class SongSelect
        {
            public ulong User { get; set; }
            public int Selected { get; set; }
        }
    }
}