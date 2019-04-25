# Kurumi

Kurumi is an open source discord bot written in c#. 
More information about the features on [discordbots.org](https://discordbots.org/bot/374274129282596885), [discord.bots.gg](https://discord.bots.gg/bots/374274129282596885) or [invite Kurumi](https://discordapp.com/oauth2/authorize?client_id=374274129282596885&scope=bot&permissions=8) and use the help command.

## Planned features
-  Youtube & Twitch stream/video notifications
-  Uno
-  Webadmin
-  Polls
-  Reset a user's exp on a guild
-  Remove users who left the guild from the leaderboard

## Self hosting
**Requirements:**<br>
-  Basic understanding of C#
-  .NET Core 3.0<br>
-  ffmpeg<br>
-  youtube-dl<br>
-  Visual Studio 2019 (or any IDE with .NET Core 3.0 support)<br>
-  Windows or Linux machine<br>

### Installing
**Windows**
1. Download .NET Core 3.0 (SDK and Runtime) from [here](https://dotnet.microsoft.com/download/dotnet-core/3.0)
2. Download and install Visual Studio from [here](https://visualstudio.microsoft.com/downloads/) (this will take some time)
3. Clone (or download) Kurumi (from here)
4. Build Kurumi but don't run it.
5. Copy the files from the directory (default: \bin\debug\netcoreapp3.0\publish) to the directory where you plan to start it from.
6. Open a command line and navigate to the directory
7. Type ``dotnet Kurumi.dll --console`` and hit enter
8. Use the extract command on all of the valid files. If it failes: Kurumi doesn't have permission to create a folder in the C drive. Close it and start it again as administrator or go to C:\ and create a folder named Kurumi.
9. Download ffmpeg from [here](https://ffmpeg.zeranoe.com/builds/)
10. Download youtube-dl from [here](if you see this I forgot to add the link)
11. Copy ffmpeg.exe and youtube-dl.exe to C:\Kurumi\Data\Bin
12. Download opus.dll & libsodium.dll and copy it into the folder where Kurumi is.
13. Open C:\Kurumi\Data\KurumiData.json and set the API & Kurumi version to the current version of Kurumi (or don't /shrug, it doesn't really matter)
14. Open C:\Kurumi\Data\Settings\Config.json and fill in the missing data (guide below)
15. You can now start Kurumi.
16. DO NOT CLOSE THE CONSOLE, IT WON'T SAVE ALL THE DATA! Type quit in the console and hit enter.

**Linux**
1. Download .NET Core 3.0 from the link in the windows guide.
2. Visual Studio is not available on Linux, but you can use Visual Studio Code [link](https://code.visualstudio.com/download)
3. Clone (or download) Kurumi (from here)
4. Build Kurumi but don't run it.
5. Copy the files from the build directory (I don't know whats the default directory in VS Code) to the directory where you plan to run it from.
6. Open terminal and run Kurumi with ``--console``
7. Use the extract command on all the valid files. (The root for Kurumi will be /Home/Kurumi but you can change it in the source code)
8. Download and install ffmpeg, youtube-dl, libsodium and opus
9. Open /Home/Kurumi/Data/KurumiData.json and set the Kurumi & API version (Not important)
10. Open /Home/Kurumi/Data/Settings/Config.json and fill in the missing data (guide below)
11. You can now start Kurumi with ``dotnet Kurumi.dll``
12. DO NOT CLOSE THE TERMINAL, IT WON'T SAVE ALL THE DATA! Type quit in the terminal and hit enter.

### Config
This is how the config file looks like by default (if not, some of this might be outdated).
Lines marked with * cannot be empty, you need to get those set up to be able to start Kurumi.
```
{
    "Environment": "Development",  <= In development mode the bot won't ignore other bots and won't send error reports to sentry
    *"BotToken": "", <= Discord bot token, register a bot at https://discordapp.com/developers/applications and copy the BOT token
    "IbSearchKey": "", <= Not used, the service died (http://ibsearch.xxx/). You can leave it empty
    *"YoutubeKey": "", <= Youtube API key. (https://console.developers.google.com/)
    "BotlistKey": "", <= Leave it empty
    "OsuKey": "", <= Used for the osu command, (https://osu.ppy.sh/api)
    "SentryKey": "", <= Online error tracking (https://sentry.io)
    "RandomOrgKey": "", <= Generate random numbers from atmospheric noise. (https://www.random.org/)
    "AniListClientId": "", <= Anime, manga and character command. (https://anilist.co/)
    "AniListSecret": "", <==||
    "EmbedColor": 0xFF6249, <= Color of the stripe next to the messages
    "DefaultLanguage": "en", <= The default language of Kurumi
    "YoutubeCacheSizeGB": 1, <= Kurumi will save the songs and when the total size is larger then this value the oldest song will be deleted (0 to disable)
    "BackupDB": false, <= Kurumi saves the data to the database every 30 minute, the first save after midnight (and before 3 AM) will create a backup of the database if this is set to true
    "LoggerMode": 0,
    *"ShardCount": 1, <= If you don't know this, you don't need it. Leave it on 1
    "Administrators": [ ] <= The users (only put ids in the list separated with ,) who can use admin-only commands, only the first in the list will receive DM error reports
}
```
**Logger mode**<br>
0 - Only messages<br>
1 - Message + guild: id<br>
2 - Message + guild: id, user: id<br>
3 - Message + guild: id, channel: id, user: id<br>
4 - Message + guild: name<br>
5 - Message + guild: name, user: name<br>
6 - Message + guild: name, channel: name, user: name<br>
7 - Message + g: id<br>
8 - Message + g: id, u: id<br>
9 - Message + g: id, c: id, u: id<br>
10 - Message + g: name<br>
11 - Message + g: name, u: name,<br>
12 - Message + g: name, c: name, u: name (I use this mode)<br>

### License
Licensed under the [MIT License](LICENSE.md).
