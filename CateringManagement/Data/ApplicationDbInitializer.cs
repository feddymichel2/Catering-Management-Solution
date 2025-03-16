using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CateringManagement.Data
{
    public static class ApplicationDbInitializer
    {
        static string[] roleNames = { "Admin", "Security", "Supervisor", "Staff" };
        public static async void Seed(IApplicationBuilder applicationBuilder)
        {
            ApplicationDbContext context = applicationBuilder.ApplicationServices.CreateScope()
                .ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                //Create the database if it does not exist and apply the Migration
                context.Database.Migrate();

                //Create Roles
                var RoleManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                //string[] roleNames = { "Admin", "Security", "Supervisor", "Staff" };
                IdentityResult roleResult;
                foreach (var roleName in roleNames)
                {
                    var roleExist = await RoleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
                //Create Users
                var userManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                AddUser(userManager, "dstovell@niagaracollege.ca", "Admin,Security");
                AddUser(userManager, "jkaluba@niagaracollege.ca", "Admin,Security");
                AddUser(userManager, "admin@outlook.com", "Admin,Security", "Pa55w@rd");
                AddUser(userManager, "security@outlook.com", "Security", "Pa55w@rd");
                AddUser(userManager, "supervisor@outlook.com", "Supervisor", "Pa55w@rd");
                AddUser(userManager, "staff@outlook.com", "Staff", "Pa55w@rd");
                AddUser(userManager, "user@outlook.com");
                //Now add all of the students
                string[] safeEmails = new string[] { "halbashatweh1@ncstudents.niagaracollege.ca", "ballen9@ncstudents.niagaracollege.ca", "varaujoguedes1@ncstudents.niagaracollege.ca", "dawan1@ncstudents.niagaracollege.ca", "dbaptiste2@ncstudents.niagaracollege.ca", "abaxter9@ncstudents.niagaracollege.ca", "sbhandari19@ncstudents.niagaracollege.ca", "jbinalay1@ncstudents.niagaracollege.ca", "fbriones1@ncstudents.niagaracollege.ca", "dbriscall1@ncstudents.niagaracollege.ca", "ebugiardini1@ncstudents.niagaracollege.ca", "scabrera2@ncstudents.niagaracollege.ca", "jcarmichael3@ncstudents.niagaracollege.ca", "jcastanomejia1@ncstudents.niagaracollege.ca", "achepurnov2@ncstudents.niagaracollege.ca", "jclayton5@ncstudents.niagaracollege.ca", "rcote6@ncstudents.niagaracollege.ca", "adagan1@ncstudents.niagaracollege.ca", "ndautermann1@ncstudents.niagaracollege.ca", "ddavidson14@ncstudents.niagaracollege.ca", "jdeguzman4@ncstudents.niagaracollege.ca", "jdimarcantonio2@ncstudents.niagaracollege.ca", "mdionne1@ncstudents.niagaracollege.ca", "ddivyansh2@ncstudents.niagaracollege.ca", "bdurocher4@ncstudents.niagaracollege.ca", "gdyer1@ncstudents.niagaracollege.ca", "ndykstra2@ncstudents.niagaracollege.ca", "nerbek1@ncstudents.niagaracollege.ca", "ofamobiwo1@ncstudents.niagaracollege.ca", "afindlater1@ncstudents.niagaracollege.ca", "jfuentesgonzale1@ncstudents.niagaracollege.ca", "jgill82@ncstudents.niagaracollege.ca", "sgill127@ncstudents.niagaracollege.ca", "mgonzalesgonzal1@ncstudents.niagaracollege.ca", "mhadvaidya1@ncstudents.niagaracollege.ca", "rhavryshkiv1@ncstudents.niagaracollege.ca", "chead3@ncstudents.niagaracollege.ca", "khenderson17@ncstudents.niagaracollege.ca", "rherreraaguilar1@ncstudents.niagaracollege.ca", "nhilu1@ncstudents.niagaracollege.ca", "nhrnjez1@ncstudents.niagaracollege.ca", "nisa1@ncstudents.niagaracollege.ca", "jjacoborodrigue1@ncstudents.niagaracollege.ca", "ljakhar2@ncstudents.niagaracollege.ca", "ejames12@ncstudents.niagaracollege.ca", "mjean11@ncstudents.niagaracollege.ca", "fjidelola1@ncstudents.niagaracollege.ca", "yjin13@ncstudents.niagaracollege.ca", "sjohnston37@ncstudents.niagaracollege.ca", "sjohnstonjeffre1@ncstudents.niagaracollege.ca", "lkammegnekamdem1@ncstudents.niagaracollege.ca", "mkari1@ncstudents.niagaracollege.ca", "mkhan45@ncstudents.niagaracollege.ca", "jkim200@ncstudents.niagaracollege.ca", "elittle4@ncstudents.niagaracollege.ca", "vlopezchavez1@ncstudents.niagaracollege.ca", "jlucciola1@ncstudents.niagaracollege.ca", "dmaldonadoburgo1@ncstudents.niagaracollege.ca", "jmartin86@ncstudents.niagaracollege.ca", "jmartinezavila1@ncstudents.niagaracollege.ca", "fmichel1@ncstudents.niagaracollege.ca", "qmohammed2@ncstudents.niagaracollege.ca", "imoreiraromao1@ncstudents.niagaracollege.ca", "cmurguiacozar1@ncstudents.niagaracollege.ca", "jmurikkanolikka1@ncstudents.niagaracollege.ca", "amuro1@ncstudents.niagaracollege.ca", "emyers3@ncstudents.niagaracollege.ca", "mnguyen41@ncstudents.niagaracollege.ca", "enorris1@ncstudents.niagaracollege.ca", "unzekwesi1@ncstudents.niagaracollege.ca", "kobeta1@ncstudents.niagaracollege.ca", "togunlola2@ncstudents.niagaracollege.ca", "dorozco4@ncstudents.niagaracollege.ca", "hpanchal13@ncstudents.niagaracollege.ca", "rpancholi2@ncstudents.niagaracollege.ca", "jparson1@ncstudents.niagaracollege.ca", "bpatel158@ncstudents.niagaracollege.ca", "spatel315@ncstudents.niagaracollege.ca", "vpatel193@ncstudents.niagaracollege.ca", "spaudyal2@ncstudents.niagaracollege.ca", "spaul21@ncstudents.niagaracollege.ca", "nrajaonalison1@ncstudents.niagaracollege.ca", "esandoqa1@ncstudents.niagaracollege.ca", "rsharma255@ncstudents.niagaracollege.ca", "gshrestha1@ncstudents.niagaracollege.ca", "gsingh1409@ncstudents.niagaracollege.ca", "gsingh1241@ncstudents.niagaracollege.ca", "gsingh2220@ncstudents.niagaracollege.ca", "hsingh1382@ncstudents.niagaracollege.ca", "csmallwood3@ncstudents.niagaracollege.ca", "psoriano1@ncstudents.niagaracollege.ca", "ispirleanu1@ncstudents.niagaracollege.ca", "csrigley3@ncstudents.niagaracollege.ca", "dsychevskyi1@ncstudents.niagaracollege.ca", "dtailor1@ncstudents.niagaracollege.ca", "gtatulea1@ncstudents.niagaracollege.ca", "ntemple1@ncstudents.niagaracollege.ca", "jterdik3@ncstudents.niagaracollege.ca", "athompson8@ncstudents.niagaracollege.ca", "ptorresmoreno1@ncstudents.niagaracollege.ca", "avillarrealguti1@ncstudents.niagaracollege.ca", "cwachowiak1@ncstudents.niagaracollege.ca", "cwarner10@ncstudents.niagaracollege.ca", "aweeber1@ncstudents.niagaracollege.ca", "lwilliston1@ncstudents.niagaracollege.ca", "kwood18@ncstudents.niagaracollege.ca", "kzizian1@ncstudents.niagaracollege.ca" };
                foreach(string email in safeEmails)
                {
                    AddUser(userManager, email, "Admin,Security");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetBaseException().Message);
            }
        }

        /// <summary>
        /// Creates the Identity User and adds them to the roles.  
        /// Note that this sets EmailConfirmed to true.
        /// </summary>
        /// <param name="userManager">The UserManager<IdentityUser> </param>
        /// <param name="email">The email for the account.  Will also be used as the UserName</param>
        /// <param name="theRoles">String containing comma separated list of Role names. Omit if no roles</param>
        /// <param name="password">Password if you don't want the default</param>
        private static void AddUser(UserManager<IdentityUser> userManager,
            string email, string theRoles = "", string password = "Pa55w@rd")
        {
            if (userManager.FindByEmailAsync(email).Result == null)
            {
                IdentityUser user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                IdentityResult result = userManager.CreateAsync(user, password).Result;

                if (result.Succeeded)
                {
                    string[] roles = theRoles.Split(',');
                    foreach (var role in roles)
                    {
                        if(roleNames.Contains(role))
                        {
                            userManager.AddToRoleAsync(user, role).Wait();
                        }
                    }
                }
            }
        }
    }
}
