using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Search
{
    public class UrbanDictionary : ModuleBase
    {
        [Command("urbandictionary")]
        [Alias("ud", "udict")]
        public async Task Urbandictionary([Optional, Remainder]string word)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (word == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["urban_not_found"]);
                    return;
                }

                //Get data
                UDictDefinition[] definitions = await GetDefinitions(word);
                List<string> Definitions = new List<string>();
                for (int i = 0; i < (definitions?.Length ?? 0) && i < 3; i++)
                {
                    string def = definitions[i].Definition.Remove("]").Remove("[");
                    string author = lang["urban_author", "AUTHOR", definitions[i].Author, "LIKES", definitions[i].ThumbsUp.ToString(), "DISLIKES", definitions[i].ThumbsDown.ToString()];
                    if (def.Length + author.Length > 1024)
                        def = def.Truncate(1021 - author.Length) + "...";
                    Definitions.Add(def + author);
                }
                //Not found
                if (Definitions.Count == 0)
                {
                    await Context.Channel.SendEmbedAsync(lang["urban_not_found"]);
                    return;
                }
                //Send
                var Embed = new EmbedBuilder()
                    .WithColor(Config.EmbedColor);
                for (int i = 0; i < Definitions.Count; i++)
                {
                    Embed.AddField($"#{i + 1}", Definitions[i]);
                }
                await ReplyAsync("", embed: Embed.Build());
                await Utilities.Log(new LogMessage(LogSeverity.Info, "UrbanDictionary", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "UrbanDictionary", null, ex), Context);
            }
        }

        private async Task<UDictDefinition[]> GetDefinitions(string Word)
        {
            string word = WebUtility.UrlEncode(Word);
            string Result = await new HttpClient().GetStringAsync($"http://api.urbandictionary.com/v0/define?term={word}").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Dictionary<string, UDictDefinition[]>>(Result)["list"];
        }

        public class UDictDefinition
        {
            [JsonProperty("definition")]
            public string Definition { get; set; }
            [JsonProperty("thumbs_up")]
            public int ThumbsUp { get; set; }
            [JsonProperty("author")]
            public string Author { get; set; }
            [JsonProperty("thumbs_down")]
            public int ThumbsDown { get; set; }
        }
    }
}