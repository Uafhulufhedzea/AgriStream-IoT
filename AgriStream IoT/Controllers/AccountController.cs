using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using AgriStream_IoT.Models;
using AgriStream_IoT.Data;

namespace AgriStream_IoT.Controllers
{
    public class AccountController : Controller
    {
        private readonly AgriDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AgriDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Account/Register
        public async Task<IActionResult> Register()
        {
            ViewBag.Roles = new SelectList(
                await _context.SystemRoles
                    .Where(role => role.RoleName != "Admin")
                    .ToListAsync(),
                "RoleId",
                "RoleName");
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserAccount model)
        {
            if (await _context.SystemRoles
                .AnyAsync(role => role.RoleId == model.RoleId && role.RoleName == "Admin"))
            {
                ModelState.AddModelError(nameof(model.RoleId), "Administrator accounts cannot be registered here.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.UserAccounts.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["RegistrationMessage"] = $"Account created successfully. Your account ID is {model.UserId}. You can now log in.";
                    return RedirectToAction(nameof(Login));
                }
                catch (DbUpdateException exception) when (exception.InnerException is SqlException)
                {
                    _logger.LogError(exception, "Unable to save a new account for username {Username}.", model.Username);
                    ModelState.AddModelError(string.Empty, "The account could not be created in the database. Verify the database schema and try again.");
                }
            }
            ViewBag.Roles = new SelectList(
                await _context.SystemRoles
                    .Where(role => role.RoleName != "Admin")
                    .ToListAsync(),
                "RoleId",
                "RoleName");
            return View(model);
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password); // Simple matching for exam simplicity

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

                TempData["UserRole"] = user.Role.RoleName;
                TempData["Username"] = user.Username;
                return RedirectToAction("Dashboard", "AgriData");
            }

            ViewBag.Error = "Invalid username or password credentials.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
