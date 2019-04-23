using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.StartUp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Permission
{
    public class PermissionManager : ModuleBase
    {
        private static readonly ConcurrentDictionary<(ulong, ulong), List<(string, bool)>> PermissionCache = new ConcurrentDictionary<(ulong, ulong), List<(string, bool)>>();
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
                        if (Role.Name == "@everyone")
                            continue;
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
            if(PermissionCache.ContainsKey((role.Id, guild.Id))) //Try to get from cache
            {
                var List = PermissionCache[(role.Id, guild.Id)];
                for (int i = 0; i < List.Count; i++)
                {
                    var command = List[i];
                    if (command.Item1.Equals(Command, StringComparison.CurrentCultureIgnoreCase))
                        return command.Item2;
                }
            }

            bool Result = false;
            string path = $"{KurumiPathConfig.GuildDatabase}{guild.Id}{KurumiPathConfig.Separator}Permissions{KurumiPathConfig.Separator}{role.Id}.perm";
            if (File.Exists(path))
            {
                var Permissions = File.ReadAllLines(path);
                for (int i = 0; i < Permissions.Length; i++)
                {
                    if (Permissions[i].Equals(Command, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Result = true;
                        break;
                    }
                }
            }
            else
                Result = true;

            if(PermissionCache.ContainsKey((role.Id, guild.Id))) //Get the list and add the command
                PermissionCache[(role.Id, guild.Id)].Add((Command, Result));
            else //Create a list
            {
                List<(string, bool)> commandList = new List<(string, bool)>()
                {
                    (Command, Result),
                };
                PermissionCache.TryAdd((role.Id, guild.Id), commandList);
            }

            return Result;
        }


        [Command("permission")]
        [RequireContext(ContextType.Guild)]
        public async Task PermissionCommand([Optional]string op, [Optional]string role, [Optional]string permission)
        {
            var lang = Language.GetLanguage(Context.Guild);
            if (op == null || role == null)
            {
                await Context.Channel.SendEmbedAsync(lang["permission_help"]);
                return;
            }
            if (op == "copy") //Copy's syntax is different, to make the code look better I moved it to a different function
            {
                await CopyPermissions(role, lang);
                return;
            }
            if (role?.ToLower() == "everyone")
                role = "@everyone";

            //Try get role
            IRole Role = Context.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == role ||
                                                            x.Mention.Equals(role, StringComparison.CurrentCultureIgnoreCase) ||
                                                            x.Name.Equals(role, StringComparison.CurrentCultureIgnoreCase));
            if (Role == null)
            {
                await Context.Channel.SendEmbedAsync(lang["permission_role_not_found"]);
                return;
            }

            if (op != "list" && op != "reset" && !NameOfCommand(permission)) //List and reset doesn't need permission
            {
                await Context.Channel.SendEmbedAsync(lang["permission_not_found"]);
                return;
            }

            //Load permissions
            List<string> Permissions = new List<string>();
            string Path = $"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Permissions{KurumiPathConfig.Separator}{Role.Id}.perm";
            if (File.Exists(Path))
                Permissions.AddRange(File.ReadAllLines(Path));

            switch (op)
            {
                case "remove":
                case "add":
                    bool remove = op == "remove";
                    if (permission == null) //Permission is empty
                    {
                        await Context.Channel.SendEmbedAsync(lang["permission_empty"]);
                        return;
                    }

                    for (int i = 0; i < PermissionlessCommands.Length; i++) //Adding a command which has no permission
                    {
                        if (PermissionlessCommands[i].Equals(permission.ToLower(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            await Context.Channel.SendEmbedAsync(lang["permission_mod_permissionless"]);
                            return;
                        }
                    }

                    int c = Permissions.Count;
                    if (CommandData.CategoryExists(permission)) //Add/remove commands from category
                    {
                        List<Command> Commands = CommandData.GetCommandsFor(permission);
                        for (int i = 0; i < Commands.Count; i++)
                        {
                            var command = Commands[i];
                            if (command.HasPermission)
                            {
                                string name = command.CommandName.ToLower();
                                if (remove && Permissions.Contains(name))
                                {
                                    Permissions.Remove(name);
                                }
                                else if (!Permissions.Contains(name))
                                {
                                    Permissions.Add(name);
                                }
                            }
                        }
                    }
                    else //Add/remove permission
                    {
                        if (remove)
                        {
                            if (Permissions.Contains(permission.ToLower()))
                                Permissions.Remove(permission.ToLower());
                        }
                        else
                            Permissions.Add(permission.ToLower());
                    }

                    //Write permissions
                    Directory.CreateDirectory($"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Permissions");
                    File.WriteAllText(Path, string.Join("\n", Permissions));

                    //Remove cache
                    PermissionCache.TryRemove((Role.Id, Context.Guild.Id), out _);

                    //Send message
                    if (remove)
                    {
                        if (c == Permissions.Count) //No change
                            await Context.Channel.SendEmbedAsync(lang["permission_role_doesnt_have_perm", "PERMISSION", permission.FirstCharToUpper()]);
                        else
                            await Context.Channel.SendEmbedAsync(lang["permission_removed", "PERMISSION", permission.FirstCharToUpper(), "ROLE", Role.Name]);
                    }
                    else
                        await Context.Channel.SendEmbedAsync(lang["permission_added", "PERMISSION", permission.FirstCharToUpper(), "ROLE", Role.Name]);
                    break;
                case "list":
                    //Check if there are permissions
                    if (Permissions.Count > 0)
                        await Context.Channel.SendEmbedAsync(string.Join("\n", Permissions), lang["permission_role_permissions", "ROLE", Role.Name]);
                    else //No permissions
                    {
                        if (File.Exists(Path))
                            await Context.Channel.SendEmbedAsync(lang["permission_role_no_permission"]); //Has no permissions
                        else
                            await Context.Channel.SendEmbedAsync(lang["permission_role_all_permission"]); //Has all permissions
                    }
                    break;
                case "reset":
                    if (File.Exists(Path))
                        File.Delete(Path);

                    //Remove cache
                    PermissionCache.TryRemove((Role.Id, Context.Guild.Id), out _);

                    await Context.Channel.SendEmbedAsync(lang["permission_reset", "ROLE", Role.Name]);
                    break;
            }
        }
        public async Task CopyPermissions(string role, LanguageDictionary lang)
        {
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

            IRole Role1 = Context.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == Roles[0] ||
                                                            x.Mention.Equals(Roles[0], StringComparison.CurrentCultureIgnoreCase) ||
                                                            x.Name.Equals(Roles[0], StringComparison.CurrentCultureIgnoreCase));
            IRole Role2 = Context.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == Roles[1] ||
                                                x.Mention.Equals(Roles[1], StringComparison.CurrentCultureIgnoreCase) ||
                                                x.Name.Equals(Roles[1], StringComparison.CurrentCultureIgnoreCase));

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

            //Copy permissions
            string Path = $"{KurumiPathConfig.GuildDatabase}{Context.Guild.Id}{KurumiPathConfig.Separator}Permissions{KurumiPathConfig.Separator}";
            Directory.CreateDirectory(Path);

            if (File.Exists($"{Path}{Role2.Id}.perm")) //Delete role 2 if exists
                File.Delete($"{Path}{Role2.Id}.perm");

            File.Copy($"{Path}{Role1.Id}.perm", $"{Path}{Role2.Id}.perm");
            await Context.Channel.SendEmbedAsync(lang["permission_copied", "ROLE1", Role1.Name, "ROLE2", Role2.Name]);
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