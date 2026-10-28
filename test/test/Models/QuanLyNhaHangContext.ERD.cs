using Microsoft.EntityFrameworkCore;

namespace test.Models;

// File này mở rộng QuanLyNhaHangContext (class gốc đã khai báo "partial"),
// nên KHÔNG cần sửa file QuanLyNhaHangContext.cs cũ.
public partial class QuanLyNhaHangContext
{
    public virtual DbSet<ThanhToan> ThanhToans { get; set; } = null!;
    public virtual DbSet<CongViec> CongViecs { get; set; } = null!;
    public virtual DbSet<PhanCong> PhanCongs { get; set; } = null!;
    public virtual DbSet<Kho> Khos { get; set; } = null!;
    public virtual DbSet<NguyenLieu> NguyenLieus { get; set; } = null!;
    public virtual DbSet<TonKho> TonKhos { get; set; } = null!;
    public virtual DbSet<CtNguyenLieu> CtNguyenLieus { get; set; } = null!;
    public virtual DbSet<PhieuNhap> PhieuNhaps { get; set; } = null!;
    public virtual DbSet<CtKhoNhap> CtKhoNhaps { get; set; } = null!;

    // Hàm này được khai báo sẵn (partial) ở cuối OnModelCreating của file gốc
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // ----- Cấu hình cho các cột MỚI của 3 bảng cũ (EF sẽ gộp với cấu hình ở file gốc) -----
        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.Property(e => e.DiaChi).HasMaxLength(255);
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.Property(e => e.Luong).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Kho).WithMany()
                .HasForeignKey(d => d.MaKho)
                .HasConstraintName("FK_NhanVien_Kho");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasMaxLength(30);
        });

        modelBuilder.Entity<Ban>(entity =>
        {
            // Mỗi bàn một mã QR riêng, không trùng
            entity.HasIndex(e => e.MaQR).IsUnique();
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.ToTable("ThanhToan");
            entity.HasKey(e => e.MaTT);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PhuongThuc).HasMaxLength(30);
            entity.Property(e => e.TrangThai).HasMaxLength(30);

            entity.HasOne(d => d.HoaDon).WithMany()
                .HasForeignKey(d => d.MaHD)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CongViec>(entity =>
        {
            entity.ToTable("CongViec");
            entity.HasKey(e => e.MaCV);
            entity.Property(e => e.TenCV).HasMaxLength(100);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasMaxLength(30);
        });

        modelBuilder.Entity<PhanCong>(entity =>
        {
            entity.ToTable("PhanCong");
            entity.HasKey(e => new { e.MaNV, e.MaCV });

            entity.HasOne(d => d.NhanVien).WithMany()
                .HasForeignKey(d => d.MaNV)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CongViec).WithMany()
                .HasForeignKey(d => d.MaCV)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.ToTable("Kho");
            entity.HasKey(e => e.MaKho);
            entity.Property(e => e.TenKho).HasMaxLength(100);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.SDT).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.TrangThai).HasMaxLength(30);
        });

        modelBuilder.Entity<NguyenLieu>(entity =>
        {
            entity.ToTable("NguyenLieu");
            entity.HasKey(e => e.MaNL);
            entity.Property(e => e.TenNL).HasMaxLength(100);
            entity.Property(e => e.DonViTinh).HasMaxLength(30);
            entity.Property(e => e.GiaNhap).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai).HasMaxLength(30);
        });

        modelBuilder.Entity<TonKho>(entity =>
        {
            entity.ToTable("TonKho");
            entity.HasKey(e => e.MaTon);
            entity.Property(e => e.SoLuongTon).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(30);

            entity.HasOne(d => d.Kho).WithMany(p => p.TonKhos)
                .HasForeignKey(d => d.MaKho)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.NguyenLieu).WithMany()
                .HasForeignKey(d => d.MaNL)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CtNguyenLieu>(entity =>
        {
            entity.ToTable("CT_NguyenLieu");
            entity.HasKey(e => new { e.MaMon, e.MaNL });
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.MonAn).WithMany()
                .HasForeignKey(d => d.MaMon)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.NguyenLieu).WithMany()
                .HasForeignKey(d => d.MaNL)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PhieuNhap>(entity =>
        {
            entity.ToTable("PhieuNhap");
            entity.HasKey(e => e.MaNhap);
            entity.Property(e => e.NgayNhap).HasColumnType("datetime");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai).HasMaxLength(30);

            entity.HasOne(d => d.NhanVien).WithMany()
                .HasForeignKey(d => d.MaNV)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CtKhoNhap>(entity =>
        {
            entity.ToTable("CT_KhoNhap");
            entity.HasKey(e => new { e.MaNhap, e.MaNL });
            entity.Property(e => e.SoLuong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 0)");
            entity.Ignore(e => e.ThanhTien);   // cột tự tính trong SQL, không ghi từ C#

            entity.HasOne(d => d.PhieuNhap).WithMany(p => p.ChiTiets)
                .HasForeignKey(d => d.MaNhap)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.NguyenLieu).WithMany()
                .HasForeignKey(d => d.MaNL)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}