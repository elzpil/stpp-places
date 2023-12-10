﻿using Microsoft.AspNetCore.Identity;
using stpp.Auth.Model;
using stpp.Data;

namespace stpp.Auth
{
    public class AuthDbSeeder
    {
        private readonly UserManager<ForumRestUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AuthDbSeeder(UserManager<ForumRestUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync ()
        {
            await AddDefaultRoles();
            await AddAdminUser();
        }

        private async Task AddDefaultRoles()
        {
            foreach(var role in ForumRoles.All)
            {
                var roleExists  = await _roleManager.RoleExistsAsync(role);
                if(!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
        private async Task AddAdminUser()
        {
            var newAdminUser = new ForumRestUser
            {
                UserName = "adminn",
                Email = "admin@admin.com"
            };
            var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (existingAdminUser == null)
            {
                var createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "VerySafePassword1!");

                if (createAdminUserResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newAdminUser, ForumRoles.Admin);
                }
            }
        }

    }
}
