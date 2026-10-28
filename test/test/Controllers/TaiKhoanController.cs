using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Đăng ký và đăng nhập dành cho KHÁCH HÀNG (nhân viên dùng AccountController)
    public class TaiKhoanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly PasswordHasher<KhachHang> _hasher = new();

        public TaiKhoanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /TaiKhoan/DangKy
        [HttpGet]
        public IActionResult DangKy(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new DangKyViewModel { ReturnUrl = returnUrl });
        }

        // POST: /TaiKhoan/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DangKyViewModel model)
        {
            // Bỏ khoảng trắng, dấu chấm, gạch ngang trong số điện thoại rồi mới kiểm tra
            var sdt = ChuanHoaSdt(model.SDT);
            if (!Regex.IsMatch(sdt, @"^0\d{9}$"))
            {
                ModelState.AddModelError(nameof(model.SDT), "Số điện thoại gồm 10 chữ số và bắt đầu bằng 0");
            }
            else if (await _context.KhachHangs.AnyAsync(k => k.SDT == sdt))
            {
                ModelState.AddModelError(nameof(model.SDT), "Số điện thoại này đã được đăng ký, hãy đăng nhập");
            }

            if (!ModelState.IsValid) return View(model);

            var khach = new KhachHang
            {
                HoTen = model.HoTen.Trim(),
                SDT = sdt,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim()
            };
            khach.MatKhau = _hasher.HashPassword(khach, model.MatKhau);

            try
            {
                _context.KhachHangs.Add(khach);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người cùng đăng ký một số điện thoại gần như cùng lúc
                ModelState.AddModelError(nameof(model.SDT), "Không tạo được tài khoản, số điện thoại có thể đã được đăng ký");
                return View(model);
            }

            await DangNhapAsync(khach);   // đăng ký xong tự đăng nhập luôn
            return ChuyenHuong(model.ReturnUrl);
        }

        // GET: /TaiKhoan/DangNhap
        [HttpGet]
        public IActionResult DangNhap(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new DangNhapKhachViewModel { ReturnUrl = returnUrl });
        }

        // POST: /TaiKhoan/DangNhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapKhachViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var sdt = ChuanHoaSdt(model.SDT);
            var khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SDT == sdt);

            // Khách mẫu cũ chưa có mật khẩu (MatKhau = NULL) thì không đăng nhập được
            var hopLe = khach != null
                        && !string.IsNullOrEmpty(khach.MatKhau)
                        && _hasher.VerifyHashedPassword(khach, khach.MatKhau, model.MatKhau) != PasswordVerificationResult.Failed;

            if (!hopLe)
            {
                // Dùng chung một thông báo để người lạ không dò ra số nào đã đăng ký
                ModelState.AddModelError("", "Sai số điện thoại hoặc mật khẩu");
                return View(model);
            }

            await DangNhapAsync(khach!);
            return ChuyenHuong(model.ReturnUrl);
        }

        // ---- Hàm phụ ----

        private static string ChuanHoaSdt(string? sdt)
            => Regex.Replace(sdt ?? "", @"[\s\.\-]", "");

        private async Task DangNhapAsync(KhachHang khach)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, khach.MaKH.ToString()),
                new(ClaimTypes.Name, khach.HoTen),
                new(ClaimTypes.Role, VaiTro.KhachHang)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        // Chỉ quay lại địa chỉ nội bộ của web, tránh bị chuyển hướng sang trang lạ
        private IActionResult ChuyenHuong(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}