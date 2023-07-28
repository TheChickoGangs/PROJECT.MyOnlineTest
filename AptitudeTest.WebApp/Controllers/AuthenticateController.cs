using AptitudeTest.WebApp.Data;
using AptitudeTest.WebApp.Models;
using AptitudeTest.WebApp.Models.Enum;
using AptitudeTest.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace AptitudeTest.WebApp.Controllers
{
    public class AuthenticateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthenticateController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel vm)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email.ToLower().Trim() == vm.Email.ToLower().Trim()))
                {
                    ModelState.AddModelError("EmailExistErr", "This Email already Exist");
                    return View();
                }

                if (_context.Users.Any(u => u.PhoneNumber.ToLower().Trim() == vm.PhoneNumber.ToLower().Trim()))
                {
                    ModelState.AddModelError("PhoneExistErr", "This Phone Number already Exist");
                    return View();
                }

                User newUser = new User()
                {
                    Email = vm.Email,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    PhoneNumber = vm.PhoneNumber,
                    BirthDay = vm.BirthDay
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                TempData["Success"] = "You need to wait for MANAGER to Verify Your Infomation";

                return RedirectToAction(nameof(Login));
            }
            else
            {
                return View(vm);
            }
        }


        //Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var getUser = _context.Users.Where(u => u.UserName == vm.UserName).FirstOrDefault();

                if (getUser == null)
                {
                    ModelState.AddModelError("NameErr", "Wrong UserName");
                    return View();
                }

                if (getUser.Password != vm.Password)
                {
                    ModelState.AddModelError("PassErr", "Wrong Password");
                    return View();
                }

                if (getUser.Role == (int)EnumRoles.MANAGER)
                {
                    HttpContext.Session.Set<User>("user", getUser);
                    return RedirectToAction("Dashboard", "Manager", new { area = "MANAGER" });
                }

                if(getUser.IsActive == false)
                {
                    TempData["Message"] = "Please wait to get Login Permision from MANAGER";
                    return RedirectToAction(nameof(Login));
                }

                HttpContext.Session.Set<User>("user", getUser);
                return RedirectToAction("Index", "Candidate", new { area = "CANDIDATE" });
            }
            else
            {
                return View(vm);
            }
        }


        //Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Set<User>("user", null);
            return RedirectToAction(nameof(Login));
        }
    }
}
