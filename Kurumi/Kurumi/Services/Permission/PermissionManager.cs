using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Kurumi.StartUp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Permission
{
    public class PermissionManager : ModuleBase
    {
        public static string[] PermissionlessCommands = new string[0]; //Is "permissionless" a word??
        public static bool HasPermission(ICommandContext Context, string command)
        {
            try
            {
                if (Config.Administrators.Contains(Context.User.Id) || 
                    Context.Guild == null || 
                    Context.User.Id == Context.Guild.OwnerId) //Guild owner and admins have all permissions and there no permissions in DMs
                    return true;
                
                var Roles = (Context.User as SocketGuildUser).Roles;
                var Aliases = GetAliases(command);
                for (int i = 0; i < Aliases.Count; i++)
                {
                    for (int j = 0; j < PermissionlessCommands.Length; j++) //Check if the command has permission
                    {
                        if (Aliases[i].Equals(PermissionlessCommands[j], StringComparison.CurrentCultureIgnoreCase))
                            return true; //Command doesn't have permission
                    }

                    foreach (var Role in Roles) //Check if the role has permission
                    {
                        if (RoleHasPermission(Context.Guild, Role, Aliases[i]))
                            return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log(new LogMessage(LogSeverity.Error, "PermissionManager", null, ex), Context);
            }
            return false;
        }
        private static bool RoleHasPermission(IGuild guild, SocketRole role, string Command)
        {
            var permissions = GuildDatabase.GetOrFake(guild.Id).Permissions;
            if (permissions.Count == 0) //No permissions set
                return true;

            for (int i = 0; i < permissions.Count; i++)
            {
                var p = permissions[i];
                if (p.RoleId == role.Id)
                {
                    if (p.PermissionList.Count == 0)
                        return false; //No permissions

                    for (int j = 0; j < p.PermissionList.Count; j++)
                    {
                        var c = p.PermissionList[j];
                        if (c.Equals(Command, StringComparison.CurrentCultureIgnoreCase))
                            return true; //Command allowed
                    }
                    return false; //Command not allowed
                }
            }
            return true; //No permissions set for this role
        }

        [Command("permission")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireContext(ContextType.Guild)]
        public async Task PermissionCommand([Optional]string op, [Optional]string role, [Optional]string permission)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (op == null || role == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["permission_help"]);
                    return;
                }
                op = op.ToLower();
                IRole Role = null;
                if (op != "copy") //Copy has a different format, Role1;Role2
                {
                    if (role.ToLower() == "everyone")
                        role = "@everyone";
                    //Try get role
                    Role = Utilities.GetRole(Context.Guild, role);
                    if (Role == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["permission_role_not_found"]);
                        return;
                    }
                }

                switch (op)
                {
                    case "remove":
                    case "add":
                        await AddOrRemove(Role, permission, op == "remove");
                        break;
                    case "list":
                        var guild = GuildDatabase.GetOrFake(Context.Guild.Id);
                        for (int i = 0; i < guild.Permissions.Count; i++)
                        {
                            var p = guild.Permissions[i];
                            if (p.RoleId == Role.Id)
                            {
                                if (p.PermissionList.Count == 0)
                                    await Context.Channel.SendEmbedAsync(lang["permission_role_no_permission"]);
                                else
                                    await Context.Channel.SendEmbedAsync(string.Join("\n", p.PermissionList), lang["permission_role_permissions", "ROLE", Role.Name]);
                                return;
                            }
                        }
                        await Context.Channel.SendEmbedAsync(lang["permission_role_all_permission"]);
                        break;
                    case "reset":
                        guild = GuildDatabase.Get(Context.Guild.Id);
                        if (guild != null)
                        {
                            var p = guild.Permissions.FirstOrDefault(x => x.RoleId == Role.Id);
                            if (p != null)
                                guild.Permissions.Remove(p);
                        }
                        await Context.Channel.SendEmbedAsync(lang["permission_reset", "ROLE", Role.Name]);
                        break;
                    case "copy":
                        //Check format
                        if (!role.Contains(";"))
                        {
                            await Context.Channel.SendEmbedAsync(lang["permission_role_not_correct"]);
                            return;
                        }
                        //Parse roles
                        string[] Roles = role.Split(";");
                        if (Roles[0]?.ToLower() == "everyone")
                            Roles[0] = "@everyone";
                        else if (Roles[1]?.ToLower() == "everyone")
                            Roles[1] = "@everyone";

                        //Get roles
                        IRole Role1 = Utilities.GetRole(Context.Guild, Roles[0]);
                        IRole Role2 = Utilities.GetRole(Context.Guild, Roles[1]);
                        if (Role1 == null)
                        {
                            await Context.Channel.SendEmbedAsync(lang["permission_role1_not_found"]);
                            return;
                        }
                        else if (Role2 == null)
                        {
                            await Context.Channel.SendEmbedAsync(lang["permission_role2_not_found"]);
                            return;
                        }

                        guild = GuildDatabase.GetOrFake(Context.Guild.Id);
                        RolePermissions role1;
                        RolePermissions role2 = guild.Permissions.FirstOrDefault(x => x.RoleId == Role2.Id);
                        if ((role1 = guild.Permissions.FirstOrDefault(x => x.RoleId == Role1.Id)) == null && role2 != null)
                            guild.Permissions.Remove(role2);
                        else
                            role2.PermissionList = new List<string>(role1.PermissionList);

                        await Context.Channel.SendEmbedAsync(lang["permission_copied", "ROLE1", Role1.Name, "ROLE2", Role2.Name]);
                        break;
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Permission", "success"), Context);
            }
            catch(Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Permission", null, ex), Context);
            }
        }
        public async Task AddOrRemove(IRole role, string permission, bool Remove)
        {
            var lang = Language.GetLanguage(Context.Guild);

            //Create a list of the permissions which will be added or removed
            List<string> PermissionChange = new List<string>();
            if (CommandData.CategoryExists(permission))
            {
                var c = CommandData.GetCommandsFor(permission);
                for (int i = 0; i < c.Count; i++)
                {
                    if (c[i].HasPermission)
                        PermissionChange.Add(c[i].CommandName.ToLower());
                }
            }
            else if (NameOfCommand(permission))
            {
                if (PermissionlessCommands.Any(x => x.Equals(permission, StringComparison.CurrentCultureIgnoreCase)))
                {
                    await Context.Channel.SendEmbedAsync(lang["permission_mod_permissionless"]);
                    return;
                }
                else
                    PermissionChange.Add(permission.ToLower());
            }
            else
            {
                await Context.Channel.SendEmbedAsync(lang["permission_not_found"]);
                return;
            }

            //Get the role permission object of the role
            var guild = GuildDatabase.GetOrCreate(Context.Guild.Id);
            RolePermissions Permissions = null;
            for (int i = 0; i < guild.Permissions.Count; i++)
            {
                var p = guild.Permissions[i];
                if (p.RoleId == role.Id)
                    Permissions = p;
            }

            if (Permissions == null) //Quick add/remove
            {
                if (Remove)
                {
                    await Context.Channel.SendEmbedAsync(lang["permission_role_doesnt_have_perm", "PERMISSION", permission.FirstCharToUpper()]);
                    return;
                }
                else
                {
                    guild.Permissions.Add(new RolePermissions() { PermissionList = PermissionChange, RoleId = role.Id });
                    await Context.Channel.SendEmbedAsync(lang["permission_added", "PERMISSION", permission.FirstCharToUpper(), "ROLE", role.Name]);
                    return;
                }
            }

            //Add/Remove permissions
            int Change = 0;
            for (int i = 0; i < Permissions.PermissionList.Count; i++)
            {
                var p = Permissions.PermissionList[i];
                if (Remove && PermissionChange.Contains(p.ToLower()))
                {
                    Permissions.PermissionList.Remove(p);
                    Change++;
                }
                else if (!Remove && !PermissionChange.Contains(p.ToLower()))
                {
                    Permissions.PermissionList.Add(p);
                    Change++;
                }
            }

            //Send message
            if (Change == 0)
                await Context.Channel.SendEmbedAsync(lang["permission_role_doesnt_have_perm", "PERMISSION", permission.FirstCharToUpper()]);
            else if (Remove)
                await Context.Channel.SendEmbedAsync(lang["permission_removed", "PERMISSION", permission.FirstCharToUpper(), "ROLE", role.Name]);
            else
                await Context.Channel.SendEmbedAsync(lang["permission_added", "PERMISSION", permission.FirstCharToUpper(), "ROLE", role.Name]);
        }

        public static bool CommandExists(string Command)
        {
            foreach (var cmd in Program.Bot.CommandHandler.Commands.Commands)
            {
                foreach (var alias in cmd.Aliases)
                {
                    if (string.Equals(Command, alias, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        public static IReadOnlyList<string> GetAliases(string Command)
        {
            foreach (var cmd in Program.Bot.CommandHandler.Commands.Commands)
            {
                foreach (var alias in cmd.Aliases)
                {
                    if (string.Equals(Command, alias, StringComparison.CurrentCultureIgnoreCase))
                        return cmd.Aliases;
                }
            }
            return null;
        }
        public static bool NameOfCommand(string Command)
        {
            foreach (var cmd in Program.Bot.CommandHandler.Commands.Commands)
            {
                if (cmd.Name.Equals(Command, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}