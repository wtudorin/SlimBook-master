using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Models;
using SlimBook.Utility;

namespace SlimBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class UserController : Controller
    {
        public readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var userList = _db.ApplicationUser.Include(u => u.Company).ToList();
            var userRole = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in userList)
            {
                var roleId = userRole.FirstOrDefault(ur => ur.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;
                if (user.Company == null)
                {
                    user.Company = new Company() { Name = string.Empty };
                }
            }
            return Json(new { data = userList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var user = _db.ApplicationUser.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = $"User lock/unlock error" });
            }
            bool isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now;
            user.LockoutEnd = isLocked ? DateTime.Now : DateTime.Now.AddYears(1);
            _db.SaveChanges();
            return Json(new { success = true, message = $"User {(isLocked ? " Unlock" : " Lock")} successful." });
        }

        #endregion API Calls
    }
}