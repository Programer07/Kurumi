using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Services.Permission
{
    public class CommandData
    {
        public static List<Command> CommandCache = new List<Command>();
        public static List<Command> GetCommandsFor(string category)
        {
            if (CommandCache.Count == 0)
                LoadCommands(out Exception e);
            List<Command> Commands = new List<Command>();
            for (int i = 0; i < CommandCache.Count; i++)
            {
                var command = CommandCache[i];
                if (command.Category.Equals(category, StringComparison.CurrentCultureIgnoreCase))
                    Commands.Add(command);
            }
            return Commands;
        }
        public static bool CategoryExists(string category)
        {
            if (CommandCache.Count == 0)
                LoadCommands(out _);

            for (int i = 0; i < CommandCache.Count; i++)
            {
                if (CommandCache[i].Category.Equals(category, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        public static Command GetCommand(string Command)
        {
            if (CommandCache.Count == 0)
                LoadCommands(out _);

            for (int i = 0; i < CommandCache.Count; i++)
            {
                if (CommandCache[i].CommandName.Equals(Command, StringComparison.CurrentCultureIgnoreCase))
                    return CommandCache[i];
            }
            return null;
        }

        public static bool LoadCommands(out Exception ex)
        {
            try
            {
                ex = null;
                //Load commands
                string Content = File.ReadAllText($"{KurumiPathConfig.Settings}Commands.json");
                List <Command> LoadedCommands = JsonConvert.DeserializeObject<List<Command>>(Content); //Pre loading to prevent reseting cache on cache reloading
                if (LoadedCommands == null)
                    return false;
                CommandCache = LoadedCommands;
                //Setup permissionless commands from permission manager
                List<string> Commands = new List<string>();
                foreach (Command o in CommandCache)
                {
                    if (!o.HasPermission)
                        Commands.Add(o.CommandName.ToLower());
                }

                //Hidden commands
                Commands.Add("lolidance");
                Commands.Add("eggplant");

                PermissionManager.PermissionlessCommands = Commands.ToArray(); //In PermissionManager.cs
                return true;
            }
            catch (Exception exception)
            {
                ex = exception;
                return false;
            }
        }
    }
}