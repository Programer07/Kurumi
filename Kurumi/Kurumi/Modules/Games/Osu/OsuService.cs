using Kurumi.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games.Osu
{
    public class OsuService
    {
        private readonly HttpClient Http;
        private const string UserEndpoint = "https://osu.ppy.sh/api/get_user";

        public OsuService()
        {
            Http = new HttpClient();
        }


        public OsuPlayer GetPlayer(string Username, OsuGameMode Mode = OsuGameMode.Standard) => RequestUser(Username, Mode).Result;
        public OsuPlayer GetPlayer(int UserId, OsuGameMode Mode = OsuGameMode.Standard) => RequestUser(UserId, Mode).Result;
        public string GetUserAvatar(int UserId) => $"https://a.ppy.sh/{UserId}";
        public string GetUserAvatar(string UserId) => $"https://a.ppy.sh/{UserId}";



        private async Task<OsuPlayer> RequestUser(object Identifier, OsuGameMode Mode)
        {
            string type = Identifier is string ? "string" : "id";
            string ApiResponse = await Http.GetStringAsync($"{UserEndpoint}?k={Config.OsuApiKey}&u={Identifier.ToString()}&m={(byte)Mode}&type={type}");
            OsuPlayer[] PlayerArray = JsonConvert.DeserializeObject<OsuPlayer[]>(ApiResponse);
            if (PlayerArray.Length == 0)
                return null;
            else
                return PlayerArray[0];
        }

    }

    public class OsuPlayer
    {
        [JsonProperty("user_id")]
        public string Id { get; set; }
        [JsonProperty("username")]
        public string Name { get; set; }
        [JsonProperty("count300")]
        public string Count300 { get; set; }
        [JsonProperty("count100")]
        public string Count100 { get; set; }
        [JsonProperty("count50")]
        public string Count50 { get; set; }
        [JsonProperty("playcount")]
        public string PlayCount { get; set; }
        [JsonProperty("pp_rank")]
        public string GlobalRank { get; set; }
        [JsonProperty("total_score")]
        public string TotalScore { get; set; }
        [JsonProperty("level")]
        public string Level { get; set; }
        [JsonProperty("pp_raw")]
        public string PP { get; set; }
        [JsonProperty("accuracy")]
        public string Accuracy { get; set; }
        [JsonProperty("count_rank_ss")]
        public string SSCount { get; set; }
        [JsonProperty("count_rank_ssh")]
        public string SSPlusCount { get; set; }
        [JsonProperty("count_rank_s")]
        public string SCount { get; set; }
        [JsonProperty("count_rank_sh")]
        public string SPlusCount { get; set; }
        [JsonProperty("count_rank_a")]
        public string ACount { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("pp_country_rank")]
        public string CountryRank { get; set; }
    }
    public enum OsuGameMode : byte
    {
        Standard = 0,
        Taiko = 1,
        CatchTheBeat = 2,
        Mania = 3
    }
}