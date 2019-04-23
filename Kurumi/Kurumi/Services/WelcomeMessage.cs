using Discord;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services
{
    public static class WelcomeMessage
    {
        public static Task SendMessage(SocketGuildUser arg)
        {
            return Task.Run(async () =>
            {
                SocketGuild guild = arg.Guild;
                try
                {
                    //Get channel id
                    ulong ChannelId = GuildConfigDatabase.GetOrFake(guild.Id).WelcomeChannel;
                    //If channel is not 0
                    if (ChannelId != 0)
                    {
                        //Check if channel exists
                        if (!(Program.Bot.DiscordClient.GetChannel(ChannelId) is SocketTextChannel channel))
                        {
                            //Warn guild owner
                            LanguageDictionary lang = Language.GetLanguage(guild);
                            await guild.Owner.SendEmbedAsync(lang["welcomemessage_channel_invalid", "GUILD", guild.Name]);
                            return Task.CompletedTask;
                        }
                        //Get welcome message
                        KurumiRandom rand = new KurumiRandom();
                        List<string> Messages = GuildConfigDatabase.Get(guild.Id).WelcomeMessages;
                        if (Messages.Count == 0) //There was a bug which let users set welcome channel without message, this corrects that error.
                            Messages.Add("Welcome {user} to the {server} server!"); 

                        int Index = 0;
                        if (Messages.Count != 1)
                            Index = rand.Next(0, Messages.Count - 1);
                        string Message = Messages[Index]
                                .Replace("{user}", arg.Mention, StringComparison.OrdinalIgnoreCase)
                                .Replace("{server}", guild.Name, StringComparison.OrdinalIgnoreCase)
                                .Replace("{membercount}", guild.MemberCount.ToString(), StringComparison.OrdinalIgnoreCase);
                        //Send
                        await channel.SendMessageAsync(Message);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("The server responded with error 50013: Missing Permissions"))
                    {
                        try
                        {
                            var lang = Language.GetLanguage(guild);
                            await guild.Owner.SendEmbedAsync(lang["welcomemessage_no_permission"]);
                        }
                        catch (Exception) { } //Owner disabled direct messages.
                        return Task.CompletedTask;
                    }
                    await Utilities.Log(new LogMessage(LogSeverity.Error, "SendWelcomeMessage", null, ex));
                }
                return Task.CompletedTask;
            });
        }
    }
}