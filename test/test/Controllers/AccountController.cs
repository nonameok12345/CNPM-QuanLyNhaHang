using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly PasswordHasher<NhanVien> _hasher = new();

        public AccountController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.TaiKhoan == model.TaiKhoan);

            // Cố ý dùng chung một thông báo cho cả "sai tài khoản" và "sai mật khẩu"
            // để người lạ không dò ra được tài khoản nào tồn tại
            var hopLe = nv != null &&
                        _hasher.VerifyHashedPassword(nv, nv.MatKhau, model.MatKhau) != PasswordVerificationResult.Failed;
            if (!hopLe)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            var vaiTro = VaiTro.TuChucVu(nv!.ChucVu);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, nv.MaNV.ToString()),
                new(ClaimTypes.Name, nv.HoTen),
                new(ClaimTypes.Role, vaiTro)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Chỉ quay lại địa chỉ nội bộ của web, tránh bị chuyển hướng sang trang lạ
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirectToAction("Index", "QuanTri");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // Đăng nhập rồi nhưng không đủ quyền (ví dụ thu ngân vào trang quản lý)
        public IActionResult AccessDenied() => View();
    }
}