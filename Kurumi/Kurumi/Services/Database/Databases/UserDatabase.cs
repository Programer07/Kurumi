using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Models;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Database.Databases
{
    public class UserDatabase : KurumiDatabase<User>, IKurumiDatabase
    {
        public void Load()
        {
            Cache = new ConcurrentDictionary<ulong, User>();
            ConsoleHelper.Write("Waiting for MongoDB...", ConsoleColor.Yellow);
            var Users = DatabaseManager.Database.GetCollection<User>("User").Find(_ => true).ToList();
            for (int i = 0; i < Users.Count; i++)
            {
                var user = Users[i];
                ConsoleHelper.Write($"Loading: {i}/{Users.Count} | Current: {user.UserId}", ConsoleColor.Yellow);
                Cache.TryAdd(user.UserId, user);
            }
            ConsoleHelper.ClearCurrentLine();
            ConsoleHelper.Write("Users loaded.\n", ConsoleColor.Green);
        }

        public void Save(bool Show)
        {
            if (Cache.Count == 0)
                return;
            var Collection = DatabaseManager.Database.GetCollection<User>("User_Temp");
            var CacheCopy = new ConcurrentDictionary<ulong, User>(Cache);
            int i = 0;
            foreach (var User in CacheCopy)
            {
                if (Show)
                {
                    i++;
                    ConsoleHelper.Write($"Saving: {i}/{CacheCopy.Count} | Current: {User.Key}", ConsoleColor.Yellow);
                }
                User.Value.UserId = User.Key;
                Collection.InsertOne(User.Value);
            }
            if (Show)
            {
                ConsoleHelper.ClearCurrentLine();
                ConsoleHelper.Write("Users saved.\n", ConsoleColor.Green);
            }
            DatabaseManager.Database.DropCollection("User");
            DatabaseManager.Database.RenameCollection("User_Temp", "User");
        }
    }
}