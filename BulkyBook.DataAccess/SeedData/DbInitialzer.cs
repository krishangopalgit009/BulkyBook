using BulkyBook.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.SeedData
{
    public class DbInitialzer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDBContext _dBContext;

        public DbInitialzer(UserManager<IdentityUser> userManager,
               RoleManager<IdentityRole> roleManager, ApplicationDBContext dBContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dBContext = dBContext;
        }

        public void Initialize()
        {
            //migrations if they are not applied

            try
            {
                if (_dBContext.Database.GetPendingMigrations().Count() > 0)
                {
                    _dBContext.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

            }

            //Create roles if they are not created

            if (!_roleManager.RoleExistsAsync(StaticDetail.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(StaticDetail.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetail.Role_User_Indi)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetail.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(StaticDetail.Role_User_Comp)).GetAwaiter().GetResult();

                //if roles are not created, then we will create admin user as well

                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "mohitpaul84@gmail.com",
                    Email = "mohitpaul84@gmail.com",
                    Name = "Krishan Gopal",
                    PhoneNumber = "9872115704",
                    StreetAddress = "IMC Building",
                    State = "Punjab",
                    PostalCode = "141001",
                    City = "Ludhiana"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser user = _dBContext.ApplicationUsers.FirstOrDefault(u => u.Email == "mohitpaul84@gmail.com");

                _userManager.AddToRoleAsync(user, StaticDetail.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }

    }
}
