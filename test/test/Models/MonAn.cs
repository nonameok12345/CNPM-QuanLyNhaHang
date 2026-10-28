using System;
using System.Collections.Generic;

namespace test.Models;

public partial class MonAn
{
    public int MaMon { get; set; }

    public string TenMon { get; set; } = null!;

    public decimal? Gia { get; set; }

    public int? MaDanhMuc { get; set; }

    public string? HinhAnh { get; set; }

    public virtual DanhMuc? MaDanhMucNavigation { get; set; }
}
