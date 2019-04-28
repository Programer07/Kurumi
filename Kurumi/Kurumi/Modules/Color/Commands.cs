using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Attributes;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.ColorRoles
{
    public class Commands : ModuleBase
    {
        private static readonly List<(string Name, Color Color)> DefaultRoles = new List<(string Name, Color Color)>()
        {
            ("DEV Dark Red", Color.DarkRed),
            ("DEV Dark Blue", Color.DarkBlue),
            ("DEV Dark Green", Color.DarkGreen),
            ("DEV Red", Color.Red),
            ("DEV Purple", Color.Purple),
            ("DEV Green", Color.Green),
            ("DEV Blue", Color.Blue),
            ("DEV Yellow", Color.Gold)
        };


        [Command("createroles")]
        [RequireContext(ContextType.Guild)]
        public async Task CreateRoles()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Context.User.Id != Context.Guild.OwnerId)
                {
                    IGuildUser owner = Context.Guild.GetOwnerAsync().Result;
                    await Context.Channel.SendEmbedAsync(lang["color_not_owner", "OWNER", owner.Username]);
                    return;
                }

                var GuildColorRoles = GetRoles();
                if (GuildColorRoles.Count != DefaultRoles.Count)
                {
                    await Context.Channel.SendEmbedAsync(lang["color_creating"]);

                    var GuildConfig = GuildDatabase.GetOrCreate(Context.Guild.Id);
                    var GuildRoleNames = GuildColorRoles.ConvertAll(x => x.Name);
                    for (int i = 0; i < DefaultRoles.Count; i++)
                    {
                        (string Name, Discord.Color Color) DRole = DefaultRoles[i];
                        if (!GuildRoleNames.Contains(DRole.Name))
                        {
                            GuildConfig.ColorRoles.Add((await CreateRole(DRole.Name, DRole.Color)).Id);
                        }
                    }
                    await Context.Channel.SendEmbedAsync(lang["color_created_def"]);
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(lang["color_already_created"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "CreateRoles", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "CreateRoles", null, ex), Context);
            }
        }

        [Command("color")]
        [RequireContext(ContextType.Guild)]
        public async Task SelectRole([Remainder, Optional]string Role)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);

                List<IRole> ColorRoles = GetRoles();
                if(Role == null)
                {
                    if(ColorRoles.Count == 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["color_no_roles"]);
                        return;
                    }

                    string RoleString = string.Empty;
                    for (int i = 0; i < ColorRoles.Count; i++)
                    {
                        var Crole = ColorRoles[i];
                        RoleString += $"<@&{Crole.Id}> - {Crole.Name}\n";
                    }

                    await Context.Channel.SendEmbedAsync($"{RoleString}\n{lang["color_footer"]}", Title: lang["color_title"]);
                    return;
                }
                Role = Role.ToLower();
                List<string> ColorNames = ColorRoles.ConvertAll(x => x.Name.ToLower());
                if(Role != "clear" && !ColorNames.Contains(Role))
                {
                    await Context.Channel.SendEmbedAsync(lang["color_not_color"]);
                    return;
                }
                await ClearAllColor();
                if (Role != "clear")
                {
                    var r = GetRole(Role);
                    await (Context.User as IGuildUser).AddRoleAsync(r);
                    await Context.Channel.SendEmbedAsync(lang["color_success_select", "ROLE", r.Name]);
                }
                else
                    await Context.Channel.SendEmbedAsync(lang["color_success_clear", "USER", Context.User.Username]);

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Color", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Color", null, ex), Context);
            }
        }

        [Command("addcolorrole")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireGuildOwnerVote]
        public async Task AddRole([Optional]string colorString, [Remainder, Optional]string Role)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Role == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["color_invalid_name"]);
                    return;
                }
                uint ColorValue;
                try
                {
                    ColorValue = Convert.ToUInt32(colorString, 16);
                }
                catch (Exception)
                {
                    await Context.Channel.SendEmbedAsync(lang["color_invalid_color"]);
                    return;
                }
                Color color = new Color(ColorValue);
                IRole role = await CreateRole(Role, color);
                GuildDatabase.GetOrCreate(Context.Guild.Id).ColorRoles.Add(role.Id);
                await Context.Channel.SendEmbedAsync(lang["color_success_set", "ROLE", role.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "AddColorRole", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "AddColorRole", null, ex), Context);
            }
        }

        [Command("removecolorrole")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireGuildOwnerVote]
        public async Task RemoveRole([Remainder, Optional]string Role)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (Role == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["color_doesnt_exists"]);
                    return;
                }
                IRole role = GetRole(Role);
                if (role == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["color_doesnt_exists"]);
                    return;
                }
                List<ulong> CustomRoles = GuildDatabase.GetOrFake(Context.Guild.Id).ColorRoles;
                if (!CustomRoles.Contains(role.Id))
                {
                    await Context.Channel.SendEmbedAsync(lang["color_not_color"]);
                    return;
                }
                CustomRoles.Remove(role.Id);
                await Context.Channel.SendEmbedAsync(lang["color_success_remove", "ROLE", role.Name]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "RemoveColor", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RemoveColor", null, ex), Context);
            }
        }

        private List<IRole> GetRoles()
        {
            var Roles = new List<IRole>();
            var RoleIds = GuildDatabase.Get(Context.Guild.Id)?.ColorRoles;
            if (RoleIds == null)
                return Roles;
            for (int i = 0; i < RoleIds.Count; i++)
            {
                var Role = Context.Guild.GetRole(RoleIds[i]);
                if (Role != null)
                    Roles.Add(Role);
            }
            return Roles;
        }
        private async Task<IRole> CreateRole(string Name, Discord.Color color)
        {
            GuildPermissions permissions = new GuildPermissions();
            permissions = permissions.Modify(false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false);
            return await Context.Guild.CreateRoleAsync(Name, permissions, color);
        }
        private IRole GetRole(string Name)
        {
            foreach (var Role in Context.Guild.Roles)
            {
                if (Role.Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase))
                    return Role;
            }
            return null;
        }
        private async Task ClearAllColor()
        {
            var Roles = GetRoles();
            foreach (IRole role in Roles)
            {
                if ((Context.User as SocketGuildUser).Roles.Contains((role as SocketRole)))
                {
                    await (Context.User as IGuildUser).RemoveRoleAsync(role);
                }
            }
        }
    }
}