using Behsazan.Application.Interfaces;
using Behsazan.Domain.Entities;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Behsazan.Infrastructure.Seeding;

public class DefaultDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DefaultDataSeeder> _logger;

    public DefaultDataSeeder(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<DefaultDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        #region Exit if Already Seeded
        if (await _dbContext.Roles.AnyAsync(r => r.Name == "Admin"))
        {
            _logger.LogInformation("Default data already seeded. Skipping.");
            return;
        }
        #endregion

        #region Seed Permissions
        var permissions = GetDefaultPermissions();

        foreach (var permission in permissions)
        {
            if (!await _dbContext.Permissions.AnyAsync(p => p.Key == permission.Key))
            {
                _dbContext.Permissions.Add(permission);
            }
        }
        #endregion

        #region Seed Admin Role
        var adminRole = new Role
        {
            Name = "Admin",
            IsSystem = true,
            CreatedBy = -1,
        };
        _dbContext.Roles.Add(adminRole);
        #endregion

        #region Seed Admin User
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = _passwordHasher.Hash("Admin@123"),
            IsActive = true,
            CreatedBy = -1,
        };
        _dbContext.Users.Add(adminUser);
        #endregion

        await _dbContext.SaveChangesAsync();

        #region Seed UserRole + RolePermissions
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedBy = -1,
        };
        _dbContext.UserRoles.Add(userRole);

        var savedPermissions = await _dbContext.Permissions.ToListAsync();

        foreach (var permission in savedPermissions)
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                CreatedBy = -1,
            });
        }

        await _dbContext.SaveChangesAsync();
        #endregion

        _logger.LogInformation("Default data seeded successfully. Admin user: admin / Admin@123");
    }

    private static List<Permission> GetDefaultPermissions()
    {
        return new()
        {
            #region Customers
            new() { Key = "customers.view",     NameFa = "مشاهده مشتریان",      Module = "Customers", Action = "View",     IsActive = true, CreatedBy = -1 },
            new() { Key = "customers.create",    NameFa = "ایجاد مشتری",          Module = "Customers", Action = "Create",   IsActive = true, CreatedBy = -1 },
            new() { Key = "customers.edit",      NameFa = "ویرایش مشتری",         Module = "Customers", Action = "Edit",     IsActive = true, CreatedBy = -1 },
            new() { Key = "customers.delete",    NameFa = "حذف مشتری",            Module = "Customers", Action = "Delete",   IsActive = true, CreatedBy = -1 },
            #endregion

            #region Projects
            new() { Key = "projects.view",      NameFa = "مشاهده پروژه‌ها",       Module = "Projects",  Action = "View",     IsActive = true, CreatedBy = -1 },
            new() { Key = "projects.create",     NameFa = "ایجاد پروژه",           Module = "Projects",  Action = "Create",   IsActive = true, CreatedBy = -1 },
            new() { Key = "projects.edit",       NameFa = "ویرایش پروژه",          Module = "Projects",  Action = "Edit",     IsActive = true, CreatedBy = -1 },
            new() { Key = "projects.delete",     NameFa = "حذف پروژه",            Module = "Projects",  Action = "Delete",   IsActive = true, CreatedBy = -1 },
            #endregion

            #region Invoices
            new() { Key = "invoices.view",       NameFa = "مشاهده فاکتورها",       Module = "Invoices",  Action = "View",     IsActive = true, CreatedBy = -1 },
            new() { Key = "invoices.create",      NameFa = "ایجاد فاکتور",          Module = "Invoices",  Action = "Create",   IsActive = true, CreatedBy = -1 },
            new() { Key = "invoices.edit",        NameFa = "ویرایش فاکتور",         Module = "Invoices",  Action = "Edit",     IsActive = true, CreatedBy = -1 },
            new() { Key = "invoices.delete",      NameFa = "حذف فاکتور",           Module = "Invoices",  Action = "Delete",   IsActive = true, CreatedBy = -1 },
            #endregion

            #region Deposits
            new() { Key = "deposits.view",       NameFa = "مشاهده پرداخت‌ها",      Module = "Deposits",  Action = "View",     IsActive = true, CreatedBy = -1 },
            new() { Key = "deposits.create",      NameFa = "ثبت پرداخت",            Module = "Deposits",  Action = "Create",   IsActive = true, CreatedBy = -1 },
            new() { Key = "deposits.edit",        NameFa = "ویرایش پرداخت",         Module = "Deposits",  Action = "Edit",     IsActive = true, CreatedBy = -1 },
            new() { Key = "deposits.delete",      NameFa = "حذف پرداخت",           Module = "Deposits",  Action = "Delete",   IsActive = true, CreatedBy = -1 },
            #endregion

            #region Users
            new() { Key = "users.manage",        NameFa = "مدیریت کاربران",        Module = "Users",     Action = "Manage",   IsActive = true, CreatedBy = -1 },
            #endregion

            #region Roles
            new() { Key = "roles.manage",        NameFa = "مدیریت نقش‌ها",         Module = "Roles",     Action = "Manage",   IsActive = true, CreatedBy = -1 },
            #endregion
        };
    }
}
