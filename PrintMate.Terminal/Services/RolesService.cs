using HandyControl.Data;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;
using Opc.Ua;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace PrintMate.Terminal.Services
{
    public class RolesService
    {
        private readonly ConfigurationManager _configManager;
        private readonly DatabaseContext _dbContext;

        public RolesService(ConfigurationManager configManager, DatabaseContext dbContext)
        {
            _configManager = configManager;
            _dbContext = dbContext;
        }

        public List<Role> GetAllRoles()
        {
            var roleSettings = _configManager.Get<RoleSettings>();

            if (roleSettings == null)
            {
                // Создаем новые настройки если их нет
                roleSettings = new RoleSettings { Roles = new List<Role>() };
                _configManager.Update<RoleSettings>(s => s.Roles = new List<Role>());
                _configManager.SaveNow();
            }

            // Инициализируем список если он null
            if (roleSettings.Roles == null)
            {
                roleSettings.Roles = new List<Role>();
                _configManager.Update<RoleSettings>(s => s.Roles = new List<Role>());
                _configManager.SaveNow();
            }

            return roleSettings.Roles;
        }

        public Role GetRoleById(Guid id)
        {
            var role = GetAllRoles();
            var result = role.FirstOrDefault(p => p.Id == id);
            return result;
        }

        public Role GetRoleByName(string name)
        {
            var role = GetAllRoles();
            var result = role.FirstOrDefault(p => p.Name == name);
            return result;
        }

        public Role GetRoleByDisplayName(string displayName)
        {
            var role = GetAllRoles();
            var result = role.FirstOrDefault(p => p.DisplayName == displayName);
            return result;
        }

        public bool AddRole(string name, string displayName, List<string> permissions = null)
        {
            try
            {
                _configManager.Update<RoleSettings>(settings =>
                {
                    // Инициализируем если null
                    if (settings.Roles == null)
                    {
                        settings.Roles = new List<Role>();
                    }

                    var existingRole = settings.Roles.FirstOrDefault(p => p.Name == name);
                    if (existingRole != null)
                        throw new InvalidOperationException("Role already exists");

                    var role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        DisplayName = displayName,
                        Permissions = permissions ?? new List<string>()
                    };

                    settings.Roles.Add(role); // Теперь безопасно
                });

                _configManager.SaveNow();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to add role: {ex.Message}");
                return false;
            }

        }

        public bool RemoveRole(Guid id)
        {
            try
            {
                var usersWithRole = _dbContext.Users.Where(u => u.RoleId == id).ToList();

                _configManager.Update<RoleSettings>(settings =>
                {
                    if (settings.Roles == null) return;

                    var role = settings.Roles.FirstOrDefault(p => p.Id == id);
                    if (role != null)
                    {
                        settings.Roles.Remove(role);
                    }
                });

                _configManager.SaveNow();

                foreach (var user in usersWithRole)
                {
                    user.RoleId = Guid.Empty; 
                }

                if (usersWithRole.FirstOrDefault() != null)
                {
                    _dbContext.SaveChanges();
                    Console.WriteLine($"Role removed from {usersWithRole.Count} user(s)");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to remove role: {ex.Message}");
                return false;
            }
        }

        public bool UpdateRole(Role updated)
        {
            var settings = GetAllRoles();

            var role = settings.FirstOrDefault(r => r.Id == updated.Id);
            if (role == null)
                return false;

            role.Name = updated.Name;
            role.DisplayName = updated.DisplayName;
            role.Permissions = updated.Permissions ?? new List<string>();

            _configManager.SaveNow();
            return true;
        }

        public bool UpdateRole(Guid id, string name, string displayName, List<string> permissions)
        {
            try
            {
                _configManager.Update<RoleSettings>(settings =>
                {
                    if (settings.Roles == null) return;

                    var role = settings.Roles.FirstOrDefault(r => r.Id == id);
                    if (role == null)
                        throw new InvalidOperationException("Role not found");

                    role.Name = name;
                    role.DisplayName = displayName;
                    role.Permissions = permissions ?? new List<string>();
                });

                _configManager.SaveNow();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to update role: {ex.Message}");
                return false;
            }
        }

        // === Методы для работы с пользователями в БД ===

        public async Task<Role> GetUserRole(int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.RoleId == Guid.Empty)
                return null;

            return GetRoleById(user.RoleId);
        }

        public async Task<List<string>> GetUserPermissions(int userId)
        {
            var role = await GetUserRole(userId);
            return role.Permissions ?? new List<string>();
        }

        public async Task<bool> UserHasPermission(int userId, string permission)
        {
            var permissions = await GetUserPermissions(userId);
            return permissions.Contains(permission);
        }
        public async Task<bool> IsUserHasRole(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            var result = user.RoleId != Guid.Empty;

            return result;
        }
      
        public async Task<List<Models.User>> GetUsersByRoleId(Guid roleId) => await _dbContext.Users.Where(p => p.RoleId == roleId).ToListAsync();
       
        public async Task<bool> AssignRoleToUser(int userId, Guid roleId)
        {
            try
            {
                var role = GetRoleById(roleId);
                if (role == null) return false;

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return false;

                user.RoleId = roleId;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to assign role to user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromUser(int userId)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || user.RoleId == Guid.Empty)
                    return false;

                user.RoleId = Guid.Empty;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to remove role from user: {ex.Message}");
                return false;
            }
        }
    }
}
