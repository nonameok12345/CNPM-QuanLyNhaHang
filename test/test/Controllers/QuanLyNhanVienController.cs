using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using test.Models;

namespace test.Controllers
{
    // Quản lý nhân viên: CHỈ người quản lý được vào (lương, mật khẩu là thông tin nhạy cảm)
    [Authorize(Roles = VaiTro.QuanLy)]
    public class QuanLyNhanVienController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly PasswordHasher<NhanVien> _hasher = new();

        public QuanLyNhanVienController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // Mã nhân viên đang đăng nhập (lấy từ cookie, xem AccountController.Login)
        private int? MaNhanVienHienTai =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ma) ? ma : null;

        // GET: /QuanLyNhanVien?q=...&chucVu=...
        public async Task<IActionResult> Index(string? q, string? chucVu)
        {
            var query = _context.NhanViens.Include(n => n.Kho).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var tuKhoa = q.Trim();
                query = query.Where(n => n.HoTen.Contains(tuKhoa)
                                      || n.TaiKhoan.Contains(tuKhoa)
                                      || (n.SDT != null && n.SDT.Contains(tuKhoa)));
            }
            if (!string.IsNullOrWhiteSpace(chucVu))
                query = query.Where(n => n.ChucVu == chucVu);

            var nhanViens = await query.OrderBy(n => n.HoTen).ToListAsync();

            // Đếm số việc đang làm của từng người (chưa có ngày kết thúc hoặc chưa tới ngày kết thúc)
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var soViec = await _context.PhanCongs
                .Where(p => p.NgayKetThuc == null || p.NgayKetThuc >= homNay)
                .GroupBy(p => p.MaNV)
                .Select(g => new { MaNV = g.Key, So = g.Count() })
                .ToDictionaryAsync(x => x.MaNV, x => x.So);

            var danhSach = nhanViens.Select(n => new NhanVienDong
            {
                NhanVien = n,
                SoViecDangLam = soViec.GetValueOrDefault(n.MaNV),
                LaTaiKhoanHienTai = n.MaNV == MaNhanVienHienTai
            }).ToList();

            ViewBag.TuKhoa = q ?? "";
            ViewBag.ChucVu = chucVu ?? "";
            ViewBag.DanhSachChucVu = await LayDanhSachChucVuAsync(null);
            return View(danhSach);
        }

        // GET: /QuanLyNhanVien/Create
        public async Task<IActionResult> Create()
        {
            var model = new NhanVienFormViewModel { ChucVu = "Phục vụ" };
            await NapDanhSachAsync(model);
            return View("Form", model);
        }

        // POST: /QuanLyNhanVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVienFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MatKhau))
                ModelState.AddModelError(nameof(model.MatKhau), "Vui lòng nhập mật khẩu");

            await KiemTraThemAsync(model, null);
            if (!ModelState.IsValid)
            {
                await NapDanhSachAsync(model);
                return View("Form", model);
            }

            var nv = new NhanVien
            {
                HoTen = model.HoTen.Trim(),
                ChucVu = model.ChucVu.Trim(),
                SDT = string.IsNullOrWhiteSpace(model.SDT) ? null : model.SDT.Trim(),
                NgayVaoLam = model.NgayVaoLam,
                TaiKhoan = model.TaiKhoan.Trim(),
                Luong = model.Luong,
                MaKho = model.MaKho
            };
            // Mật khẩu luôn được mã hóa trước khi lưu (giống lúc đăng nhập kiểm tra bằng PasswordHasher)
            nv.MatKhau = _hasher.HashPassword(nv, model.MatKhau!);
            _context.NhanViens.Add(nv);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được nhân viên, vui lòng thử lại.");
                await NapDanhSachAsync(model);
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã thêm nhân viên {nv.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /QuanLyNhanVien/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            var model = new NhanVienFormViewModel
            {
                MaNV = nv.MaNV,
                HoTen = nv.HoTen,
                ChucVu = nv.ChucVu,
                SDT = nv.SDT,
                NgayVaoLam = nv.NgayVaoLam,
                TaiKhoan = nv.TaiKhoan,
                Luong = nv.Luong,
                MaKho = nv.MaKho
            };
            await NapDanhSachAsync(model);
            return View("Form", model);
        }

        // POST: /QuanLyNhanVien/Edit — mã nhân viên nằm trong ô ẩn của form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NhanVienFormViewModel model)
        {
            var nv = await _context.NhanViens.FindAsync(model.MaNV);
            if (nv == null) return NotFound();

            await KiemTraThemAsync(model, nv);
            if (!ModelState.IsValid)
            {
                await NapDanhSachAsync(model);
                return View("Form", model);
            }

            nv.HoTen = model.HoTen.Trim();
            nv.ChucVu = model.ChucVu.Trim();
            nv.SDT = string.IsNullOrWhiteSpace(model.SDT) ? null : model.SDT.Trim();
            nv.NgayVaoLam = model.NgayVaoLam;
            nv.TaiKhoan = model.TaiKhoan.Trim();
            nv.Luong = model.Luong;
            nv.MaKho = model.MaKho;

            // Chỉ đổi mật khẩu khi quản lý có nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(model.MatKhau))
                nv.MatKhau = _hasher.HashPassword(nv, model.MatKhau);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được thay đổi, vui lòng thử lại.");
                await NapDanhSachAsync(model);
                return View("Form", model);
            }

            TempData["ThongBao"] = $"Đã cập nhật nhân viên {nv.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /QuanLyNhanVien/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            if (id == MaNhanVienHienTai)
            {
                TempData["Loi"] = "Bạn không thể xóa chính tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            // Nhân viên đã lập hóa đơn / phiếu nhập thì không xóa được (sẽ làm hỏng số liệu cũ)
            var daCoGiaoDich = await _context.HoaDons.AnyAsync(h => h.MaNV == id)
                            || await _context.PhieuNhaps.AnyAsync(p => p.MaNV == id);
            if (daCoGiaoDich)
            {
                TempData["Loi"] = $"{nv.HoTen} đã lập hóa đơn hoặc phiếu nhập nên không xóa được. " +
                                  "Hãy đổi mật khẩu để vô hiệu hóa tài khoản này.";
                return RedirectToAction(nameof(Index));
            }

            // Các phân công công việc của người này đi theo (không còn ý nghĩa nữa)
            var phanCongs = await _context.PhanCongs.Where(p => p.MaNV == id).ToListAsync();
            _context.PhanCongs.RemoveRange(phanCongs);
            _context.NhanViens.Remove(nv);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = $"Không xóa được {nv.HoTen} vì còn dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ThongBao"] = $"Đã xóa nhân viên {nv.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        // ================= Phân công công việc =================

        // GET: /QuanLyNhanVien/PhanCong/5
        public async Task<IActionResult> PhanCong(int? id)
        {
            if (id == null) return NotFound();

            var nv = await _context.NhanViens.Include(n => n.Kho).FirstOrDefaultAsync(n => n.MaNV == id);
            if (nv == null) return NotFound();

            var danhSach = await _context.PhanCongs
                .Include(p => p.CongViec)
                .Where(p => p.MaNV == id)
                .OrderByDescending(p => p.NgayBatDau)
                .ToListAsync();

            var daGiao = danhSach.Select(p => p.MaCV).ToList();
            var chuaGiao = await _context.CongViecs
                .Where(c => !daGiao.Contains(c.MaCV))
                .OrderBy(c => c.TenCV)
                .ToListAsync();

            return View(new PhanCongViewModel { NhanVien = nv, DanhSach = danhSach, CongViecChuaGiao = chuaGiao });
        }

        // POST: /QuanLyNhanVien/GiaoViec
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaoViec(int maNV, int maCV, DateOnly? ngayBatDau, DateOnly? ngayKetThuc)
        {
            if (!await _context.NhanViens.AnyAsync(n => n.MaNV == maNV)) return NotFound();

            var congViec = await _context.CongViecs.FindAsync(maCV);
            if (congViec == null)
            {
                TempData["Loi"] = "Vui lòng chọn công việc cần giao.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }

            var bd = ngayBatDau ?? DateOnly.FromDateTime(DateTime.Today);
            if (ngayKetThuc != null && ngayKetThuc < bd)
            {
                TempData["Loi"] = "Ngày kết thúc không được trước ngày bắt đầu.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }

            // Khóa của bảng PhanCong là (MaNV, MaCV) nên mỗi người chỉ có một dòng cho mỗi công việc
            if (await _context.PhanCongs.AnyAsync(p => p.MaNV == maNV && p.MaCV == maCV))
            {
                TempData["Loi"] = $"Công việc \"{congViec.TenCV}\" đã được giao cho nhân viên này.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }

            _context.PhanCongs.Add(new PhanCong
            {
                MaNV = maNV,
                MaCV = maCV,
                NgayBatDau = bd,
                NgayKetThuc = ngayKetThuc
            });
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã giao việc \"{congViec.TenCV}\".";
            return RedirectToAction(nameof(PhanCong), new { id = maNV });
        }

        // POST: /QuanLyNhanVien/KetThucViec — đánh dấu công việc kết thúc từ hôm nay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KetThucViec(int maNV, int maCV)
        {
            var pc = await _context.PhanCongs.FindAsync(maNV, maCV);
            if (pc == null) return NotFound();

            pc.NgayKetThuc = DateOnly.FromDateTime(DateTime.Today);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã kết thúc công việc.";
            return RedirectToAction(nameof(PhanCong), new { id = maNV });
        }

        // POST: /QuanLyNhanVien/GoPhanCong — xóa hẳn dòng phân công (giao nhầm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoPhanCong(int maNV, int maCV)
        {
            var pc = await _context.PhanCongs.FindAsync(maNV, maCV);
            if (pc == null) return NotFound();

            _context.PhanCongs.Remove(pc);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gỡ phân công.";
            return RedirectToAction(nameof(PhanCong), new { id = maNV });
        }

        // POST: /QuanLyNhanVien/TaoCongViec — tạo nhanh một công việc mới để giao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoCongViec(int maNV, string? tenCV, string? moTa)
        {
            tenCV = tenCV?.Trim();
            if (string.IsNullOrEmpty(tenCV) || tenCV.Length > 100)
            {
                TempData["Loi"] = "Tên công việc bắt buộc và tối đa 100 ký tự.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }
            moTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();
            if (moTa?.Length > 255)
            {
                TempData["Loi"] = "Mô tả tối đa 255 ký tự.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }

            if (await _context.CongViecs.AnyAsync(c => c.TenCV == tenCV))
            {
                TempData["Loi"] = $"Công việc \"{tenCV}\" đã tồn tại, hãy chọn trong danh sách.";
                return RedirectToAction(nameof(PhanCong), new { id = maNV });
            }

            _context.CongViecs.Add(new CongViec { TenCV = tenCV, MoTa = moTa });
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo công việc \"{tenCV}\". Giờ bạn có thể giao việc này.";
            return RedirectToAction(nameof(PhanCong), new { id = maNV });
        }

        // ---- Hàm phụ ----

        // Danh sách chức vụ cho ô chọn: các chức vụ gợi ý + chức vụ đang có trong database
        private async Task<List<string>> LayDanhSachChucVuAsync(string? them)
        {
            var dangCo = await _context.NhanViens.Select(n => n.ChucVu).Distinct().ToListAsync();
            var ds = ChucVuNhanVien.TatCa.ToList();
            foreach (var cv in dangCo.Append(them ?? ""))
            {
                if (!string.IsNullOrWhiteSpace(cv) && !ds.Contains(cv)) ds.Add(cv);
            }
            return ds;
        }

        private async Task NapDanhSachAsync(NhanVienFormViewModel model)
        {
            model.DanhSachChucVu = await LayDanhSachChucVuAsync(model.ChucVu);
            model.DanhSachKho = await _context.Khos
                .OrderBy(k => k.TenKho)
                .Select(k => new SelectListItem(k.TenKho, k.MaKho.ToString()))
                .ToListAsync();
        }

        // Những kiểm tra mà DataAnnotations không làm được: tài khoản trùng, kho có thật, không bỏ trống người quản lý
        private async Task KiemTraThemAsync(NhanVienFormViewModel model, NhanVien? hienTai)
        {
            var taiKhoan = model.TaiKhoan?.Trim() ?? "";
            if (taiKhoan.Length > 0 &&
                await _context.NhanViens.AnyAsync(n => n.TaiKhoan == taiKhoan && n.MaNV != model.MaNV))
            {
                ModelState.AddModelError(nameof(model.TaiKhoan), $"Tài khoản \"{taiKhoan}\" đã có người dùng.");
            }

            if (model.MaKho != null && !await _context.Khos.AnyAsync(k => k.MaKho == model.MaKho))
                ModelState.AddModelError(nameof(model.MaKho), "Kho không tồn tại.");

            // Không cho hạ chức người quản lý cuối cùng, nếu không sẽ không còn ai vào được trang quản lý
            if (hienTai != null
                && VaiTro.TuChucVu(hienTai.ChucVu) == VaiTro.QuanLy
                && VaiTro.TuChucVu(model.ChucVu ?? "") != VaiTro.QuanLy)
            {
                var chucVus = await _context.NhanViens.Select(n => n.ChucVu).ToListAsync();
                if (chucVus.Count(cv => VaiTro.TuChucVu(cv) == VaiTro.QuanLy) <= 1)
                {
                    ModelState.AddModelError(nameof(model.ChucVu),
                        "Đây là người quản lý duy nhất, hãy tạo thêm một quản lý khác trước khi đổi chức vụ.");
                }
            }
        }
    }
}
