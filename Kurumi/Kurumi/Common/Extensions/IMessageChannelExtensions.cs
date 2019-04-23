using Discord;
using Discord.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Common.Extensions
{
    public static class IMessageChannelExtensions
    {
        public static async Task<(IUserMessage Message, EmbedBuilder Embed)> SendEmbedAsync(this IMessageChannel Channel,
            object Description = null, object Title = null, object Footer = null, object ImageUrl = null, object ThumbnailUrl = null, object Url = null)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Config.EmbedColor);
            if (Description != null)
                embed.WithDescription(Description.ToString());
            if (Title != null)
                embed.WithTitle(Title.ToString());
            if (Footer != null)
                embed.WithFooter(Footer.ToString());
            if (ImageUrl != null)
                embed.WithImageUrl(ImageUrl.ToString());
            if (ThumbnailUrl != null)
                embed.WithThumbnailUrl(ThumbnailUrl.ToString());
            if (Url != null)
                embed.WithUrl(Url.ToString());
            return (await Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false), embed);
        }

        public static async Task<(IUserMessage Message, EmbedBuilder Embed)> SendEmbedAsync(this IUser User,
            object Description = null, object Title = null, object Footer = null, object ImageUrl = null, object ThumbnailUrl = null, object Url = null)
                => await (await User.GetOrCreateDMChannelAsync()).SendEmbedAsync(Description, Title, Footer, ImageUrl, ThumbnailUrl, Url);

        public static async Task<(IUserMessage Message, EmbedBuilder Embed)?> TrySendEmbedAsync(this IMessageChannel Channel,
            object Description = null, object Title = null, object Footer = null, object ImageUrl = null, object ThumbnailUrl = null, object Url = null)
        {
            try
            {
                return await Channel.SendEmbedAsync(Description, Title, Footer, ImageUrl, ThumbnailUrl, Url);
            }
            catch(Exception)
            {
                return null;
            }
        }
        public static async Task<(IUserMessage Message, EmbedBuilder Embed)?> TrySendEmbedAsync(this IUser User,
            object Description = null, object Title = null, object Footer = null, object ImageUrl = null, object ThumbnailUrl = null, object Url = null)
        {
            try
            {
                return await User.SendEmbedAsync(Description, Title, Footer, ImageUrl, ThumbnailUrl, Url);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<IUserMessage> SendEmbedAsync(this IMessageChannel Channel, EmbedBuilder embed)
            => await Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
    }
}