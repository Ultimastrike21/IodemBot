﻿using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Modules;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.ColossoBattles.ColossoPvE;

namespace IodemBot.Core.Leveling
{
    internal static class ServerGames
    {
        internal static async void UserWonColosso(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(40, 70);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            if (userAccount.ServerStats.ColossoStreak > userAccount.ServerStats.ColossoHighestStreak)
            {
                userAccount.ServerStats.ColossoHighestStreak = userAccount.ServerStats.ColossoStreak;
            }

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            if (userAccount.ServerStats.ColossoWins >= 15)
            {
                await GoldenSun.AwardClassSeries("Brute Series", user, channel);
            }

            await Task.CompletedTask;
        }

        internal static async void UserLostColosso(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(1, 10);
            uint newLevel = userAccount.LevelNumber;
            userAccount.ServerStats.ColossoStreak = 0;
            UserAccounts.SaveAccounts();

            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            await Task.CompletedTask;
        }

        internal static async Task UserSentCommand(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.CommandsUsed++;
            if (userAccount.ServerStats.CommandsUsed >= 100)
            {
                await GoldenSun.AwardClassSeries("Scrapper Series", user, channel);
            }
        }

        internal static async Task UserWonRPS(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.rpsWins++;
            userAccount.ServerStats.rpsStreak++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.rpsStreak == 4)
            {
                await GoldenSun.AwardClassSeries("Air Seer Series", user, channel);
            }

            if (userAccount.ServerStats.rpsWins == 15)
            {
                await GoldenSun.AwardClassSeries("Aqua Seer Series", user, channel);
            }
        }

        internal static void UserDidNotWinRPS(SocketGuildUser user)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.rpsStreak = 0;
            UserAccounts.SaveAccounts();
        }

        internal static async Task UserWonBattle(UserAccount userAccount, int winsInARow, BattleStats battleStats, ColossoPvE.BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            var xpawarded = (uint)(new Random()).Next(20, 40) * Math.Min(3, (uint)Math.Pow(((int)diff + 1), 2));
            userAccount.XP += xpawarded;
            userAccount.inv.AddBalance(xpawarded / 2);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            userAccount.ServerStats.ColossoHighestStreak = Math.Max(userAccount.ServerStats.ColossoHighestStreak, userAccount.ServerStats.ColossoStreak);
            userAccount.ServerStats.ColossoHighestRoundEndless = Math.Max(userAccount.ServerStats.ColossoHighestRoundEndless, winsInARow);

            userAccount.BattleStats += battleStats;
            var bs = userAccount.BattleStats;

            if (Global.random.Next(0, 100) <= 7 + battleStats.totalTeamMates * 2)
            {
                ChestQuality awardedChest = getRandomChest(diff);
                userAccount.inv.AwardChest(awardedChest);
                var embed = new EmbedBuilder();
                embed.WithColor(Colors.get("Iodem"));
                embed.WithDescription($"{((SocketTextChannel)battleChannel).Users.Where(u => u.Id == userAccount.ID).FirstOrDefault().Mention} found a {Inventory.ChestIcons[awardedChest]} {awardedChest} Chest!");
                await battleChannel.SendMessageAsync("", false, embed.Build());
            }

            if (userAccount.ServerStats.ColossoWins >= 15)
            {
                await GoldenSun.AwardClassSeries("Brute Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.killsByHand >= 161)
            {
                await GoldenSun.AwardClassSeries("Samurai Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.damageDealt >= 666666)
            {
                await GoldenSun.AwardClassSeries("Ninja Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.soloBattles >= 50)
            {
                await GoldenSun.AwardClassSeries("Ranger Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.totalTeamMates >= 100)
            {
                await GoldenSun.AwardClassSeries("Dragoon Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.HPhealed >= 333333)
            {
                await GoldenSun.AwardClassSeries("White Mage Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.revives >= 50)
            {
                await GoldenSun.AwardClassSeries("Medium Series", userAccount, (SocketTextChannel)battleChannel);
            }

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        private static ChestQuality getRandomChest(BattleDifficulty diff)
        {
            ChestQuality[] chests;
            switch (diff)
            {
                case BattleDifficulty.Tutorial:
                    chests = new ChestQuality[] { ChestQuality.Wooden };
                    return chests[Global.random.Next(0, chests.Length)];

                case BattleDifficulty.Easy:
                default:
                    chests = new ChestQuality[] { ChestQuality.Wooden, ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Silver };
                    return chests[Global.random.Next(0, chests.Length)];

                case BattleDifficulty.Medium:
                    chests = new ChestQuality[] { ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Silver };
                    return chests[Global.random.Next(0, chests.Length)];

                case BattleDifficulty.MediumRare:
                    chests = new ChestQuality[] { ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Gold };
                    return chests[Global.random.Next(0, chests.Length)];

                case BattleDifficulty.Hard:
                    chests = new ChestQuality[] { ChestQuality.Silver, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Adept };
                    return chests[Global.random.Next(0, chests.Length)];
            }
        }

        internal static async Task UserHasCursed(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            if (userAccount.ServerStats.hasQuotedMatthew && userAccount.ServerStats.hasWrittenCurse)
            {
                await GoldenSun.AwardClassSeries("Curse Mage Series", user, channel);
            }
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ColossoPvE.BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(0, 10);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoStreak = 0;

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpPsynergy(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.lookedUpInformation++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.lookedUpInformation >= 21)
            {
                await GoldenSun.AwardClassSeries("Apprentice Series", user, channel);
            }
        }

        internal static async Task UserLookedUpClass(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.lookedUpClass++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.lookedUpClass >= 21)
            {
                await GoldenSun.AwardClassSeries("Page Series", user, channel);
            }
        }
    }
}