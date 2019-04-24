using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Search.AniList
{
    public class Commands : ModuleBase
    {
        [Command("anime")]
        public async Task Anime([Remainder]string title)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Config.AniListClientId == null || Config.AniListSecret == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["command_disabled"]);
                    return;
                }

                var ani = new AniListService().GetAnime(title);
                if (ani == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["anime_404"]);
                    return;
                }
                if (ani.NSFW && !((SocketTextChannel)Context.Channel).IsNsfw)
                {
                    await Context.Channel.SendEmbedAsync(lang["anime_adult", "ANIME", ani.Title]);
                    return;
                }
                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                                   .WithColor(Config.EmbedColor)
                                   .WithFooter(lang["manga/anime_powered"])
                                   .WithImageUrl(ani.Banner)
                                   .WithThumbnailUrl(ani.Image)
                                   .WithTitle(ani.EnglishTitle ?? ani.Title ?? "???")
                                   .AddField(lang["anime_title"], ani.Title ?? "???")
                                   .AddField(lang["anime_type"], ani.Type?.ToLower().FirstCharToUpper() ?? "???", true)
                                   .AddField(lang["manga/anime_status"], ani.Status?.ToLower().FirstCharToUpper() ?? "???", true)
                                   .AddField(lang["anime_title"], ani.Episodes ?? "???", true)
                                   .AddField(lang["manga/anime_score"], ani.Score == null ? "-" : ((double)ani.Score / 10).ToString(), true)
                                   .AddField(lang["manga/anime_genres"], ani.Genres == null ? "-" : ani.Genres)
                                   .AddField(lang["manga/anime_description"], string.IsNullOrEmpty(ani.Description) ? "-" : ani.Description.Length > 1024 ? ani.Description.Truncate(1021) + "..." : ani.Description));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Anime", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Anime", null, ex), Context);
            }
        }

        [Command("manga")]
        public async Task Manga([Remainder]string title)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                if (Config.AniListClientId == null || Config.AniListSecret == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["command_disabled"]);
                    return;
                }
                var manga = new AniListService().GetManga(title);
                if (manga == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["manga_404"]);
                    return;
                }
                if (manga.NSFW && !((SocketTextChannel)Context.Channel).IsNsfw)
                {
                    await Context.Channel.SendEmbedAsync(lang["manga_adult", "MANGA", manga.Title]);
                    return;
                }
                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithFooter(lang["manga/anime_powered"])
                    .WithTitle(manga.Title ?? manga.NativeTitle ?? "???")
                    .WithThumbnailUrl(manga.Image)
                    .AddField(lang["manga_native_title"], manga.NativeTitle ?? "???")
                    .AddField(lang["manga/anime_status"], manga.Status?.ToLower().FirstCharToUpper() ?? "???", true)
                    .AddField(lang["manga/anime_score"], manga.Score == null ? "-" : ((double)manga.Score / 10).ToString(), true)
                    .AddField(lang["manga_volumes"], manga.Volumes ?? "???", true)
                    .AddField(lang["manga_chapters"], manga.Chapters ?? "???", true)
                    .AddField(lang["manga/anime_genres"], manga.Genres == null ? "-" : manga.Genres)
                    .AddField(lang["manga/anime_description"], string.IsNullOrEmpty(manga.Description) ? "-" : manga.Description.Length > 1024 ? manga.Description.Truncate(1021) + "..." : manga.Description));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Manga", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Manga", null, ex), Context);
            }
        }

        [Command("character")]
        public async Task Character([Remainder]string name)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);
                if (Config.AniListClientId == null || Config.AniListSecret == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["command_disabled"]);
                    return;
                }

                var Char = new AniListService().GetCharacter(name);
                if (Char == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_404"]);
                    return;
                }
                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithFooter(lang["manga/anime_powered"])
                    .WithImageUrl(Char.Image)
                    .WithTitle(Char.Name ?? Char.NativeName)
                    .AddField(lang["character_native_name"], Char.NativeName ?? "-")
                    .AddField(lang["manga/anime_description"], string.IsNullOrEmpty(Char.Description) ? "-" : Char.Description.Length > 1024 ? Char.Description.Truncate(1021) + "..." : Char.Description));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Character", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Character", null, ex), Context);
            }
        }
    }
}