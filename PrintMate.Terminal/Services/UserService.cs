using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Models;

namespace PrintMate.Terminal.Services
{
    public class UserService
    {
        private readonly DatabaseContext db;
        public UserService(DatabaseContext db)
        {
            this.db = db;
        }

        public async Task<List<User>> GetUsers()
        {
            return await db.Users.ToListAsync();
        }

        public async Task<User> GetByLogin(string login)
        {
            return await db.Users.FirstOrDefaultAsync(x => x.Login == login);
        }

        public async Task<bool> Add(User user)
        {
            var existingUser = await GetByLogin(user.Login);
            if (existingUser != null)
            {
                return false;
            }
            try
            {
                await db.Users.AddAsync(user);
                var result = await db.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to add user: {ex}");
                return false;
            }
        }

        public async Task<bool> Remove(string login)
        {
            var existingUser = await db.Users.FirstOrDefaultAsync(x => x.Login == login);
            if (existingUser == null) return false;

            try
            {
                db.Users.Remove(existingUser);
                var result = await db.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to remove user: {ex}");
                return false;
            }
        }

        public async Task<User> Update(User user)
        {
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return user;
        }
    }
}
