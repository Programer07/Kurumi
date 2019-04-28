using Discord;
using Kurumi.Modules;
using Kurumi.Services;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Kurumi.Common
{
    public class Language
    {
        public static Dictionary<string, (LanguageData Data, LanguageDictionary Lang)> Languages = new Dictionary<string, (LanguageData, LanguageDictionary)>();

        public static LanguageDictionary GetLanguage(IGuild guild)
        {
            if (guild == null) //DMs
                return new LanguageDictionary(Languages[Config.DefaultLanguage].Lang);

            //Guilds
            LanguageDictionary language;
            string lang = GuildDatabase.GetOrFake(guild.Id).Lang;
            if (Languages.ContainsKey(lang))
                language = new LanguageDictionary(Languages[lang].Lang);
            else
                language = new LanguageDictionary(Languages[Config.DefaultLanguage].Lang);
            language.Guild = guild;
            return language;
        }

        public static bool LoadLanguages(out Exception ex)
        {
            try
            {
                Directory.CreateDirectory($"{KurumiPathConfig.Settings}Lang");
                string[] LangFiles = Directory.GetFiles($"{KurumiPathConfig.Settings}Lang", "*.json");
                if (LangFiles.Length == 0)
                    throw new Exception("No language found!");

                foreach (string file in LangFiles)
                {
                    //Load
                    string LanguageFile = File.ReadAllText(file);
                    string DataString = LanguageFile.Split("</languageData>")[0].Replace("<languageData>", "");
                    string LangString = LanguageFile.Split("</languageData>")[1];
                    //Parse
                    LanguageData Data = JsonConvert.DeserializeObject<LanguageData>(DataString);
                    Dictionary<string, string> l = JsonConvert.DeserializeObject<Dictionary<string, string>>(LangString);
                    LanguageDictionary ld = new LanguageDictionary(l, Data.Code);
                    //Reload
                    if (Languages.ContainsKey(Data.Code))
                        Languages.Remove(Data.Code);
                    //Add
                    Languages.Add(Data.Code, (Data, ld));

                }
                ex = null;
                return true;
            }
            catch (Exception exception)
            {
                ex = exception;
                return false;
            }
        }

        public static string GetLanguagCode(string lang)
        {
            foreach (var (Data, Lang) in Languages.Values)
            {
                if(Data.Code.Equals(lang, StringComparison.CurrentCultureIgnoreCase) || Data.DisplayName.Equals(lang, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Data.Code;
                }
            }
            return null;
        }
    }

    public class LanguageDictionary
    {
        private readonly Hashtable lang = new Hashtable();
        public IGuild Guild { get; set; }
        public string Code { get; private set; }

        public LanguageDictionary(LanguageDictionary d)
        {
            lang = new Hashtable(d.lang);
            Code = d.Code;
        }
        public LanguageDictionary(Dictionary<string, string> d, string code)
        {
            this.lang = new Hashtable(d);
            this.Code = code;
        }

        public string this[string key, [Optional]object var0, [Optional]object val0,
                                      [Optional]object var1, [Optional]object val1,
                                      [Optional]object var2, [Optional]object val2,
                                      [Optional]object var3, [Optional]object val3]
        {
            get
            {
                if (lang.ContainsKey(key))
                {
                    string text = (lang[key] as string).Replace($"@{var0?.ToString()}@", val0?.ToString())
                                                .Replace($"@{var1?.ToString()}@", val1?.ToString())
                                                .Replace($"@{var2?.ToString()}@", val2?.ToString())
                                                .Replace($"@{var3?.ToString()}@", val3?.ToString());
                    if (Guild != null)
                        text = text.Replace("@PREFIX@", GuildDatabase.GetOrFake(Guild.Id).Prefix);
                    else
                        text = text.Replace("@PREFIX@", CommandHandler.DEFAULT_PREFIX);
                    return text;
                }
                return key;
            }
        }
    }
    public class LanguageData
    {
        public string DisplayName { get; set; }
        public string CreatedBy { get; set; }
        public string Version { get; set; }
        public string Code { get; set; }
    }
}