using App.Contracts;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }


    public static void SeedAppData(AppDbContext context)
    {
        SeedLookUpTable(context.ManagementCompanyRoles, InitialData.ManagementCompanyRoleSeeds);
        SeedLookUpTable(context.CustomerRepresentativeRoles, InitialData.CustomerRepresentativeRoleSeeds);
        SeedLookUpTable(context.ContactTypes, InitialData.ContactTypeSeeds);
        SeedLookUpTable(context.PropertyTypes, InitialData.PropertyTypeSeeds);
        SeedLookUpTable(context.LeaseRoles, InitialData.LeaseRoleSeeds);
        SeedLookUpTable(context.TicketCategories, InitialData.TicketCategorySeeds);
        SeedLookUpTable(context.TicketStatuses, InitialData.TicketStatusSeeds);
        SeedLookUpTable(context.TicketPriorities, InitialData.TicketPrioritySeeds);
        SeedLookUpTable(context.WorkStatuses, InitialData.WorkStatusSeeds);
        
        context.SaveChanges();
    }

    public static void SeedLookUpTable<T> (DbSet<T> table, (string code, string en, string et)[] seed)
    where T : class, ILookUpEntity, new()
    {
        foreach ((string code, string en, string et) in seed)
        {
            if (table.Any(x => x.Code == code)) continue;
            
            var entity = new T
            {
                Code = code,
                Label = new LangStr
                {
                    ["en"] = en,
                    ["et"] = et
                }
            };
            table.Add(entity);
        }
        
    }
    
     public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = roleManager.FindByNameAsync(roleName).Result;

            if (role != null) continue;

            role = new AppRole()
            {
                Name = roleName,
            };

            var result = roleManager.CreateAsync(role).Result;
            if (!result.Succeeded)
            {
                throw new ApplicationException("Role creation failed!");
            }
        }


        foreach (var userInfo in InitialData.Users)
        {
            var user = userManager.FindByEmailAsync(userInfo.email).Result;
            if (user == null)
            {
                user = new AppUser()
                {
                    Email = userInfo.email,
                    UserName = userInfo.email,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(user, userInfo.password).Result;
                if (!result.Succeeded)
                {
                    throw new ApplicationException("User creation failed!");
                }
            }

            foreach (var role in userInfo.roles)
            {
                if (userManager.IsInRoleAsync(user, role).Result)
                {
                    Console.WriteLine($"User {user.UserName} already in role {role}");
                    continue;
                }
                
                var roleResult = userManager.AddToRoleAsync(user, role).Result;
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
                else
                {
                    Console.WriteLine($"User {user.UserName} added to role {role}");
                }
            }
        }
    }

}