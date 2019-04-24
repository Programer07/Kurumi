using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Leveling;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace Kurumi.Modules.Leveling
{
    public class Level : ModuleBase
    {
        [Command("level")]
        [RequireContext(ContextType.Guild)]
        public async Task SendLevel([Optional, Remainder]string user)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);

                //Get user
                IUser User;
                if (user == null)
                    User = Context.User;
                else
                    User = await Utilities.GetUser(Context.Guild, user);
                if (User == null)
                {
                    await Context.Channel.SendEmbedAsync(Language.GetLanguage(Context.Guild)["level_user_not_found"]);
                    return;
                }
                //Draw save send delete
                Bitmap Panel = DrawLevelBoard(User, lang);
                string Path = $"{KurumiPathConfig.Temp}l_{User.Id}.png";
                Panel.Save(Path);
                await Context.Channel.SendFileAsync(Path);
                File.Delete(Path);

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Level", "success"), Context);
            }
            catch (Exception ex)
            {
                if (ex is ExternalException extEx && extEx.Message == "A generic error occurred in GDI+.")
                    await Utilities.Log(new LogMessage(LogSeverity.Warning, "Level", extEx.Message), Context);
                else
                    await Utilities.Log(new LogMessage(LogSeverity.Error, "Level", null, ex));
            }
        }

        private Bitmap DrawLevelBoard(IUser User, LanguageDictionary lang)
        {
            //Get exp
            uint UserGlobalExp = GlobalUserDatabase.GetOrFake(User.Id).Exp;
            uint UserServerExp = GuildUserDatabase.Get(Context.Guild.Id, User.Id)?.Exp ?? 0;
            //Get level
            byte CurrentGlobalLevel = ExpManager.Level(UserGlobalExp, GuildConfigDatabase.INC_GLOBAL);
            uint Increment = (uint)GuildConfigDatabase.GetOrFake(Context.Guild.Id).Inc;
            byte CurrentServerLevel = ExpManager.Level(UserServerExp, Increment);
            //Get current level minimum exp
            uint CurrentGlobalLevelExp = ExpManager.LevelStartExp(CurrentGlobalLevel, GuildConfigDatabase.INC_GLOBAL);
            uint CurrentServerLevelExp = ExpManager.LevelStartExp(CurrentServerLevel, Increment);
            //Get next level minimum exp
            uint NextGlobalLevelExp = ExpManager.LevelStartExp(CurrentGlobalLevel + 1U, GuildConfigDatabase.INC_GLOBAL);
            uint NextServerLevelExp = ExpManager.LevelStartExp(CurrentServerLevel + 1U, Increment);

            double GlobalProgress = (double)UserGlobalExp / NextGlobalLevelExp * 100;
            double ServerProgress = (double)UserServerExp / NextServerLevelExp * 100;

            Bitmap Board = new Bitmap(340, 200);
            Graphics BoardGraphics = Graphics.FromImage(Board);
            BoardGraphics.DrawImage(Bitmap.FromFile($"{KurumiPathConfig.Graphics}level_panel.png"), 0, 0, 340, 200);

            SolidBrush b = new SolidBrush(Color.White);

            //DRAW GLOBAL
            Font f = new Font("Palatino", 12, FontStyle.Regular);

            //Draw progressbar
            Bitmap GlobalProgressBar = DrawProgressbar((uint)GlobalProgress, UserGlobalExp);
            BoardGraphics.DrawImage(GlobalProgressBar, new Point(20, 55));

            //Draw min
            int ExpY = 80;
            BoardGraphics.DrawString(CurrentGlobalLevelExp.ToString(), f, b, new Point(16, ExpY));

            //Draw max
            float GlobalMaxLength = BoardGraphics.MeasureString(NextGlobalLevelExp.ToString(), f).Width;
            BoardGraphics.DrawString(NextGlobalLevelExp.ToString(), f, b, new PointF(320 - GlobalMaxLength, ExpY));


            //Draw current level
            int LevelY = 35;
            BoardGraphics.DrawString(lang["level_level"] + CurrentGlobalLevel.ToString(), f, b, new Point(16, LevelY));

            //Draw next level
            float GLength = BoardGraphics.MeasureString(lang["level_level"] + (CurrentGlobalLevel + 1).ToString(), f).Width;
            BoardGraphics.DrawString(lang["level_level"] + (CurrentGlobalLevel + 1).ToString(), f, b, new PointF(320 - GLength, LevelY));



            //DRAW SERVER

            //Draw progressbar
            Bitmap ServerProgressBar = DrawProgressbar((uint)ServerProgress, UserServerExp);
            BoardGraphics.DrawImage(ServerProgressBar, new Point(20, 155));

            //Draw min
            ExpY = 180;
            BoardGraphics.DrawString(CurrentServerLevelExp.ToString(), f, b, new Point(18, ExpY));

            //Draw max
            float ServerMaxLength = BoardGraphics.MeasureString(NextServerLevelExp.ToString(), f).Width;
            BoardGraphics.DrawString(NextServerLevelExp.ToString(), f, b, new PointF(320 - ServerMaxLength, ExpY));

            //Draw current level
            LevelY = 135;
            BoardGraphics.DrawString(lang["level_level"] + CurrentServerLevel.ToString(), f, b, new Point(18, LevelY));

            //Draw next level
            float SLength = BoardGraphics.MeasureString(lang["level_level"] + (CurrentServerLevel + 1).ToString(), f).Width;
            BoardGraphics.DrawString(lang["level_level"] + (CurrentServerLevel + 1).ToString(), f, b, new PointF(320 - SLength, LevelY));


            //Write global and server
            BoardGraphics.DrawString(lang["level_global_level"], f, b, new Point(5, 10));
            BoardGraphics.DrawString(lang["level_server_level"], f, b, new Point(5, 110));
            return Board;
        }

        private static Bitmap DrawProgressbar(uint progress, uint value)
        {
            Bitmap ProgressBar = new Bitmap(300, 20);
            Graphics ProgressBarGraphics = Graphics.FromImage(ProgressBar);


            //Draw outline
            Pen p = new Pen(new SolidBrush(Color.DarkGray), 2);
            ProgressBarGraphics.DrawRectangle(p, new Rectangle(new Point(1, 1), new Size(298, 18)));

            //Draw progress
            float Length = (float)progress / 100 * 296;
            ProgressBarGraphics.FillRectangle(new SolidBrush(ColorTranslator.FromHtml("#FF6249")), new RectangleF(new PointF(2, 2), new SizeF(Length, 16)));

            //Draw value
            Font f = new Font("Palatino", 10, FontStyle.Regular);
            float ValueLength = ProgressBarGraphics.MeasureString(value.ToString(), f).Width - 2;
            if (Length > ValueLength)
            {
                ProgressBarGraphics.DrawString(value.ToString(), f, new SolidBrush(Color.White), new PointF(Length - ValueLength, 2));
            }


            return ProgressBar;
        }
    }
}
