using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Random;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Kurumi.Modules.Image
{
    public class NSFW : ModuleBase
    {
        private HttpClient Client { get; set; }
        private static readonly ConcurrentDictionary<ulong, List<string>> History = new ConcurrentDictionary<ulong, List<string>>();

        [Command("rule34")]
        [RequireContext(ContextType.Guild)]
        public async Task Rule34([Remainder, Optional]string tags) => await SendImage(HentaiType.Rule34, tags);
        [Command("danbooru")]
        [RequireContext(ContextType.Guild)]
        public async Task Danbooru([Remainder, Optional]string tags) => await SendImage(HentaiType.Danbooru, tags);
        [Command("e621")]
        [RequireContext(ContextType.Guild)]
        public async Task E621([Remainder, Optional]string tags) => await SendImage(HentaiType.E621, tags);
        [Command("gelbooru")]
        [RequireContext(ContextType.Guild)]
        public async Task Gelbooru([Remainder, Optional]string tags) => await SendImage(HentaiType.Gelbooru, tags);
        [Command("konachan")]
        [RequireContext(ContextType.Guild)]
        public async Task Konachan([Remainder, Optional]string tags) => await SendImage(HentaiType.Konachan, tags);
        [Command("yandere")]
        [RequireContext(ContextType.Guild)]
        public async Task Yandere([Remainder, Optional]string tags) => await SendImage(HentaiType.Yandere, tags);
        [Command("randomhentai")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomHentai([Remainder, Optional]string tags)
        {
            List<int> Checked = new List<int>();
            int Limit = Enum.GetNames(typeof(HentaiType)).Length;

        New:
            if(Checked.Count == Limit)
            {
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.TrySendEmbedAsync(lang["NSFW_no_pictures"]);
                return;
            }
            int type = new Random().Next(0, Limit);
        Get:
            if (Checked.Contains(type))
            {
                type++;
                if (type == Limit)
                    type = 0;
                goto Get;
            }
            else
                Checked.Add(type);

            if (await GetLink((HentaiType)type, tags, false) == null)
                goto New;

            await SendImage((HentaiType)type, tags);
        }
        [Command("clearhistory")]
        [RequireContext(ContextType.Guild)]
        public async Task Clearhistory()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (History.ContainsKey(Context.Guild.Id))
                {
                    History.TryRemove(Context.Guild.Id, out List<string> history);
                    await Context.Channel.SendEmbedAsync(lang["NSFW_history_cleared", "COUNT", history.Count]);
                }
                else
                    await Context.Channel.SendEmbedAsync(lang["NSFW_history_empty"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ClearHistory", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ClearHistory", null, ex), Context);
            }
        }



        private async Task SendImage(HentaiType type, string tags)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if(!(Context.Channel as ITextChannel).IsNsfw)
                {
                    await Context.Channel.SendEmbedAsync(lang["NSFW_outside_NSFW", "COMMAND", $"{type}"]);
                    return;
                }
                string link = await GetLink(type, tags);
                if(link == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["NSFW_no_pictures"]);
                    return;
                }
                await Context.Channel.SendEmbedAsync($"[{lang["NSFW_open_in_browser"] }]({link})", ImageUrl: link);
                await Utilities.Log(new LogMessage(LogSeverity.Info, $"{type}", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, $"{type}", null, ex), Context);
            }
        }

        private async Task<string> GetLink(HentaiType type, string tags, bool UpdateHistory = true)
        {
            if(!string.IsNullOrWhiteSpace(tags))
                tags.Replace(" ", "%20");

            Client = new HttpClient();
            string API = "";
            switch (type)
            {
                case HentaiType.E621:
                    API = $"https://e621.net/post/index.json?limit=1000&tags={tags}";
                    break;
                case HentaiType.Gelbooru:
                    API = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=1000&tags={tags}";
                    break;
                case HentaiType.Konachan:
                    API = $"https://konachan.com/post.json?s=post&q=index&limit=1000&tags={tags}";
                    break;
                case HentaiType.Rule34:
                    API = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=1000&tags={tags}";
                    break;
                case HentaiType.Danbooru:
                    API = $"http://danbooru.donmai.us/posts.json?limit=1000&tags={tags}";
                    break;
                case HentaiType.Yandere:
                    API = $"https://yande.re/post.json?limit=1000&tags={tags}";
                    break;
                /*case HentaiType.ibsearch:
                        API = $"https://ibsearch.xxx/api/v1/images.json?q={tags}&limit=1000&key={KurumiIbSearchAPI}";
                    break;*/
            }

            //Get images
            List<Hentai> Response;
            if (type == HentaiType.E621 || type == HentaiType.Konachan || type == HentaiType.Danbooru || type == HentaiType.Yandere)
            {
                string res = await Client.AddFakeHeaders().GetStringAsync(API);
                Response = JsonConvert.DeserializeObject<List<Hentai>>(res);
            }
            else
            {
                Response = new List<Hentai>();
                Stream s = await Client.GetStreamAsync(API);
                var reader = XmlReader.Create(s, new XmlReaderSettings() { Async = true });
                while(await reader.ReadAsync())
                {
                    if (reader.Name == "post" && reader.NodeType == XmlNodeType.Element)
                    {
                        Response.Add(new Hentai()
                        {
                            File_Url = reader["file_url"],
                            Tags = reader["tags"],
                            Rating = reader["rating"]
                        });
                    }
                }
            }

            if (Response.Count == 0) //No images found
                return null;

            //Get server history
            List<string> ServerHistory;
            if (History.ContainsKey(Context.Guild.Id))
                ServerHistory = History[Context.Guild.Id];
            else
                ServerHistory = new List<string>();

            //Filter safe, blacklisted images and links which are already in the server history.
            List<Hentai> ValidImages = new List<Hentai>();
            for (int i = 0; i < Response.Count; i++)
            {
                var h = Response[i];
                if (h.Rating != "s" && !ServerHistory.Contains(h.File_Url) && !BlackListed(h.Tags) && !BlackListed(h.Tag_String))
                    ValidImages.Add(h);
            }

            if (ValidImages.Count == 0) //No valid images found
                return null;

            //Pick a random image
            int Index = new KurumiRandom().Next(0, ValidImages.Count);
            var pick = ValidImages[Index];

            //Add to history
            if (UpdateHistory)
            {
                ServerHistory.Add(pick.File_Url);
                if (ServerHistory.Count > 100) //Reset history if there are more then 100 images in it
                {
                    History.TryRemove(Context.Guild.Id, out _);
                    History.TryAdd(Context.Guild.Id, new List<string>());
                }
                else if (!History.ContainsKey(Context.Guild.Id)) //Add to history if new
                    History.TryAdd(Context.Guild.Id, ServerHistory);
            }

            return pick.File_Url;
        }

        private bool BlackListed(string Tags)
        {
            if (Tags == null)
                return false;

            for (int i = 0; i < BlackList.Length; i++)
            {
                if (Tags.Contains(BlackList[i], StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private static readonly string[] BlackList =
        {
            "loli",
            "shota",
            "child",
            "lolicon",
            "shotacon",
        };

        public class Hentai
        {
            public string File_Url { get; set; }
            public string Tags { get; set; }
            public string Tag_String { get; set; }
            public string Rating { get; set; }
        }

        public enum HentaiType
        {
            E621,
            Gelbooru,
            Konachan,
            Rule34,
            Danbooru,
            Yandere
        }
    }
}