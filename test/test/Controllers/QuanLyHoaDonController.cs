using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Hóa đơn và thanh toán: dành cho nhân viên và quản lý
    [Authorize(Roles = VaiTro.QuanLy + "," + VaiTro.NhanVien)]
    public class QuanLyHoaDonController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        // Mỗi lúc chỉ một thu ngân được sửa / thanh toán hóa đơn, để hai người bấm cùng lúc
        // không làm thu thừa tiền hoặc sai tổng tiền. Web chạy một tiến trình nên cách này đủ dùng.
        private static readonly SemaphoreSlim _khoa = new(1, 1);

        public QuanLyHoaDonController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        private int LayMaNV() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET: /QuanLyHoaDon — danh sách hóa đơn. Mặc định chỉ hiện hóa đơn "Chưa thanh toán".
        public async Task<IActionResult> Index(DateTime? ngay, string? trangThai)
        {
            // Chưa có tham số trangThai trên địa chỉ (mới vào trang) thì lọc "Chưa thanh toán";
            // chọn "Tất cả" thì tham số có mặt nhưng rỗng
            if (!Request.Query.ContainsKey("trangThai"))
                trangThai = TrangThaiHoaDon.ChuaThanhToan;

            var query = _context.HoaDons.Include(h => h.Ban).AsQueryable();

            if (ngay.HasValue)
            {
                var tu = ngay.Value.Date;
                var den = tu.AddDays(1);
                query = query.Where(h => h.NgayLap >= tu && h.NgayLap < den);
            }
            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(h => h.TrangThai == trangThai);

            var hoaDons = await query.OrderByDescending(h => h.NgayLap).Take(100).ToListAsync();

            // Lấy tiền đã trả và tên khách bằng hai câu truy vấn riêng cho dễ đọc
            var maHDs = hoaDons.Select(h => h.MaHD).ToList();
            var daTra = await _context.ThanhToans
                .Where(t => maHDs.Contains(t.MaHD) && t.TrangThai == TrangThaiThanhToan.ThanhCong)
                .GroupBy(t => t.MaHD)
                .Select(g => new { MaHD = g.Key, Tong = g.Sum(x => x.SoTien) })
                .ToDictionaryAsync(x => x.MaHD, x => x.Tong);

            var maKHs = hoaDons.Where(h => h.MaKH != null).Select(h => h.MaKH!.Value).Distinct().ToList();
            var tenKhach = await _context.KhachHangs
                .Where(k => maKHs.Contains(k.MaKH))
                .ToDictionaryAsync(k => k.MaKH, k => k.HoTen);

            var danhSach = hoaDons.Select(h => new HoaDonDong
            {
                MaHD = h.MaHD,
                SoBan = h.Ban.SoBan,
                TenKhach = h.MaKH != null && tenKhach.TryGetValue(h.MaKH.Value, out var ten) ? ten : "Khách vãng lai",
                NgayLap = h.NgayLap,
                TongTien = h.TongTien,
                DaTra = daTra.GetValueOrDefault(h.MaHD),
                TrangThai = h.TrangThai
            }).ToList();

            ViewBag.Ngay = ngay;
            ViewBag.TrangThai = trangThai;
            return View(danhSach);
        }

        // GET: /QuanLyHoaDon/Tao — chọn bàn để lập hóa đơn cho khách vãng lai
        public async Task<IActionResult> Tao()
        {
            var bans = await _context.Bans
                .Where(b => b.TrangThai != TrangThaiBan.NgungSuDung)
                .OrderBy(b => b.SoBan)
                .ToListAsync();
            return View(bans);
        }

        // POST: /QuanLyHoaDon/Tao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tao(int maBan)
        {
            var ban = await _context.Bans.FindAsync(maBan);
            if (ban == null || ban.TrangThai == TrangThaiBan.NgungSuDung)
            {
                TempData["Loi"] = "Bàn không hợp lệ.";
                return RedirectToAction(nameof(Tao));
            }

            // Bàn này đã có hóa đơn khách vãng lai chưa thanh toán thì mở lại, tránh tạo trùng
            var daCo = await _context.HoaDons.FirstOrDefaultAsync(h =>
                h.MaBan == maBan && h.MaDatBan == null && h.MaKH == null
                && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan);
            if (daCo != null)
                return RedirectToAction(nameof(Details), new { id = daCo.MaHD });

            var hoaDon = new HoaDon
            {
                MaBan = maBan,
                MaNV = LayMaNV(),
                NgayLap = DateTime.Now,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan
            };
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = hoaDon.MaHD });
        }

        // GET: /QuanLyHoaDon/Details/5 — chi tiết món, thanh toán của một hóa đơn
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vm = await TaoViewModelAsync(id.Value);
            if (vm == null) return NotFound();

            return View(vm);
        }

        // POST: /QuanLyHoaDon/ThemMon — nhân viên gọi thêm món cho hóa đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ThemMon(int id, int maMon, int soLuong)
            => TrongKhoa(async () =>
            {
                var hd = await LayHoaDonMoAsync(id);
                if (hd == null) return QuayLai(id);

                if (soLuong < 1 || soLuong > 99)
                {
                    TempData["Loi"] = "Số lượng phải từ 1 đến 99.";
                    return QuayLai(id);
                }

                var mon = await _context.MonAns.FindAsync(maMon);
                if (mon == null)
                {
                    TempData["Loi"] = "Món ăn không tồn tại.";
                    return QuayLai(id);
                }

                var dong = hd.ChiTietHoaDons.FirstOrDefault(c => c.MaMon == maMon);
                if (dong == null)
                {
                    // Lấy giá từ database tại lúc gọi, để sau này đổi giá không làm sai hóa đơn
                    hd.ChiTietHoaDons.Add(new ChiTietHoaDon { MaMon = maMon, SoLuong = soLuong, DonGia = mon.Gia ?? 0 });
                }
                else
                {
                    dong.SoLuong = Math.Min(dong.SoLuong + soLuong, 99);
                }

                hd.TongTien = hd.ChiTietHoaDons.Sum(c => c.SoLuong * c.DonGia);
                await LuuAsync();
                return QuayLai(id);
            });

        // POST: /QuanLyHoaDon/DoiSoLuong — sửa số lượng một món; số lượng 0 nghĩa là xóa món đó
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> DoiSoLuong(int id, int maMon, int soLuong)
            => TrongKhoa(async () =>
            {
                var hd = await LayHoaDonMoAsync(id);
                if (hd == null) return QuayLai(id);

                if (soLuong < 0 || soLuong > 99)
                {
                    TempData["Loi"] = "Số lượng phải từ 0 đến 99 (0 là xóa món).";
                    return QuayLai(id);
                }

                var dong = hd.ChiTietHoaDons.FirstOrDefault(c => c.MaMon == maMon);
                if (dong == null) return QuayLai(id);

                // Tính tổng mới trước khi thay đổi gì cả
                var tongMoi = hd.ChiTietHoaDons.Sum(c => c == dong ? soLuong * c.DonGia : c.SoLuong * c.DonGia);

                // Không cho giảm tổng tiền xuống thấp hơn số tiền khách đã trả
                var daTra = await TinhDaTraAsync(id);
                if (tongMoi < daTra)
                {
                    TempData["Loi"] = $"Khách đã trả {daTra:N0} đ nên tổng tiền không thể thấp hơn số này.";
                    return QuayLai(id);
                }

                if (soLuong == 0)
                    _context.ChiTietHoaDons.Remove(dong);
                else
                    dong.SoLuong = soLuong;

                hd.TongTien = tongMoi;
                await LuuAsync();
                return QuayLai(id);
            });

        // POST: /QuanLyHoaDon/ThemThanhToan — ghi một lần khách trả tiền
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ThemThanhToan(int id, decimal soTien, string phuongThuc)
            => TrongKhoa(async () =>
            {
                var hd = await LayHoaDonMoAsync(id);
                if (hd == null) return QuayLai(id);

                if (!PhuongThucThanhToan.TatCa.Contains(phuongThuc))
                {
                    TempData["Loi"] = "Hình thức thanh toán không hợp lệ.";
                    return QuayLai(id);
                }
                if (hd.TongTien <= 0)
                {
                    TempData["Loi"] = "Hóa đơn chưa có món nào nên chưa thể thanh toán.";
                    return QuayLai(id);
                }

                var daThanhToan = await _context.ThanhToans
                    .Where(t => t.MaHD == id && t.TrangThai == TrangThaiThanhToan.ThanhCong)
                    .ToListAsync();
                var daTra = daThanhToan.Sum(t => t.SoTien);
                var conLai = hd.TongTien - daTra;

                if (soTien <= 0 || soTien > conLai || soTien != decimal.Truncate(soTien))
                {
                    TempData["Loi"] = $"Số tiền phải là số nguyên từ 1 đến {conLai:N0} đ (số tiền còn lại).";
                    return QuayLai(id);
                }

                _context.ThanhToans.Add(new ThanhToan
                {
                    MaHD = id,
                    NgayTT = DateTime.Now,
                    SoTien = soTien,
                    PhuongThuc = phuongThuc,
                    TrangThai = TrangThaiThanhToan.ThanhCong
                });

                // Gắn tên nhân viên thu ngân vào hóa đơn (hóa đơn khách tự đặt lúc đầu chưa có nhân viên)
                hd.MaNV ??= LayMaNV();

                // Trả đủ thì đóng hóa đơn
                string thongBao;
                if (daTra + soTien >= hd.TongTien)
                {
                    hd.TrangThai = TrangThaiHoaDon.DaThanhToan;
                    var cacHinhThuc = daThanhToan.Select(t => t.PhuongThuc).Append(phuongThuc).Distinct().ToList();
                    hd.PhuongThucTT = cacHinhThuc.Count == 1 ? cacHinhThuc[0] : PhuongThucThanhToan.KetHop;
                    thongBao = "Đã thanh toán đủ. Hóa đơn đã đóng.";

                    // Trừ nguyên liệu theo công thức món; lưu cùng lúc với thanh toán ở LuuAsync bên dưới
                    var kq = await TruTonKho.TruChoHoaDonAsync(_context, hd);
                    if (kq.CoTru) thongBao += " Đã trừ tồn kho theo công thức món.";
                    if (kq.SoMonChuaCoCongThuc > 0)
                        thongBao += $" {kq.SoMonChuaCoCongThuc} món chưa có công thức nên chưa trừ kho.";
                    if (kq.Thieu.Count > 0)
                        thongBao += " Lưu ý tồn kho không đủ: " + string.Join("; ", kq.Thieu) + ".";
                }
                else
                {
                    thongBao = $"Đã ghi nhận {soTien:N0} đ. Còn lại {conLai - soTien:N0} đ.";
                }

                if (await LuuAsync()) TempData["ThongBao"] = thongBao;
                return QuayLai(id);
            });

        // POST: /QuanLyHoaDon/Huy/5 — hủy hóa đơn (chỉ khi khách chưa trả đồng nào)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Huy(int id)
            => TrongKhoa(async () =>
            {
                var hd = await LayHoaDonMoAsync(id);
                if (hd == null) return QuayLai(id);

                if (await TinhDaTraAsync(id) > 0)
                {
                    TempData["Loi"] = "Hóa đơn đã có thanh toán nên không hủy được.";
                    return QuayLai(id);
                }

                hd.TrangThai = TrangThaiHoaDon.DaHuy;
                if (!await LuuAsync()) return QuayLai(id);

                TempData["ThongBao"] = "Đã hủy hóa đơn.";
                return RedirectToAction(nameof(Index));
            });

        // ---- Hàm phụ ----

        // Chạy một việc trong "khóa" (mỗi lúc chỉ một người), xong thì luôn mở khóa
        private async Task<IActionResult> TrongKhoa(Func<Task<IActionResult>> viec)
        {
            await _khoa.WaitAsync();
            try
            {
                return await viec();
            }
            finally
            {
                _khoa.Release();
            }
        }

        private IActionResult QuayLai(int id) => RedirectToAction(nameof(Details), new { id });

        // Lấy hóa đơn còn mở (chưa thanh toán đủ, chưa hủy); nếu không được thì báo lỗi và trả null
        private async Task<HoaDon?> LayHoaDonMoAsync(int id)
        {
            var hd = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.MaHD == id);

            if (hd == null || hd.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
            {
                TempData["Loi"] = "Hóa đơn không tồn tại hoặc đã đóng, không thể thay đổi.";
                return null;
            }
            return hd;
        }

        private async Task<decimal> TinhDaTraAsync(int maHD)
        {
            var tong = await _context.ThanhToans
                .Where(t => t.MaHD == maHD && t.TrangThai == TrangThaiThanhToan.ThanhCong)
                .SumAsync(t => (decimal?)t.SoTien);
            return tong ?? 0;
        }

        // Lưu thay đổi; trả về true nếu thành công, false (kèm thông báo lỗi) nếu thất bại
        private async Task<bool> LuuAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = "Không lưu được thay đổi, vui lòng thử lại.";
                return false;
            }
        }

        private async Task<HoaDonChiTietViewModel?> TaoViewModelAsync(int id)
        {
            var hd = await _context.HoaDons
                .Include(h => h.Ban)
                .Include(h => h.DatBan)
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(h => h.MaHD == id);
            if (hd == null) return null;

            var vm = new HoaDonChiTietViewModel
            {
                HoaDon = hd,
                ThanhToans = await _context.ThanhToans
                    .Where(t => t.MaHD == id)
                    .OrderBy(t => t.NgayTT)
                    .ToListAsync(),
                MonAnList = await _context.MonAns.OrderBy(m => m.TenMon).ToListAsync()
            };

            if (hd.MaKH != null)
            {
                var kh = await _context.KhachHangs.FindAsync(hd.MaKH.Value);
                if (kh != null)
                {
                    vm.TenKhach = kh.HoTen;
                    vm.SDTKhach = kh.SDT;
                }
            }
            if (hd.MaNV != null)
                vm.TenNhanVien = (await _context.NhanViens.FindAsync(hd.MaNV.Value))?.HoTen;

            return vm;
        }
    }
}
