using Kurumi.Common.Extensions;
using Kurumi.Common.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Search.AniList
{
    public class AniListService
    {
        private readonly GraphQLClient Client;

        public AniListService()
        {
            Client = new GraphQLClient("https://graphql.anilist.co");
        }

        public Anime GetAnime(string Title)
        {
            var Response = Client.Get(AnimeQuery, new { TitleVar = Title }).Result;
            return Response.Get<Anime>("Media");
        }
        public Manga GetManga(string Title)
        {
            var Response = Client.Get(MangaQuery, new { TitleVar = Title }).Result;
            return Response.Get<Manga>("Media");
        }
        public Character GetCharacter(string Name)
        {
            var Response = Client.Get(CharacterQuery, new { NameVar = Name }).Result;
            return Response.Get<Character>("Character");
        }

        #region Queries
        private static readonly string AnimeQuery = @"
                   query ($TitleVar: String) {
                    Media(search: $TitleVar, type: ANIME) {
                        title {
                            romaji
                            english
                        }
                        type
                        episodes
                        averageScore
                        status
                        genres
                        coverImage {
                            extraLarge
                        }
                        bannerImage
                        description
                        isAdult
                    }
                   }";
        private static readonly string MangaQuery = @"
                   query ($TitleVar: String) {
                    Media(search: $TitleVar, format: MANGA) {
                        title {
                            romaji
                            native
                        }
                        chapters
                        volumes
                        status
                        averageScore
                        genres
                        coverImage {
                            extraLarge
                        }
                        description
                        isAdult
                    }
                   }";
        private static readonly string CharacterQuery = @"
                   query ($NameVar: String) {
                    Character(search: $NameVar) {
                        name {
                            first
                            last
                            native
                        }
                        description
                        image {
                            large
                        }
                    }
                   }";
        #endregion Queries

        #region ResponseClasses
        public class Anime
        {
            public Anime(JToken title, string type, string episodes, int? averageScore, string status, JArray genres, JToken coverImage, string bannerImage, string description, bool isAdult)
            {
                Title = title["romaji"].ToString();
                EnglishTitle = title["english"].ToString();
                Type = type;
                Episodes = episodes;
                Score = averageScore;
                Status = status;
                Genres = string.Join(", ", genres.ToObject<string[]>());
                Image = coverImage["extraLarge"].ToString();
                Banner = bannerImage;
                Description = description.FormatHTML();
                NSFW = isAdult;
            }

            public string Title { get; set; }
            public string EnglishTitle { get; set; }
            public string Type { get; set; }
            public string Episodes { get; set; }
            public int? Score { get; set; }
            public string Status { get; set; }
            public string Genres { get; set; }
            public string Image { get; set; }
            public string Banner { get; set; }
            public string Description { get; set; }
            public bool NSFW { get; set; }
        }
        public class Manga
        {
            public Manga(JToken title, string chapters, string volumes, string status, int? averageScore, JArray genres, JToken coverImage, string description, bool isAdult)
            {
                NativeTitle = title["native"].ToString();
                Title = title["romaji"].ToString();
                Chapters = chapters;
                Volumes = volumes;
                Status = status;
                Score = averageScore;
                Genres = string.Join(", ", genres.ToObject<string[]>());
                Image = coverImage["extraLarge"].ToString();
                Description = description;
                NSFW = isAdult;
            }

            public string NativeTitle { get; set; }
            public string Title { get; set; }
            public string Chapters { get; set; }
            public string Volumes { get; set; }
            public string Status { get; set; }
            public int? Score { get; set; }
            public string Genres { get; set; }
            public string Image { get; set; }
            public string Description { get; set; }
            public bool NSFW { get; set; }
        }
        public class Character
        {
            public Character(JToken name, string description, JToken image)
            {
                Name = name["first"] + " " + name["last"];
                NativeName = name["native"].ToString();
                Image = image["large"].ToString();
                Description = description;
            }

            public string Name { get; set; }
            public string NativeName { get; set; }
            public string Image { get; set; }
            public string Description { get; set; }
        }
        #endregion ResponseClasses
    }
}