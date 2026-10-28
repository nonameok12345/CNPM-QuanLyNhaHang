using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace test.Models;

public partial class QuanLyNhaHangContext : DbContext
{
    public QuanLyNhaHangContext()
    {
    }

    public QuanLyNhaHangContext(DbContextOptions<QuanLyNhaHangContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<MonAn> MonAns { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<Ban> Bans { get; set; }

    public virtual DbSet<DatBan> DatBans { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK__DanhMuc__B375088764C471AB");

            entity.ToTable("DanhMuc");

            entity.Property(e => e.MaDanhMuc).ValueGeneratedNever();
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
        });

        modelBuilder.Entity<MonAn>(entity =>
        {
            entity.HasKey(e => e.MaMon).HasName("PK__MonAn__3A5B29A872C1ED6A");

            entity.ToTable("MonAn");

            entity.Property(e => e.Gia).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.HinhAnh).HasMaxLength(255);
            entity.Property(e => e.TenMon).HasMaxLength(100);

            entity.HasOne(d => d.MaDanhMucNavigation).WithMany(p => p.MonAns)
                .HasForeignKey(d => d.MaDanhMuc)
                .HasConstraintName("FK_MonAn_DanhMuc");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.ToTable("NhanVien");
            entity.HasKey(e => e.MaNV);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.SDT).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.TaiKhoan).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.MatKhau).HasMaxLength(255).IsUnicode(false);
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("KhachHang");
            entity.HasKey(e => e.MaKH);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.SDT).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.MatKhau).HasMaxLength(255).IsUnicode(false);
        });

        modelBuilder.Entity<Ban>(entity =>
        {
            entity.ToTable("Ban");
            entity.HasKey(e => e.MaBan);
            entity.Property(e => e.SoBan).HasMaxLength(20);
            entity.Property(e => e.TrangThai).HasMaxLength(30);
        });

        modelBuilder.Entity<DatBan>(entity =>
        {
            entity.ToTable("DatBan");
            entity.HasKey(e => e.MaDatBan);
            entity.Property(e => e.NgayGio).HasColumnType("datetime");
            entity.Property(e => e.TrangThai).HasMaxLength(30);

            entity.HasOne(d => d.Ban).WithMany()
                .HasForeignKey(d => d.MaBan)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne<KhachHang>().WithMany()
                .HasForeignKey(d => d.MaKH)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.ToTable("HoaDon");
            entity.HasKey(e => e.MaHD);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PhuongThucTT).HasMaxLength(30);
            entity.Property(e => e.TrangThai).HasMaxLength(30);

            entity.HasOne(d => d.Ban).WithMany()
                .HasForeignKey(d => d.MaBan)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne<KhachHang>().WithMany()
                .HasForeignKey(d => d.MaKH)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DatBan).WithMany()
                .HasForeignKey(d => d.MaDatBan)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne<NhanVien>()
                .WithMany()
                .HasForeignKey(e => e.MaNV)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoaDon_NhanVien");
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.ToTable("ChiTietHoaDon");
            entity.HasKey(e => new { e.MaHD, e.MaMon });
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaHD)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.MonAn).WithMany()
                .HasForeignKey(d => d.MaMon)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}