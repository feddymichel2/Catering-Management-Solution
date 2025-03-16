using CateringManagement.CustomControllers;
using CateringManagement.Data;
using CateringManagement.Models;
using CateringManagement.Utilities;
using CateringManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CateringManagement.Controllers
{
    [Authorize(Roles = "Security")]
    public class UserRoleController : CognizantController
    {
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserRoleController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: User
        public async Task<IActionResult> Index()
        {
            var identityUsers = _context.Users
                .OrderBy(u => u.UserName)
                .AsNoTracking();

            List<UserVM> users = new List<UserVM>();
            foreach (var u in identityUsers)
            {
                UserVM user = new UserVM
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    UserRoles = (List<string>)await _userManager.GetRolesAsync(u)
                };
                users.Add(user);
            }
;
            return View(users);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new BadRequestResult();
            }
            var _user = await _userManager.FindByIdAsync(id);//IdentityRole
            if (_user == null)
            {
                return NotFound();
            }
            UserVM user = new UserVM
            {
                Id = _user.Id,
                UserName = _user.UserName,
                UserRoles = (List<string>)await _userManager.GetRolesAsync(_user)
            };
            PopulateAssignedRoleData(user);
            if (user.UserName == User.Identity.Name)
            {
                ModelState.AddModelError("", "You cannot change your own role assignments.");
                ViewData["NoSubmit"] = "disabled=disabled";
            }
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Id, string[] selectedRoles)
        {
            var _user = await _userManager.FindByIdAsync(Id);//IdentityRole
            UserVM user = new UserVM
            {
                Id = _user.Id,
                UserName = _user.UserName,
                UserRoles = (List<string>)await _userManager.GetRolesAsync(_user)
            };
            if (user.UserName == User.Identity.Name)
            {
                ModelState.AddModelError("", "You cannot change your own role assignments.");
                ViewData["NoSubmit"] = "disabled=disabled";
            }
            else
            {
                try
                {
                    await UpdateUserRoles(selectedRoles, user);
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty,
                                    "Unable to save changes.");
                }
            }
            PopulateAssignedRoleData(user);
            return View(user);
        }

        private void PopulateAssignedRoleData(UserVM user)
        {//Prepare checkboxes for all Roles
            var allRoles = _context.Roles;
            var currentRoles = user.UserRoles;
            var viewModel = new List<RoleVM>();
            foreach (var r in allRoles)
            {
                viewModel.Add(new RoleVM
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    Assigned = currentRoles.Contains(r.Name)
                });
            }
            ViewBag.Roles = viewModel;
        }

        private async Task UpdateUserRoles(string[] selectedRoles, UserVM userToUpdate)
        {
            var UserRoles = userToUpdate.UserRoles;//Current roles use is in
            var _user = await _userManager.FindByIdAsync(userToUpdate.Id);//IdentityUser

            if (selectedRoles == null)
            {
                //No roles selected so just remove any currently assigned
                foreach (var r in UserRoles)
                {
                    await _userManager.RemoveFromRoleAsync(_user, r);
                }
            }
            else
            {
                //At least one role checked so loop through all the roles
                //and add or remove as required

                //We need to do this next line because foreach loops don't always work well
                //for data returned by EF when working async.  Pulling it into an IList<>
                //first means we can safely loop over the colleciton making async calls and avoid
                //the error 'New transaction is not allowed because there are other threads running in the session'
                IList<IdentityRole> allRoles = _context.Roles.ToList<IdentityRole>();

                foreach (var r in allRoles)
                {
                    if (selectedRoles.Contains(r.Name))
                    {
                        if (!UserRoles.Contains(r.Name))
                        {
                            await _userManager.AddToRoleAsync(_user, r.Name);
                        }
                    }
                    else
                    {
                        if (UserRoles.Contains(r.Name))
                        {
                            await _userManager.RemoveFromRoleAsync(_user, r.Name);
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
                _userManager.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
