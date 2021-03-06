﻿using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot
{
    public static class DiscordSocketGuildExt
    {
        public static async Task<ITextChannel> GetOrCreateTextChannelAsync(this SocketGuild guild, string channelName)
        {
            var existingChannel = guild.TextChannels.Where(c => c.Name == channelName.ToLower().Replace(' ', '-')).FirstOrDefault();
            if (existingChannel != null)
            {
                return existingChannel;
            }
            else
            {
                return await guild.CreateTextChannelAsync(channelName);
            }
        }
    }
}