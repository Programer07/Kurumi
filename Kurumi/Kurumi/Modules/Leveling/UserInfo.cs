using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Leveling;
using nQuant;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Kurumi.Modules.Leveling
{
    public class UserInfo : ModuleBase
    {
        [Command("UserInfo")]
        [Alias("ui", "profile", "userprofile", "rank")]
        [RequireContext(ContextType.Guild)]
        public async Task Userinfo([Remainder, Optional]string user)
        {
            try
            {
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
                //Calculate rank
                List<ulong>[] Ranking = ExpManager.GetRanking(Context.Guild, Context.Client);
                int GlobalRank = Ranking[0].IndexOf(User.Id) + 1;
                int ServerRank = Ranking[1].IndexOf(User.Id) + 1;
                //Draw
                Bitmap board = DrawBoard(User, GlobalRank, ServerRank);
                //Compress, save, send, delete
                var quantized = new WuQuantizer().QuantizeImage(board);
                var Path = $"{KurumiPathConfig.Temp}u_{User.Id}.png";
                quantized.Save(Path, ImageFormat.Png);
                board.Dispose();
                quantized.Dispose();
                await Context.Channel.SendFileAsync(Path);
                File.Delete(Path);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Userinfo", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Userinfo", null, ex), Context);
            }
        }

        private readonly object _lock = new object();
        //If it works don't touch it
        private Bitmap DrawBoard(IUser user, int GRank, int SRank)
        {
            lock (_lock)
            {
                var lang = Language.GetLanguage(Context.Guild);
                Pen EmbedPen = new Pen(ColorTranslator.FromHtml("#FF6249"/*Config.EmbedColor*/), 8);
                //Create base
                Bitmap Board = new Bitmap(1200, 650);
                Graphics BoardGraphics = Graphics.FromImage(Board);
                //Fill color
                //BoardGraphics.FillRectangle(Brushes.White, 0 , 0 , BoardRender.Width, BoardRender.Height);
                BoardGraphics.DrawImage(Bitmap.FromFile($"{KurumiPathConfig.Graphics}user_panel.png"), 0, 0);
                Board = Lighten(Board, -32);
                //Draw avatar onto board
                Size AvatarSize = new Size(180, 180);
                Point UsernamePoint = new Point(220, 50);
                Point AvatarAt = new Point(25, 25);
                BoardGraphics.DrawImage(GetProfilePicture(user), AvatarAt.X, AvatarAt.Y, AvatarSize.Height, AvatarSize.Width);
                BoardGraphics.DrawRectangle(new Pen(Brushes.Gray, 6), AvatarAt.X, AvatarAt.Y, AvatarSize.Width, AvatarSize.Height);
                //Draw userame onto board
                SolidBrush UsernameBrush = new SolidBrush(ColorTranslator.FromHtml("#FFFFFF"));
                int Fontsize = 64;
                while (true)
                {
                    SizeF size = BoardGraphics.MeasureString(user.Username, new Font("Arial", Fontsize, FontStyle.Regular));
                    if (size.Width > 961.5F)
                        Fontsize--;
                    else
                        break;
                }
                BoardGraphics.DrawString(user.Username, new Font("Arial", Fontsize, FontStyle.Regular), UsernameBrush, UsernamePoint);
                //Draw playing status and status
                Color scolor = Color.AliceBlue;
                switch (user.Status)
                {
                    case UserStatus.Online:
                        scolor = ColorTranslator.FromHtml("#43B581");
                        break;
                    case UserStatus.Offline:
                    case UserStatus.Invisible:
                        scolor = Color.Gray;
                        break;
                    case UserStatus.Idle:
                        scolor = ColorTranslator.FromHtml("#FAA61A");
                        break;
                    case UserStatus.AFK:
                    case UserStatus.DoNotDisturb:
                        scolor = ColorTranslator.FromHtml("#F04747");
                        break;
                }
                BoardGraphics.DrawCircle(new Pen(new SolidBrush(Color.Gray), 3), UsernamePoint.X + 40, UsernamePoint.Y + 130, 20);
                BoardGraphics.FillCircle(new SolidBrush(scolor), UsernamePoint.X + 40, UsernamePoint.Y + 130, 19);
                Font StatusFont = new Font("Arial", 32, FontStyle.Regular);
                if (user.Activity != null)
                {
                    Console.WriteLine(user.Activity.Type.ToString() + " " + user.Activity.Name.ToString());
                    BoardGraphics.DrawString(user.Activity.Type.ToString() + " " + user.Activity.Name.ToString(), StatusFont, UsernameBrush,
                        new PointF(UsernamePoint.X + 65, UsernamePoint.Y + 107));
                }
                else
                    BoardGraphics.DrawString(lang[$"userpanel_{user.Status.ToString().ToLower()}"], StatusFont, UsernameBrush, new PointF(UsernamePoint.X + 65, UsernamePoint.Y + 107));
                //Draw discriminator
                BoardGraphics.DrawString("#" + user.Discriminator, new Font("Arial", 46, FontStyle.Regular), UsernameBrush, new Point(1000, 140));
                //Draw separator line under profile picture
                Pen SeparatorPen = new Pen(Brushes.Gray, 4);
                int SeparatorY = AvatarAt.Y + AvatarSize.Height + 30;
                BoardGraphics.DrawLine(SeparatorPen, new Point(0, SeparatorY), new Point(Board.Width, AvatarAt.X + AvatarSize.Height + 30));
                //Draw sideways separator
                int SeparatorX = 250;
                BoardGraphics.DrawLine(SeparatorPen, new Point(SeparatorX, SeparatorY), new Point(SeparatorX, Board.Height));
                //Draw rank texts
                Font RankingFont = new Font("Arial", 28, FontStyle.Regular);
                float RankingOffset = 10;
                BoardGraphics.DrawString(lang["userpanel_global"], RankingFont, UsernameBrush, new PointF(RankingOffset, SeparatorY + 20));
                BoardGraphics.DrawString(lang["userpanel_server"], RankingFont, UsernameBrush, new PointF(RankingOffset, SeparatorY + 150));
                //Draw rank
                string GlobalRank = "#" + GRank;
                string ServerRank = "#" + SRank;
                BoardGraphics.DrawString(GlobalRank, RankingFont, UsernameBrush, new PointF(220 - BoardGraphics.MeasureString(GlobalRank, RankingFont).Width, SeparatorY + 50 + RankingFont.Size));
                BoardGraphics.DrawString(ServerRank, RankingFont, UsernameBrush, new PointF(220 - BoardGraphics.MeasureString(ServerRank, RankingFont).Width, SeparatorY + 170 + RankingFont.Size));
                //Draw Kurumi embed line
                BoardGraphics.DrawLine(EmbedPen, new PointF(SeparatorX + 15, SeparatorY + 40), new PointF(SeparatorX + 15, Board.Height - 20));
                //Draw user name
                string Nickname = (user as SocketGuildUser).Nickname;
                BoardGraphics.DrawString(lang["userpanel_nickname"] + (string.IsNullOrWhiteSpace(Nickname) ? user.Username : Nickname), RankingFont, UsernameBrush, new PointF(SeparatorX + 35, SeparatorY + 60));
                //Draw joined
                BoardGraphics.DrawString(lang["userpanel_joined"] + (user as SocketGuildUser).JoinedAt.Value.DateTime.ToLongDateString(), RankingFont, UsernameBrush, new PointF(SeparatorX + 35, SeparatorY + 120));
                //Draw roles
                var Roles = (user as SocketGuildUser).Roles.ToArray();
                int index = 0;
                int Limit = 750;
                string DisplayRoles = string.Empty;
                string temp = string.Empty;
                while (index < Roles.Length)
                {
                    string role = Roles[index].Name;
                    if (role != "@everyone")
                    {
                        temp += Roles[index] + ", ";
                        SizeF size = BoardGraphics.MeasureString(temp, RankingFont);
                        if (size.Width > Limit)
                        {
                            DisplayRoles += "...";
                            break;
                        }
                        else
                            DisplayRoles = temp;
                    }
                    index++;
                }
                if (DisplayRoles == string.Empty)
                    DisplayRoles = "-";
                BoardGraphics.DrawString(lang["userpanel_roles", "NUMBER", ((user as SocketGuildUser).Roles.Count - 1).ToString()]
                    + $"{DisplayRoles}", RankingFont, UsernameBrush, new PointF(SeparatorX + 35, SeparatorY + 180));
                //Draw currency stuffs
                int GlobalExp = (int)GlobalUserDatabase.GetOrFake(user.Id).Exp;
                int ServerExp = (int?)GuildUserDatabase.Get(Context.Guild.Id, user.Id)?.Exp ?? 0;
                BoardGraphics.DrawString($"{lang["userpanel_exp_global"]}{GlobalExp,6} ({lang["userpanel_exp_level"]}{ExpManager.Level((uint)GlobalExp, GuildConfigDatabase.INC_GUILD),3})" +
                                         $"\n{lang["userpanel_exp_server"]}{ServerExp,8} ({lang["userpanel_exp_level"]}" +
                                         $"{ExpManager.Level((uint)GlobalExp, (uint)GuildConfigDatabase.GetOrFake(Context.Guild.Id).Inc),3})",
                    RankingFont, UsernameBrush, new PointF(SeparatorX + 35, SeparatorY + 240)); //Exp & level
                BoardGraphics.DrawString($"{lang["userpanel_credits"]}{GlobalUserDatabase.GetOrFake(user.Id).Credit,7}¥",
                    RankingFont, UsernameBrush, new PointF(SeparatorX + 650, SeparatorY + 240)); //Credits
                                                                                                 //Draw voted
                BoardGraphics.DrawString(lang["userpanel_voted", "VOTED", (DiscordBotlist.UserVoted(user.Id).Result ? "Yes" : "No")],
                    RankingFont, UsernameBrush, new PointF(SeparatorX + 35, SeparatorY + 340));
                //Delete graphics
                BoardGraphics.Dispose();
                return Board;
            }
        }

        public static Bitmap GetProfilePicture(IUser user)
        {
            if (user.GetAvatarUrl() == null)
                return new Bitmap(1, 1);
            WebRequest request = WebRequest.Create(user.GetAvatarUrl());
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            return new Bitmap(responseStream);
        }
        public static Bitmap Lighten(Bitmap bitmap, int amount)
        {
            if (amount < -255 || amount > 255)
                return bitmap;

            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            IntPtr Scan0 = bmData.Scan0;

            int nVal = 0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - bitmap.Width * 3;
                int nWidth = bitmap.Width * 3;

                for (int y = 0; y < bitmap.Height; ++y)
                {
                    for (int x = 0; x < nWidth; ++x)
                    {
                        nVal = (int)(p[0] + amount);

                        if (nVal < 0) nVal = 0;
                        if (nVal > 255) nVal = 255;

                        p[0] = (byte)nVal;

                        ++p;
                    }
                    p += nOffset;
                }
            }
            bitmap.UnlockBits(bmData);

            return bitmap;
        }
    }
}