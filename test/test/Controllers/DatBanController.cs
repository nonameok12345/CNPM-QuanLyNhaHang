using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Controllers
{
    // Đặt bàn dành cho khách hàng: phải đăng nhập bằng tài khoản khách
    [Authorize(Roles = VaiTro.KhachHang)]
    public class DatBanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        // Mỗi lúc chỉ một người được "kiểm tra bàn trống rồi ghi lịch đặt", để hai người
        // bấm đặt cùng một bàn cùng lúc không bị trùng. Web chạy một tiến trình nên cách này đủ dùng.
        // (Khóa này dùng chung với TaiQuanController, xem KhoaDatBan.)
        private static readonly SemaphoreSlim _khoaDatBan = KhoaDatBan.Khoa;

        public DatBanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        private int LayMaKH() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET: /DatBan — chọn ngày, giờ, số người; bấm tìm thì hiện các bàn còn trống
        public async Task<IActionResult> Index(DateTime? ngay, string? gio, int? soNguoi)
        {
            var vm = new TimBanViewModel
            {
                Ngay = ngay ?? DateTime.Today,
                Gio = gio ?? "19:00",
                SoNguoi = soNguoi ?? 2
            };

            // Chỉ tìm khi khách đã bấm "Tìm bàn trống" (có đủ 3 thông tin)
            if (ngay.HasValue && !string.IsNullOrEmpty(gio) && soNguoi.HasValue)
            {
                var loi = KiemTra(ngay.Value, gio, soNguoi.Value, out var batDau);
                if (loi != null)
                    vm.Loi = loi;
                else
                    vm.BanTrong = await LayBanTrongAsync(batDau, soNguoi.Value);
            }

            return View(vm);
        }

        // POST: /DatBan/Tao — khách bấm "Chọn bàn này"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tao(DateTime ngay, string gio, int soNguoi, int maBan)
        {
            var timLai = new { ngay = ngay.ToString("yyyy-MM-dd"), gio, soNguoi };

            var loi = KiemTra(ngay, gio, soNguoi, out var batDau);
            if (loi != null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction(nameof(Index), timLai);
            }

            var maKH = LayMaKH();

            await _khoaDatBan.WaitAsync();
            try
            {
                var hetHan = DateTime.Now.AddHours(-QuyDinhDatBan.ThoiLuongGio);
                var soLichDangGiu = await _context.DatBans.CountAsync(d =>
                    d.MaKH == maKH && d.TrangThai != TrangThaiDatBan.DaHuy && d.NgayGio > hetHan);
                if (soLichDangGiu >= QuyDinhDatBan.SoLichDangGiuToiDa)
                {
                    TempData["Loi"] = $"Bạn đang giữ {soLichDangGiu} lịch đặt bàn. Hãy hủy bớt lịch không dùng trước khi đặt thêm.";
                    return RedirectToAction(nameof(Index), timLai);
                }

                var banTrong = await LayBanTrongAsync(batDau, soNguoi);
                if (!banTrong.Any(b => b.MaBan == maBan))
                {
                    TempData["Loi"] = "Rất tiếc, bàn này vừa có người đặt hoặc không phù hợp. Vui lòng chọn bàn khác.";
                    return RedirectToAction(nameof(Index), timLai);
                }

                var datBan = new DatBan
                {
                    MaKH = maKH,
                    MaBan = maBan,
                    NgayGio = batDau,
                    SoNguoi = soNguoi,
                    TrangThai = TrangThaiDatBan.DaXacNhan   // hệ thống đã kiểm tra bàn trống nên xác nhận luôn
                };
                _context.DatBans.Add(datBan);
                await _context.SaveChangesAsync();

                TempData["MaDatBan"] = datBan.MaDatBan;
                return RedirectToAction(nameof(ThanhCong));
            }
            catch (DbUpdateException)
            {
                TempData["Loi"] = "Không lưu được lịch đặt bàn, vui lòng thử lại.";
                return RedirectToAction(nameof(Index), timLai);
            }
            finally
            {
                _khoaDatBan.Release();
            }
        }

        // GET: /DatBan/ThanhCong — báo đặt bàn xong, mời khách chọn món
        public async Task<IActionResult> ThanhCong()
        {
            if (TempData.Peek("MaDatBan") is not int maDatBan)
                return RedirectToAction(nameof(CuaToi));

            var maKH = LayMaKH();
            var datBan = await _context.DatBans
                .Include(d => d.Ban)
                .FirstOrDefaultAsync(d => d.MaDatBan == maDatBan && d.MaKH == maKH);
            if (datBan == null) return RedirectToAction(nameof(CuaToi));

            return View(datBan);
        }

        // GET: /DatBan/CuaToi — các lịch đặt bàn của khách đang đăng nhập
        public async Task<IActionResult> CuaToi()
        {
            var maKH = LayMaKH();
            var danhSach = await _context.DatBans
                .Include(d => d.Ban)
                .Where(d => d.MaKH == maKH)
                .OrderByDescending(d => d.NgayGio)
                .Take(50)
                .ToListAsync();

            return View(danhSach);
        }

        // POST: /DatBan/Huy/5 — hủy lịch (chỉ lịch của chính mình và chưa tới giờ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var maKH = LayMaKH();
            var datBan = await _context.DatBans.FirstOrDefaultAsync(d => d.MaDatBan == id && d.MaKH == maKH);
            if (datBan == null) return NotFound();

            if (datBan.TrangThai != TrangThaiDatBan.DaHuy && datBan.NgayGio > DateTime.Now)
            {
                datBan.TrangThai = TrangThaiDatBan.DaHuy;

                // Món đã gọi trước cho lịch này cũng hủy theo
                var hoaDons = await _context.HoaDons
                    .Where(h => h.MaDatBan == id && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
                    .ToListAsync();
                foreach (var hd in hoaDons) hd.TrangThai = TrangThaiHoaDon.DaHuy;

                await _context.SaveChangesAsync();
                TempData["ThongBao"] = "Đã hủy lịch đặt bàn.";
            }
            else
            {
                TempData["Loi"] = "Không thể hủy lịch này (đã hủy hoặc đã qua giờ).";
            }
            return RedirectToAction(nameof(CuaToi));
        }

        // ---- Hàm phụ ----

        // Kiểm tra thông tin khách nhập; trả về câu báo lỗi, hoặc null nếu hợp lệ
        private static string? KiemTra(DateTime ngay, string gio, int soNguoi, out DateTime batDau)
        {
            batDau = default;

            if (soNguoi < 1 || soNguoi > QuyDinhDatBan.SoNguoiToiDa)
                return $"Số người phải từ 1 đến {QuyDinhDatBan.SoNguoiToiDa}.";

            if (!QuyDinhDatBan.DanhSachGio().Contains(gio) || !TimeSpan.TryParse(gio, out var gioChon))
                return "Giờ đặt không hợp lệ.";

            batDau = ngay.Date + gioChon;

            if (batDau < DateTime.Now.AddMinutes(QuyDinhDatBan.DatTruocToiThieuPhut))
                return $"Vui lòng đặt trước ít nhất {QuyDinhDatBan.DatTruocToiThieuPhut} phút.";

            if (batDau.Date > DateTime.Today.AddDays(QuyDinhDatBan.DatTruocToiDaNgay))
                return $"Chỉ nhận đặt bàn trong vòng {QuyDinhDatBan.DatTruocToiDaNgay} ngày tới.";

            return null;
        }

        // Một bàn bị coi là "bận" nếu đã có lịch (chưa hủy) bắt đầu cách giờ muốn đặt dưới 2 tiếng
        // (trước hoặc sau), vì hai lượt ăn sẽ chồng lên nhau
        private async Task<List<Ban>> LayBanTrongAsync(DateTime batDau, int soNguoi)
        {
            var tu = batDau.AddHours(-QuyDinhDatBan.ThoiLuongGio);
            var den = batDau.AddHours(QuyDinhDatBan.ThoiLuongGio);

            var banDaDat = await _context.DatBans
                .Where(d => d.TrangThai != TrangThaiDatBan.DaHuy && d.NgayGio > tu && d.NgayGio < den)
                .Select(d => d.MaBan)
                .ToListAsync();

            // Bàn đang có khách tại quán (hóa đơn chưa thanh toán, không gắn lịch đặt: do khách quét QR
            // hoặc nhân viên mở bàn) cũng bận. Tính cùng cửa sổ 2 tiếng như lịch đặt.
            var banDangCoKhach = await _context.HoaDons
                .Where(h => h.MaDatBan == null
                         && h.TrangThai == TrangThaiHoaDon.ChuaThanhToan
                         && h.NgayLap > tu && h.NgayLap < den)
                .Select(h => h.MaBan)
                .ToListAsync();
            banDaDat.AddRange(banDangCoKhach);

            return await _context.Bans
                .Where(b => b.SoChoNgoi >= soNguoi
                         && b.TrangThai != TrangThaiBan.NgungSuDung
                         && !banDaDat.Contains(b.MaBan))
                .OrderBy(b => b.SoChoNgoi).ThenBy(b => b.SoBan)
                .ToListAsync();
        }
    }
}