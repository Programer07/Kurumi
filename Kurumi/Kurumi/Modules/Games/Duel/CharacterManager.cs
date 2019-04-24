using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Modules.Games.Duel.Database;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Random;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Kurumi.Modules.Games.Duel
{
    public class CharacterManager : ModuleBase
    {
        private const int PAGE_LENGTH = 7;
        private const int MAX_INVENTORY = 25;

        [Command("createcharacter")]
        public async Task Create([Optional, Remainder]string Name)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Name == null || !DirectoryExtensions.PathIsValid(KurumiPathConfig.CharacterDatabase + Name) || Name.EndsWith("."))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_bad_name"]);
                    return;
                }
                else if (CharacterDatabase.GetCharacter(Context.User.Id) != null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_already_has"]);
                    return;
                }
                else if (CharacterDatabase.GetCharacter(Name) != null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_already_exists"]);
                    return;
                }

                var c = new Character()
                {
                    Data = new CharacterData()
                    {
                        Ai = false,
                        Exp = 0,
                        Name = Name,
                        Owner = Context.User.Id,
                        ProfilePicture = null
                    },
                    Equipment = new CharacterEquipment()
                    {
                        A = 10,
                        Y = 9,
                        X = 8,
                        Inventory = new List<PlayerItem>(),
                        Weapon = CharacterDatabase.GetItem(1) ?? new Item(),
                        Boots = CharacterDatabase.GetItem(2) ?? new Item(),
                        Hat = CharacterDatabase.GetItem(3) ?? new Item(),
                        Shirt = CharacterDatabase.GetItem(4) ?? new Item(),
                        Coat = CharacterDatabase.GetItem(5) ?? new Item(),
                        Leggings = CharacterDatabase.GetItem(6) ?? new Item(),
                        Glove = CharacterDatabase.GetItem(7) ?? new Item(),
                        BaseValues = CharacterDatabase.GetItem(x => x.Name == "Default") ?? new Item()
                    }
                };
                c.Equipment.CharData = c.Data;
                CharacterDatabase.Characters.Add(c);
                await Context.Channel.SendEmbedAsync(lang["character_create_success", "NAME", Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "CreateCharacter", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "CreateCharacter", null, ex), Context);
            }
        }
        [Command("deletecharacter")]
        public async Task DeleteCharacter()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }
                await CharacterDatabase.DeleteCharacter(Character);
                await Context.Channel.SendEmbedAsync(lang["character_deleted", "NAME", Character.Data.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "DeleteCharacter", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "DeleteCharacter", null, ex), Context);
            }
        }
        [Command("characterinfo")]
        [Alias("ci")]
        public async Task CharacterInfo([Remainder, Optional]string user)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                IUser User;
                if (user == null)
                    User = Context.User;
                else
                    User = await Utilities.GetUser(Context.Guild, user);

                if (User == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_user_not_found"]);
                    return;
                }
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    if (Context.User.Id == User.Id)
                        await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    else
                        await Context.Channel.SendEmbedAsync(lang["character_user_no_character"]);
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .WithTitle(Character.Data.Name)
                    .AddField(lang["character_owner"], User.Username)
                    .AddField(lang["character_stats"], $"▸{lang["character_hp"]}: {Character.Equipment.TotalHP()}\n▸{lang["character_damage"]}: {Character.Equipment.TotalDamage()}\n" +
                                       $"▸{lang["character_resistance"]}: {Character.Equipment.TotalResistance()}\n▸{lang["character_resistance_pen"]}: {Character.Equipment.TotalResPenetration()}\n" +
                                       $"▸{lang["character_critical"]}: {Character.Equipment.TotalCritChance()}%\n▸{lang["character_critical_multiplier"]}: {Character.Equipment.TotalCritMultiplier()}x")
                    .AddField(lang["character_level"], $"▸{lang["character_exp"]}: {Character.Data.Exp}\n▸{lang["character_level"]}: {Character.Data.GetLevel()}");

                if (Character.Data.ProfilePicture != null)
                    embed.WithThumbnailUrl((Config.SSLEnabled ? "https://" : "http://") + "api.kurumibot.moe/characters/" + HttpUtility.UrlEncode(Character.Data.Name) + "/" + Character.Data.ProfilePicture);
                await Context.Channel.SendEmbedAsync(embed);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "CharacterInfo", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "CharacterInfo", null, ex), Context);
            }
        }
        [Command("inventory")]
        public async Task Inventory()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }

                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                    .WithColor(Config.EmbedColor)
                    .AddField(lang["character_equipment"], $"▸**{lang["character_weapon"]}** {Character.Equipment.Weapon?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_hat"]}** {Character.Equipment.Hat?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_coat"]}** {Character.Equipment.Coat?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_shirt"]}** {Character.Equipment.Shirt?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_glove"]}** {Character.Equipment.Glove?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_leggings"]}** {Character.Equipment.Leggings?.ToString(lang) ?? "-"}\n" +
                                                           $"▸**{lang["character_boots"]}** {Character.Equipment.Boots?.ToString(lang) ?? "-"}\n")
                    .AddField(lang["character_inv_title", "COUNT", Character.Equipment.Inventory.Count, "MAX", MAX_INVENTORY], $"```{Character.Equipment.InventoryToString(lang).Remove("**")}```"));
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Inventory", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Inventory", null, ex), Context);
            }
        }
        [Command("iteminfo")]
        public async Task ItemInfo([Remainder, Optional]string ItemName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var item = CharacterDatabase.GetItem(x => x.Name.Equals(ItemName, StringComparison.CurrentCultureIgnoreCase));
                if (item.Type == ItemType.Invalid)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_item"]);
                    return;
                }

                await Context.Channel.SendEmbedAsync(new EmbedBuilder()
                                                        .WithColor(Config.EmbedColor)
                                                        .WithTitle(item.Name)
                                                        .WithDescription($"▸**{lang["character_price"]}**{item.Price}¥\n" +
                                                                         $"▸**{lang["character_type"]}**{item.Type}\n\n" +
                                                                         $"▸**{lang["character_hp"]}:** {item.HP}\n" +
                                                                         $"▸**{lang["character_damage"]}:** {item.Damage}\n" +
                                                                         $"▸**{lang["character_resistance"]}:** {item.Resistance}\n" +
                                                                         $"▸**{lang["character_resistance_pen"]}:** {item.ResPenetration}\n" +
                                                                         $"▸**{lang["character_critical"]}:** {item.CritChance}%\n" +
                                                                         $"▸**{lang["character_critical_multiplier"]}:** {item.CritMultiplier}x"));

                await Utilities.Log(new LogMessage(LogSeverity.Info, "ItemInfo", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ItemInfo", null, ex), Context);
            }
        }
        [Command("shop")]
        public async Task Shop([Remainder, Optional]string Input)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                ItemType type = ItemType.Invalid;
                if (Input == null || !int.TryParse(Input, out int Page) || Page <= 0)
                {
                    Page = 1;
                    if (Input != null && !Enum.TryParse(Input, out type))
                    {
                        await Context.Channel.SendEmbedAsync(lang["character_shop_invalid"]);
                        return;
                    }
                }

                List<Item> PageItems = new List<Item>();
                List<Item> Items;
                if (type == ItemType.Invalid)
                    Items = CharacterDatabase.DefaultItems.Where(x => !x.Hidden);
                else
                    Items = CharacterDatabase.DefaultItems.Where(x => !x.Hidden && x.Type == type);

                for (int i = (Page - 1) * PAGE_LENGTH; i < Page * PAGE_LENGTH && i < Items.Count; i++)
                {
                    PageItems.Add(Items[i]);
                }
                
                if (PageItems.Count == 0)
                    await Context.Channel.SendEmbedAsync(lang["character_page_empty"]);
                else
                {
                    string ItemString = string.Empty;
                    foreach (Item i in PageItems)
                    {
                        ItemString += $"▸{i.Name}\n" +
                                      $"{lang["character_price"]}{i.Price}¥".Space(12).Space(3, false, false) +
                                      $"{lang["character_type"]} {i.Type}\n";
                    }
                    await Context.Channel.SendEmbedAsync(ItemString,
                        lang["character_shop_title", "CURRENT", Page.ToString(), "MAX", Math.Ceiling((double)Items.Count / PAGE_LENGTH).ToString()], lang["character_shop_footer"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Shop", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Shop", null, ex), Context);
            }
        }
        [Command("equip")]
        public async Task Equip([Remainder, Optional]string ItemName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }
                //Check if the name has a '(any number)' at the end
                int Index = 1;//^..^
                if (new Regex(@"^.+\s*[(]\d+[)]$").IsMatch(ItemName))
                {
                    int start = ItemName.IndexOf('(');
                    string n = ItemName.Substring(start + 1, ItemName.Length - start - 1).Remove(")");
                    Index = int.Parse(n);
                    ItemName = ItemName.Remove($"({Index})").TrimEnd();
                    if (Index < 1)
                        Index = 1;
                }
                //Get the default version of the item
                var item = CharacterDatabase.GetItem(x => x.Name.Equals(ItemName, StringComparison.CurrentCultureIgnoreCase));
                if (item.Type == ItemType.Invalid || item.Type == ItemType.Skill)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_item"]);
                    return;
                }
                //Check if the character has the item
                if (!Character.Equipment.HasItem(item))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_doesnt_have_item"]);
                    return;
                }

                //Get the new item from the inventory
                int i = Character.Equipment.Inventory.FindIndexN(x => x.Id == item.Id, Index);
                PlayerItem NewItem = Character.Equipment.Inventory[i];
                //Replace item
                PlayerItem OldItem = null;
                switch (item.Type)
                {
                    case ItemType.Weapon:
                        OldItem = Character.Equipment.Weapon;
                        Character.Equipment.Weapon = NewItem;
                        break;
                    case ItemType.Boots:
                        OldItem = Character.Equipment.Boots;
                        Character.Equipment.Boots = NewItem;
                        break;
                    case ItemType.Hat:
                        OldItem = Character.Equipment.Hat;
                        Character.Equipment.Hat = NewItem;
                        break;
                    case ItemType.Shirt:
                        OldItem = Character.Equipment.Shirt;
                        Character.Equipment.Shirt = NewItem;
                        break;
                    case ItemType.Coat:
                        OldItem = Character.Equipment.Coat;
                        Character.Equipment.Coat = NewItem;
                        break;
                    case ItemType.Glove:
                        OldItem = Character.Equipment.Glove;
                        Character.Equipment.Glove = NewItem;
                        break;
                    case ItemType.Leggings:
                        OldItem = Character.Equipment.Leggings;
                        Character.Equipment.Leggings = NewItem;
                        break;
                }
                //Remove new and add old to the inventory
                Character.Equipment.Inventory.Remove(NewItem);
                Character.Equipment.Inventory.Add(OldItem);

                //Calculate changes
                string Changes = string.Empty;
                if (OldItem.HP - NewItem.HP != 0)
                    Changes += $"**{lang["character_hp"]}:** {FancyDeltaOf(OldItem.HP, NewItem.HP)}\n";
                if (OldItem.Damage - NewItem.Damage != 0)
                    Changes += $"**{lang["character_damage"]}:** {FancyDeltaOf(OldItem.Damage, NewItem.Damage)}\n";
                if (OldItem.Resistance - NewItem.Resistance != 0)
                    Changes += $"**{lang["character_resistance"]}:** {FancyDeltaOf(OldItem.Resistance, NewItem.Resistance)}\n";
                if (OldItem.ResPenetration - NewItem.ResPenetration != 0)
                    Changes += $"**{lang["character_resistance_pen"]}:** {FancyDeltaOf(OldItem.ResPenetration, NewItem.ResPenetration)}\n";
                if (OldItem.CritChance - NewItem.CritChance != 0)
                    Changes += $"**{lang["character_critical"]}:** {FancyDeltaOf(OldItem.CritChance, NewItem.CritChance)}\n";
                if (OldItem.CritMultiplier - NewItem.CritMultiplier != 0)
                    Changes += $"**{lang["character_critical_multiplier"]}:** {FancyDeltaOf(OldItem.CritMultiplier, NewItem.CritMultiplier)}\n";
                await Context.Channel.SendEmbedAsync(lang["character_equipped", "ITEM", NewItem.Name, "SLOT", NewItem.Type.ToString(), "CHANGES", Changes]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Equip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Equip", null, ex), Context);
            }
        }
        [Command("setprofilepicture")]
        public async Task ProfilePicture(string url)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (!ValidImageUrl(url))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_bad_url"]);
                    return;
                }
                else if (SizeOfImage(url) > 2097152)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_too_large"]);
                    return;
                }
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }

                await Context.Message.DeleteAsync();

                var rng = new KurumiRandom();
            Get: //The name of the file has to be changed because discord caches images
                int Name = rng.Next(10000, 99999);
                if (Name.ToString() + ".png" == Character.Data.ProfilePicture)
                    goto Get;

                WebClient client = new WebClient();
                Stream stream = client.OpenRead(url);
                Bitmap bitmap = new Bitmap(stream);

                bitmap.Save($"{KurumiPathConfig.CharacterDatabase}{Character.Data.Name}{KurumiPathConfig.Separator}{Name}.png", System.Drawing.Imaging.ImageFormat.Png);
                bitmap.Dispose();
                try
                {
                    stream.Flush();
                }
                catch (Exception) { }
                try
                {
                    stream.Close();
                }
                catch (Exception) { }
                try
                {
                    client.Dispose();
                }
                catch (Exception) { }

                string p = $"{KurumiPathConfig.CharacterDatabase}{Character.Data.Name}{KurumiPathConfig.Separator}{Character.Data.ProfilePicture}";
                if (File.Exists(p))
                    File.Delete(p);

                Character.Data.ProfilePicture = Name + ".png";
                await Context.Channel.SendEmbedAsync(lang["character_profilepicture_changed"]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "ProfilePicture", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "ProfilePicture", null, ex), Context);
            }
        }
        [Command("buy")]
        public async Task Buy([Remainder, Optional]string ItemName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var item = CharacterDatabase.GetItem(x => x.Name.Equals(ItemName, StringComparison.CurrentCultureIgnoreCase));
                if (item.Type == ItemType.Invalid || item.Hidden)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_item"]);
                    return;
                }
                var User = GlobalUserDatabase.GetOrCreate(Context.User.Id);
                if (User.Credit < item.Price)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_money"]);
                    return;
                }
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }
                else if (Character.Equipment.Inventory.Count == MAX_INVENTORY)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_inv_full"]);
                    return;
                }
                Character.Equipment.Inventory.Add(item);
                User.Credit -= item.Price;
                await Context.Channel.SendEmbedAsync(lang["character_bought", "ITEM", item.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Buy", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Buy", null, ex), Context);
            }
        }
        [Command("sell")]
        public async Task Sell([Remainder, Optional]string ItemName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }
                //Check if the name has a '(any number)' at the end
                int Index = 1;//^..^
                if (new Regex(@"^.+\s*[(]\d+[)]$").IsMatch(ItemName))
                {
                    int start = ItemName.IndexOf('(');
                    string n = ItemName.Substring(start + 1, ItemName.Length - start - 1).Remove(")");
                    Index = int.Parse(n);
                    ItemName = ItemName.Remove($"({Index})").TrimEnd();
                    if (Index < 1)
                        Index = 1;
                }
                //Get default item
                var item = CharacterDatabase.GetItem(x => x.Name.Equals(ItemName, StringComparison.CurrentCultureIgnoreCase));
                if (item.Type == ItemType.Invalid)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_item"]);
                    return;
                }
                //Check if the character has the item
                if (!Character.Equipment.HasItem(item))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_doesnt_have_item"]);
                    return;
                }
                //Get user
                var user = GlobalUserDatabase.GetOrCreate(Context.User.Id);
                //Add 90% of the price
                user.Credit += (uint)Math.Ceiling(item.Price * 0.9);
                //Get the item from the inventory and remove it
                int i = Character.Equipment.Inventory.FindIndexN(x => x.Id == item.Id, Index);
                Character.Equipment.Inventory.RemoveAt(i);
                await Context.Channel.SendEmbedAsync(lang["character_sold", "ITEM", item.Name, "PRICE", (uint)Math.Ceiling(item.Price * 0.9)]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Sell", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Sell", null, ex), Context);
            }
        }
        [Command("skill")]
        public async Task Skill([Optional]string Slot, [Remainder, Optional]string SkillName)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                var Character = CharacterDatabase.GetCharacter(Context.User.Id);
                if (Character == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_no_character"]);
                    return;
                }
                var item = CharacterDatabase.GetItem(x => x.Name.Equals(SkillName, StringComparison.CurrentCultureIgnoreCase));
                if (item.Type != ItemType.Skill)
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_item"]);
                    return;
                }
                else if (!Character.Equipment.HasItem(item))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_doesnt_have_item"]);
                    return;
                }
                if (Slot == null || !((Slot = Slot.ToLower()) == "x" || Slot == "y" || Slot == "a"))
                {
                    await Context.Channel.SendEmbedAsync(lang["character_invalid_slot"]);
                    return;
                }
                switch (Slot)
                {
                    case "x":
                        Character.Equipment.Inventory.Add(CharacterDatabase.GetItem(Character.Equipment.X));
                        Character.Equipment.X = item.Id;
                        break;
                    case "y":
                        Character.Equipment.Inventory.Add(CharacterDatabase.GetItem(Character.Equipment.Y));
                        Character.Equipment.Y = item.Id;
                        break;
                    case "a":
                        Character.Equipment.Inventory.Add(CharacterDatabase.GetItem(Character.Equipment.A));
                        Character.Equipment.A = item.Id;
                        break;
                }
                int i = Character.Equipment.Inventory.FindIndexN(x => x.Id == item.Id, 1);
                Character.Equipment.Inventory.Remove(Character.Equipment.Inventory[i]);
                await Context.Channel.SendEmbedAsync(lang["character_skill_equipped", "ITEM", item.Name, "SLOT", Slot.ToUpper()]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Skill", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Skill", null, ex), Context);
            }
        }

        private string FancyDeltaOf(int a, int b)
        {
            int Delta = b - a;
            string Fancy = " :small_red_triangle:";
            if (Delta < 0)
                Fancy = " :small_red_triangle_down:";
            else if (Delta == 0)
                Fancy = string.Empty;
            Fancy = Delta.ToString() + Fancy;
            return Fancy;
        }
        private bool ValidImageUrl(string Url)
        {
            if (Url == null || !ValidUrl(Url))
                return false;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "HEAD";
            try
            {
                request.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private long SizeOfImage(string Url)
        {
            try
            {
                WebRequest req = WebRequest.Create(Url);
                req.Method = "HEAD";
                using (WebResponse resp = req.GetResponse())
                {
                    if (long.TryParse(resp.Headers.Get("Content-Length"), out long ContentLength))
                        return ContentLength;
                    else
                        return int.MaxValue;
                }
            }
            catch (Exception)
            {
                return int.MaxValue;
            }
        }
        private bool ValidUrl(string Url)
        {
            if (Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
            {
                Uri l_strUri = new Uri(Url);
                return (l_strUri.Scheme == Uri.UriSchemeHttp || l_strUri.Scheme == Uri.UriSchemeHttps);
            }
            else
                return false;
        }
    }
}