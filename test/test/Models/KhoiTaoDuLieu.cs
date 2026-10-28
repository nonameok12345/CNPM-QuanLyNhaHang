using Microsoft.AspNetCore.Identity;

namespace test.Models;

public static class KhoiTaoDuLieu
{
    // Chạy mỗi lần web khởi động: mật khẩu nào còn ở dạng chữ thường (như '123456' trong dữ liệu mẫu)
    // thì mã hóa lại. Mật khẩu đã mã hóa luôn bắt đầu bằng "AQAAAA" nên được bỏ qua.
    public static void MaHoaMatKhauCu(QuanLyNhaHangContext db)
    {
        var hasher = new PasswordHasher<NhanVien>();
        var daDoi = false;

        foreach (var nv in db.NhanViens.ToList())
        {
            if (!nv.MatKhau.StartsWith("AQAAAA"))
            {
                nv.MatKhau = hasher.HashPassword(nv, nv.MatKhau);
                daDoi = true;
            }
        }

        if (daDoi) db.SaveChanges();
    }
}