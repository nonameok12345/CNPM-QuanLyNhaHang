# BÁO CÁO DỰ ÁN: KHẢO SÁT NGƯỜI DÙNG VÀ XÂY DỰNG SẢN PHẨM WEB QUẢN LÝ QUÁN ĂN (FOODSYNC)

Từ nhu cầu người dùng đến User Story, Acceptance Criteria và Product Backlog · CMC University · 2026

## Mục lục

- [Thông tin chung](#thông-tin-chung)
- [Bước 1 – Khảo sát người dùng](#bước-1--khảo-sát-người-dùng)
- [Bước 2 – Xác định vấn đề](#bước-2--xác-định-vấn-đề)
- [Bước 3 – Các tình huống thực tế](#bước-3--các-tình-huống-thực-tế-micro-scenarios)
- [Bước 4 – Xây dựng Persona](#bước-4--xây-dựng-persona)
- [Bước 5 – Kịch bản xây dựng](#bước-5--kịch-bản-xây-dựng)
- [Bước 6 – User Story và thang MoSCoW](#bước-6--chuyển-thành-user-story-kèm-thang-ưu-tiên-moscow)
- [Bước 7 – Tiêu chí chấp nhận và quy tắc nghiệp vụ](#bước-7--tiêu-chí-chấp-nhận-và-quy-tắc-nghiệp-vụ)
- [Bước 8 – Từ User Story đến tính năng](#bước-8--từ-câu-chuyện-người-dùng-đến-tính-năng)
- [Bước 9 – Product Backlog và kế hoạch Sprint](#bước-9--product-backlog-và-kế-hoạch-sprint)
- [Kết luận](#kết-luận)

## Thông tin chung

| Hạng mục | Nội dung |
|---|---|
| Tên dự án | FoodSync – Website tổng hợp order và quản lý quán ăn |
| Nhóm thực hiện | Nhóm 1 |
| Thành viên | Phạm Tiến Sơn, Nguyễn Duy Anh, Nguyễn Nhật Tân, Đỗ Anh Tài |
| Môn học / Giảng viên | Công nghệ phần mềm – CMC University / Nguyễn Thị Vân Anh |
| Nền tảng kỹ thuật | ASP.NET Core MVC, Entity Framework Core, SQL Server (website quản lý nhà hàng của nhóm) |
| Thời gian thực hiện | 2026 |

## BƯỚC 1 – KHẢO SÁT NGƯỜI DÙNG

**Mục tiêu:** tìm hiểu cách quán ăn đang nhận order, quản lý thực đơn và giá món; xác định công cụ đang dùng, điểm bất tiện và nhu cầu cải thiện của chủ quán.

### 1.1. Kế hoạch khảo sát

| Nội dung | Mô tả |
|---|---|
| Đối tượng | Chủ quán/đại diện quán ăn thuộc nhiều loại hình khác nhau |
| Số mẫu thực tế | 4 phản hồi Google Forms |
| Phương pháp | Khảo sát trực tuyến qua Google Forms |
| Câu hỏi chính | Loại hình quán; thời gian hoạt động; cách nhận order; có muốn công cụ quản lý quán không; chức năng mong muốn; ý kiến tự do |
| Đầu ra | Bảng thống kê số liệu để xác định vấn đề và xếp ưu tiên chức năng |

**Hạn chế:** mẫu chỉ có 4 phản hồi nên các tỷ lệ chỉ phản ánh nhóm đã trả lời, chưa đại diện cho thị trường. Báo cáo này chỉ dùng số liệu của 4 phản hồi; những điều khảo sát chưa hỏi được ghi rõ là giả thuyết cần xác minh.

### 1.2. Kết quả khảo sát (thống kê)

- **Loại hình quán:** quán cơm bình dân 2 lượt chọn (50%); nhà hàng, quán ăn nhanh và quán bún phở mỗi loại 1 lượt (25%). Tổng 5 lượt chọn trên 4 phản hồi vì có quán chọn hai loại hình.
- **Thời gian hoạt động:** từ 1 đến 3 năm: 3/4 (75%); dưới 1 năm: 1/4 (25%); trên 3 năm: 0/4.
- **Cách nhận order:** dùng tất cả phương án 2/4 (50%); chỉ gọi món trực tiếp 1/4 (25%); chỉ qua app giao đồ ăn 1/4 (25%); chỉ qua Zalo 0/4.
- **Nhu cầu công cụ quản lý quán dễ dùng:** có 4/4 (100%); không hoặc đang cân nhắc: 0/4.
- **Chức năng mong muốn:** tổng hợp order từ nhiều nơi 2/4 (50%); quản lý thực đơn và giá món 1/4 (25%); “Khác” 1/4 (chưa nêu nội dung); quản lý doanh thu, nguyên liệu và kho, lịch làm việc nhân viên: 0/4.
- **Ý kiến tự do:** 3 câu trả lời, trong đó có mong muốn có “sản phẩm mà nhiều quán sử dụng được”.

### 1.3. Tổng hợp vấn đề và nhu cầu thực tế

| Nội dung | Số quán | Tỷ lệ | Ý nghĩa với sản phẩm |
|---|---|---|---|
| Nhận order bằng tất cả kênh (trực tiếp, Zalo, app giao đồ ăn) | 2/4 | 50% | Order nằm ở nhiều nơi, cần một danh sách chung |
| Chọn chức năng tổng hợp order từ nhiều nơi | 2/4 | 50% | Chức năng được chọn nhiều nhất, là trọng tâm của MVP |
| Chọn quản lý thực đơn và giá món | 1/4 | 25% | Dữ liệu nền để lập đơn |
| Muốn có công cụ quản lý quán dễ dùng | 4/4 | 100% | Nhu cầu chung của toàn bộ mẫu |
| Mong muốn sản phẩm nhiều quán dùng được | 1/4 | 25% | Ràng buộc thiết kế: thực đơn do từng quán tự nhập |

Không quán nào chọn quản lý doanh thu, kho hay lịch nhân viên (0/4). Vì vậy các module này không phải ưu tiên của MVP; website đã có sẵn nên nhóm chỉ đưa vào kiểm thử hồi quy ở Sprint 5.

## BƯỚC 2 – XÁC ĐỊNH VẤN ĐỀ

Dựa trên khảo sát (N = 4), nhóm xác định 3 vấn đề cần giải quyết ở phiên bản đầu tiên. Mã P01–P03 trùng với báo cáo Chương 3.

| Mã | Vấn đề | Số quán | Quyết định |
|---|---|---|---|
| I1 (P01) | **Ghi nhận và theo dõi order từ nhiều kênh.** Chủ quán khó nhận và theo dõi order vì order đến từ nhiều nơi nên phải nhìn nhiều chỗ. Hệ quả cụ thể (sót, nhầm, chậm order) khảo sát chưa ghi nhận, cần xác minh thêm. | 2/4 | Trọng tâm |
| I2 (P02) | **Quản lý thực đơn và giá món.** Cần một nơi sửa món và giá để dùng khi lập đơn. Khảo sát chưa ghi nhận nguyên nhân và hệ quả. | 1/4 | Hỗ trợ |
| I3 (P03) | **Dùng chung cho nhiều loại quán, có phân quyền.** Thực đơn do từng quán tự nhập; tài khoản chia vai trò Quản lý, Nhân viên, Khách hàng. Nhiều quán độc lập trên một hệ thống là phần mở rộng. | 1/4 | Ràng buộc thiết kế |
| P04 | Nhu cầu “Khác” chưa nêu cụ thể. | 1/4 | Để sau |

**Lý do chọn I1 làm trọng tâm:** (1) có từ 2/4 quán đề cập; (2) gắn trực tiếp với mục tiêu sản phẩm là gom order; (3) giải quyết được bằng phần mềm trong phạm vi đồ án.

## BƯỚC 3 – CÁC TÌNH HUỐNG THỰC TẾ (MICRO-SCENARIOS)

*Các tình huống dưới đây là minh họa dựng từ số liệu khảo sát, chưa phải trích dẫn phỏng vấn.*

- **Tình huống 1 (từ I1 – 50%):** vào giờ đông khách, quán vừa có khách gọi trực tiếp, vừa có đơn Zalo và đơn từ app giao đồ ăn. Order nằm ở sổ ghi, điện thoại và máy tính bảng riêng nên người bán phải nhìn nhiều nơi cùng lúc, dễ bỏ sót đơn.
- **Tình huống 2 (từ I2 – 25%):** quán thêm món mới hoặc đổi giá. Nếu thực đơn chỉ nằm trên giấy hoặc sổ, nhân viên có thể không biết giá mới khi ghi order.
- **Tình huống 3 (từ I3 – 25%):** một quán khác loại hình muốn dùng cùng phần mềm. Quán cần tự nhập thực đơn của mình, và mỗi nhân viên chỉ nên thấy các chức năng thuộc vai trò của họ.

## BƯỚC 4 – XÂY DỰNG PERSONA

| | Persona 1: Nhân viên thu ngân / phục vụ | Persona 2: Chủ quán / Quản lý |
|---|---|---|
| Căn cứ | Suy ra từ vai trò Nhân viên trong website; chưa phải đối tượng khảo sát | 4 phản hồi khảo sát (chủ quán, đại diện quán) |
| Mục tiêu | Lập đơn nhanh, đúng món, đúng giá; biết đơn nào đang chờ làm; thu tiền chính xác | Xem mọi order ở một nơi; giữ thực đơn và giá đúng; nắm số đơn và doanh thu |
| Khó khăn | Phải theo dõi order ở nhiều nơi; dễ nhầm món hoặc giá khi giờ đông | Order đến từ nhiều kênh (2/4 quán); chưa có một danh sách chung |
| Chức năng dùng chính | Màn hình lập đơn, danh sách thẻ đơn, trạng thái chế biến, thanh toán | Quản lý thực đơn, bảng điều khiển, thống kê theo nguồn, phân quyền |

Khách hàng là nhóm người dùng thứ ba (quét QR gọi món, đặt bàn trước) và tạo ra hai nguồn đơn của hệ thống. Nhóm chưa lập persona riêng vì khảo sát chưa hỏi đối tượng này.

## BƯỚC 5 – KỊCH BẢN XÂY DỰNG

- **Kịch bản 1 (lập đơn tại quầy):** Đăng nhập → mở màn hình lập đơn → chọn bàn → thêm món từ thực đơn và chỉnh số lượng → chọn nguồn đơn → hệ thống tự tính tổng tiền → lưu đơn.
- **Kịch bản 2 (khách gọi món bằng QR):** Khách quét mã QR tại bàn → chọn món và gửi → đơn có nguồn “QR tại bàn” xuất hiện trong danh sách thẻ đơn → nhân viên đổi trạng thái Mới → Đang làm → Đã xong.
- **Kịch bản 3 (thanh toán):** Thu ngân chọn đơn cần thanh toán → chọn tiền mặt hoặc chuyển khoản QR → nếu chuyển khoản thì xác nhận khi thấy tiền về → trả đủ thì hóa đơn chuyển sang “Đã thanh toán”.
- **Kịch bản 4 (theo dõi kinh doanh):** Quản lý mở bảng điều khiển → xem số đơn, doanh thu hôm nay và số đơn theo từng nguồn → mở danh sách đơn để lọc khi cần kiểm tra.

## BƯỚC 6 – CHUYỂN THÀNH USER STORY (KÈM THANG ƯU TIÊN MoSCoW)

| ID | Câu chuyện người dùng | Ưu tiên (MoSCoW) | Nhóm |
|---|---|---|---|
| US01 | Với vai trò chủ quán, tôi muốn đăng ký/đăng nhập bằng tài khoản riêng, khai báo thông tin quán và được phân quyền theo vai trò để mỗi người chỉ dùng đúng chức năng của mình. | Phải | Tài khoản |
| US02 | Với vai trò chủ quán, tôi muốn thêm, sửa, xóa món và giá để thực đơn luôn đúng khi lập đơn. | Phải | Thực đơn |
| US03 | Với vai trò nhân viên, tôi muốn lập đơn tại quầy trên một màn hình: chọn món, số lượng, nguồn đơn rồi thanh toán, để ghi nhận đúng đơn và tổng tiền. | Phải | Bán hàng |
| US04 | Với vai trò nhân viên, tôi muốn xem mọi đơn trên giao diện thẻ đơn và lọc theo nguồn, ngày, trạng thái thanh toán để biết đơn đến từ đâu và xử lý đúng đơn. | Phải | Quản lý đơn |
| US05 | Với vai trò nhân viên, tôi muốn cập nhật trạng thái chế biến của đơn hoặc từng món (Mới, Đang làm, Đã xong) để biết việc nào đang chờ. | Phải | Chế biến |
| US06 | Với vai trò quản lý, tôi muốn xem số đơn và doanh thu theo nguồn trong ngày để biết kênh nào tạo nhiều đơn và doanh thu. | Nên | Báo cáo |
| US07 | Với vai trò nhân viên, tôi muốn Check-in/Check-out để quản lý biết tôi đang Online hay Offline. | Nên | Nhân sự |
| US08 | Với vai trò khách hàng, tôi muốn quét QR tại bàn để gọi món mà không phải chờ nhân viên ghi order. | Nên | Khách hàng (đã có trong website) |
| US09 | Với vai trò khách hàng, tôi muốn đặt bàn trước để có chỗ khi đến quán. | Có thể | Khách hàng (đã có trong website) |

**Thang MoSCoW:** *Phải* – bắt buộc để MVP dùng được; *Nên* – giá trị cao nhưng làm sau luồng cốt lõi; *Có thể* – hữu ích, chưa làm cũng không ảnh hưởng MVP. Mức ưu tiên lấy theo Product Backlog Chương 2 (Cao → Phải, Trung bình → Nên). US05 và US06 là giả thuyết thiết kế: khảo sát chưa hỏi quán có cần theo dõi chế biến hay thống kê theo nguồn hay không, cần xác minh thêm.

## BƯỚC 7 – TIÊU CHÍ CHẤP NHẬN VÀ QUY TẮC NGHIỆP VỤ

- **US01:** Given chưa đăng nhập, When mở trang quản lý, Then được chuyển đến trang đăng nhập. Given đăng nhập bằng tài khoản Nhân viên, When mở trang chỉ dành cho Quản lý, Then hệ thống từ chối truy cập. Given email đã có tài khoản, When đăng ký lại, Then báo lỗi và không tạo trùng.
- **US02:** Given đã đăng nhập, When nhập tên món và giá hợp lệ rồi lưu, Then món xuất hiện trong thực đơn. Given tên trống hoặc giá âm, When lưu, Then báo lỗi và không lưu. Given giá món vừa đổi, Then hóa đơn cũ vẫn giữ giá tại lúc gọi.
- **US03:** Given thực đơn có món, When chọn nhiều món với số lượng từ 1 đến 99 cùng nguồn đơn rồi lưu, Then đơn lưu đủ món, đúng nguồn và tổng tiền do máy chủ tính từ giá chốt. Given chưa chọn món hoặc số lượng ngoài khoảng cho phép, Then báo lỗi và không lưu. Given thanh toán chuyển khoản, Then khoản đó ở trạng thái chờ xác nhận cho đến khi nhân viên xác nhận.
- **US04:** Given có đơn từ QR tại bàn, đặt bàn trước và tại quầy, When mở danh sách, Then mỗi thẻ đơn hiển thị đúng nguồn. When lọc theo nguồn, ngày hoặc trạng thái thanh toán, Then chỉ đơn khớp điều kiện xuất hiện. Given chưa có đơn, Then hiện trạng thái rỗng thay vì lỗi. Đơn cũ chưa có nguồn vẫn hiển thị với giá trị mặc định.
- **US05:** Given món ở trạng thái Mới, When chuyển sang Đang làm rồi Đã xong, Then trạng thái được lưu và vẫn đúng sau khi tải lại. Given mọi món của đơn đã xong, Then đơn hiển thị Đã xong. Khi lưu thất bại, hệ thống báo lỗi và không hiển thị trạng thái giả.
- **US06:** Given ngày có 10 đơn gồm 4 QR tại bàn, 3 đặt bàn trước và 3 tại quầy, When xem bảng điều khiển, Then số đơn hiển thị 4/3/3 và tổng doanh thu theo nguồn bằng doanh thu cả ngày. Given ngày chưa có đơn, Then hiện thông báo chưa có dữ liệu.
- **US07:** Given nhân viên đang Offline, When Check-in, Then trạng thái là Online và lưu giờ vào. Given đang Online, When Check-in lần nữa, Then báo lỗi và không tạo bản ghi trùng.

**Bảng quy tắc nghiệp vụ cốt lõi**

| Quy tắc | Nội dung | Tình trạng |
|---|---|---|
| Số lượng món | Mỗi dòng món có số lượng từ 1 đến 99; sửa về 0 là xóa món | Đã có trong website |
| Giá chốt | Đơn giá lấy từ thực đơn tại lúc gọi và lưu trong chi tiết hóa đơn; tổng tiền bằng tổng số lượng nhân đơn giá, tính ở máy chủ | Đã có trong website |
| Thanh toán | Chỉ có hai hình thức: tiền mặt hoặc chuyển khoản. Hóa đơn phải có tổng tiền lớn hơn 0; số tiền mỗi lần trả là số nguyên đồng từ 1 đến số còn lại | Đã có trong website |
| Chuyển khoản | Khoản khách báo đã chuyển ở trạng thái chờ xác nhận; nhân viên xác nhận hoặc từ chối | Đã có trong website |
| Đóng hóa đơn | Trả đủ thì hóa đơn chuyển sang “Đã thanh toán”, không sửa món hay thanh toán thêm; hệ thống trừ kho theo công thức món | Đã có trong website |
| Hủy hóa đơn | Chỉ hủy được hóa đơn chưa thu đồng nào; hóa đơn đã hủy không có khoản thu nên không vào doanh thu | Đã có trong website |
| Không giảm dưới số đã trả | Không được sửa món làm tổng tiền thấp hơn số khách đã trả | Đã có trong website |
| Một thu ngân tại một thời điểm | Mỗi lúc chỉ một người được sửa hoặc thanh toán hóa đơn để tránh thu thừa tiền | Đã có trong website |
| Đặt bàn | Nhận đặt từ 10:00 đến 21:00, mỗi 30 phút; giữ bàn 2 giờ; đặt trước ít nhất 30 phút và tối đa 30 ngày; tối đa 20 người; mỗi khách giữ tối đa 5 lịch | Đã có trong website |
| Nguồn đơn | Gồm Tại quầy, QR tại bàn, Đặt bàn trước; mọi luồng tạo đơn đều gán nguồn; đơn cũ không có nguồn hiển thị giá trị mặc định | Kế hoạch Sprint 3–4 |
| Trạng thái chế biến | Mới → Đang làm → Đã xong; đơn là Đã xong khi mọi món đã xong | Kế hoạch Sprint 4 |

## BƯỚC 8 – TỪ CÂU CHUYỆN NGƯỜI DÙNG ĐẾN TÍNH NĂNG

- **I1 (Order từ nhiều kênh):** liên kết US03, US04, US05, US06, US08 → màn hình lập đơn (POS) có cột nguồn đơn, danh sách thẻ đơn có bộ lọc theo nguồn, trạng thái chế biến, thống kê số đơn và doanh thu theo nguồn, đơn tự gán nguồn khi khách quét QR.
- **I2 (Thực đơn và giá):** liên kết US02 → quản lý thực đơn, danh mục và giá món; giá chốt trong chi tiết hóa đơn.
- **I3 (Dùng chung, có phân quyền):** liên kết US01, US09 → đăng nhập theo vai trò Quản lý, Nhân viên, Khách hàng; đặt bàn trước của khách tạo ra nguồn đơn “Đặt bàn trước”.
- **Ngoài ba vấn đề trên:** US07 (chấm công) hỗ trợ quản lý nhân sự, chưa có bằng chứng từ khảo sát nên xếp mức Nên và làm sau cùng.

## BƯỚC 9 – PRODUCT BACKLOG VÀ KẾ HOẠCH SPRINT

### 9.1. Phiên bản Product Backlog đầu tiên

| Thứ tự | ID | Hạng mục | Ưu tiên | Lý do | Dự kiến Sprint |
|---|---|---|---|---|---|
| 1 | US01 | Đăng ký, đăng nhập, thông tin quán, phân quyền | Phải | Nền tảng để mỗi vai trò dùng đúng chức năng và có dữ liệu riêng | Sprint 1 |
| 2 | US02 | Quản lý thực đơn và giá món | Phải | Không có món thì không lập được đơn | Sprint 2 |
| 3 | US03 | Màn hình lập đơn (POS), nguồn đơn và thanh toán | Phải | Giải quyết trực tiếp vấn đề I1 và là chức năng được chọn nhiều nhất (2/4) | Sprint 3 |
| 4 | US04 | Danh sách thẻ đơn và bộ lọc theo nguồn | Phải | Là chỗ “gom” order về một danh sách | Sprint 4 |
| 5 | US05 | Trạng thái chế biến | Phải | Biết đơn hoặc món nào còn phải làm | Sprint 4 |
| 6 | US06 | Thống kê đơn và doanh thu theo nguồn | Nên | Dùng lại dữ liệu đơn; cần xác minh nhu cầu | Sprint 5 |
| 7 | US07 | Chấm công Check-in/Check-out | Nên | Hỗ trợ quản lý nhân sự, không chặn chức năng khác | Sprint 5 |
| 8 | US08 | Khách quét QR gọi món tại bàn | Nên | Đã có trong website; Sprint 3–4 gán nguồn tự động, Sprint 5 kiểm thử hồi quy | Sprint 3–5 |
| 9 | US09 | Khách đặt bàn trước | Có thể | Đã có trong website; Sprint 4 gán nguồn “Đặt bàn trước”, Sprint 5 kiểm thử hồi quy | Sprint 4–5 |
| 10 | KT-01 | Sửa lỗi và kiểm thử hồi quy các luồng chính | Phải | Chỉ làm khi các chức năng nghiệp vụ đã ổn định | Sprint 5 |
| 11 | KT-02 | Hướng dẫn sử dụng và chuẩn bị bàn giao | Phải | Sản phẩm cần tài liệu để bàn giao và vấn đáp | Sprint 5 |

Chưa đưa vào Backlog: quản lý nguyên liệu, kho, công thức mở rộng và báo cáo nâng cao (0/4 quán chọn); tích hợp tự động với Zalo, Grab, ShopeeFood (chưa xác minh khả năng kết nối); nhu cầu “Khác” (1/4, chưa nêu nội dung). Ở bản MVP, order từ Zalo và app giao đồ ăn chưa có nguồn riêng; nếu cần, thêm hai nguồn này là phần mở rộng.

### 9.2. Kế hoạch phát triển sản phẩm (5 Sprint)

| Sprint | Phạm vi | Increment có thể kiểm tra |
|---|---|---|
| Sprint 1 | Tài khoản, phân quyền và dữ liệu quán (US01) | Đăng nhập theo vai trò; trang quản lý được bảo vệ; kiểm thử đúng/sai |
| Sprint 2 | Thực đơn và giá món (US02) | Thêm, sửa, xóa món; kiểm tra tên và giá; hóa đơn cũ giữ giá |
| Sprint 3 | Màn hình POS, nguồn đơn và thanh toán (US03, US08) | Lập đơn nhiều món trên một màn hình, lưu đúng nguồn và tổng tiền, thanh toán tiền mặt hoặc chuyển khoản QR |
| Sprint 4 | Danh sách thẻ đơn, bộ lọc nguồn và trạng thái chế biến (US04, US05, US09) | Xem mọi đơn trong một danh sách, lọc theo nguồn, đổi trạng thái chế biến và giữ nguyên sau khi tải lại |
| Sprint 5 | Thống kê, chấm công, kiểm thử và bàn giao (US06, US07, KT-01, KT-02) | Số đơn và doanh thu theo nguồn; trạng thái Online/Offline; biên bản kiểm thử hồi quy; hướng dẫn sử dụng |

### 9.3. Đối chiếu với website hiện có

| Khả năng | Tình trạng trong website | Việc còn lại |
|---|---|---|
| Đăng nhập, phân quyền Quản lý, Nhân viên, Khách hàng | Đã có | Cô lập dữ liệu nhiều quán chưa làm |
| Thực đơn và giá chốt trong chi tiết hóa đơn | Đã có | Kiểm thử theo tiêu chí US02 |
| Hóa đơn, thanh toán tiền mặt/chuyển khoản, xác nhận chuyển khoản, hủy, trừ kho | Đã có | Cần màn hình lập đơn một lần nhiều món (Sprint 3) |
| QR gọi món tại bàn, đặt bàn trước | Đã có | Gán nguồn tự động (Sprint 3–4) |
| Danh sách hóa đơn lọc theo ngày và trạng thái | Đã có | Thẻ đơn và lọc theo nguồn (Sprint 4) |
| Nguồn đơn, trạng thái chế biến | Chưa có | Sprint 3 và Sprint 4 |
| Thống kê theo nguồn, chấm công | Chưa có | Sprint 5 |
| Tích hợp tự động Zalo, Grab, ShopeeFood | Chưa có, ngoài phạm vi MVP | Xem lại sau khi xác minh khả năng kết nối |

## KẾT LUẬN

Qua khảo sát 4 quán ăn thuộc bốn loại hình, nhóm xác định nhu cầu cốt lõi của FoodSync: có một danh sách chung để theo dõi order đến từ nhiều kênh (2/4 quán), có nơi quản lý thực đơn và giá món (1/4) và dùng được cho nhiều loại quán, có phân quyền. Cả 4/4 quán đều muốn có công cụ quản lý quán dễ dùng.

Nhu cầu này được chuyển thành 9 User Story có thang ưu tiên MoSCoW, kèm tiêu chí chấp nhận kiểm tra được và các quy tắc nghiệp vụ lấy từ chính website của nhóm. Product Backlog xếp I1 làm trọng tâm và trải qua 5 Sprint, mỗi Sprint cho ra một Increment chạy được: tài khoản, thực đơn, lập đơn có nguồn, danh sách đơn có lọc và trạng thái chế biến, rồi thống kê, kiểm thử và bàn giao.

Báo cáo phân biệt rõ phần website đã có (đăng nhập, thực đơn, hóa đơn, thanh toán, QR gọi món, đặt bàn) với phần cần phát triển (nguồn đơn, trạng thái chế biến, thống kê theo nguồn, chấm công). Hạn chế lớn nhất là cỡ mẫu 4 phản hồi; các giả thuyết về trạng thái chế biến và thống kê theo nguồn cần được xác minh bằng phỏng vấn thực tế trước khi chốt phạm vi.
