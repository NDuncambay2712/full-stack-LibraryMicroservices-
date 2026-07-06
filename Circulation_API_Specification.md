# ĐẶC TẢ API CHI TIẾT - PHÂN HỆ LƯU THÔNG (CIRCULATION SERVICE)

Tài liệu này đặc tả chi tiết toàn bộ API của phân hệ **Circulation Service** khớp 100% với mã nguồn hiện tại của Backend. Đội ngũ Frontend sử dụng tài liệu này để lập trình và kết nối hệ thống.

---

## 1. MỘT SỐ LƯU Ý CHUNG CHO FRONTEND

1. **Base URL:** Các API chạy trực tiếp tại địa chỉ cổng của Circulation Service (Ví dụ: `http://localhost:5002` hoặc địa chỉ IP deploy).
2. **Route Prefix:** Backend sử dụng prefix trực tiếp `/api/...` (Ví dụ: `/api/borrows`, `/api/invoices`, `/api/borrow-settings`). Không có tiền tố `/circulation`.
3. **Xác thực & Phân quyền:**
   * Gửi header: `Authorization: Bearer <JWT_TOKEN>` đối với mọi API yêu cầu đăng nhập.
   * Quyền hạn (`Roles`) được mã hóa trong claim `"role"` của JWT Token.
4. **Định dạng thời gian:** Định dạng chuỗi chuẩn ISO 8601 UTC (Ví dụ: `2026-06-28T13:30:00Z`).

---

## 2. DANH SÁCH ENDPOINTS (API TABLE)

### A. Quản lý Phiếu mượn (Borrows API)

| STT | Method | URL Path | Roles | Chức năng (Description) |
|---|---|---|---|---|
| 1 | `GET` | `/api/borrows` | Admin, Librarian | Lấy danh sách toàn bộ phiếu mượn trả. Hỗ trợ query params lọc:<br> - `status`: Lọc trạng thái (`Requested`, `Borrowed`, `Returned`, `Rejected`) <br> - `search`: Tìm theo tên/mã độc giả, tên/mã sách, mã phiếu <br> - `fromDate`/`toDate`: Ngày mượn bắt đầu/kết thúc (theo ngày) |
| 2 | `GET` | `/api/borrows/{id}` | Admin, Librarian, Reader | Lấy chi tiết phiếu mượn theo ID (Reader chỉ được xem phiếu của mình). |
| 3 | `GET` | `/api/borrows/me` | Reader | Lấy danh sách phiếu mượn cá nhân của độc giả đang đăng nhập. |
| 4 | `GET` | `/api/borrows/stats` | Admin, Librarian | Lấy số liệu thống kê tổng quan (Tổng số phiếu, Số sách đang mượn, Đã trả, Số phiếu phạt chưa trả, Số phiếu quá hạn, Tổng tiền phạt chưa nộp). |
| 5 | `GET` | `/api/borrows/book/{bookId}` | Admin, Librarian | Tìm kiếm các phiếu mượn liên quan đến một mã sách cụ thể. |
| 6 | `GET` | `/api/borrows/reader/{readerId}` | Admin, Librarian | Tìm kiếm các phiếu mượn liên quan đến một độc giả cụ thể. |
| 7 | `GET` | `/api/borrows/overdue` | Admin, Librarian | Danh sách các phiếu mượn đang bị quá hạn trả sách (`Status == "Borrowed"` và `DueDate < Now`). |
| 8 | `GET` | `/api/borrows/fines` | Admin, Librarian | Danh sách công nợ phí phạt chưa nộp của toàn bộ thư viện. |
| 9 | `GET` | `/api/borrows/reader/{readerId}/fines` | Admin, Librarian | Danh sách công nợ phí phạt chưa nộp của một độc giả cụ thể. |
| 10 | `POST` | `/api/borrows` | Admin, Librarian, Reader | Tạo phiếu mượn trực tiếp (Librarian) hoặc Độc giả tự đăng ký mượn sách (Reader). <br> *Yêu cầu truyền đầy đủ `readerId`.* |
| 11 | `POST` | `/api/borrows/request` | Reader | Dành riêng cho Độc giả đăng ký mượn sách. Tự động lấy `readerId` từ token, trạng thái mặc định là `Requested`. |
| 12 | `PUT` | `/api/borrows/{id}/approve` | Admin, Librarian | Duyệt yêu cầu mượn (chuyển trạng thái sang `Borrowed` và giảm số lượng sách khả dụng trong kho). |
| 13 | `PUT` | `/api/borrows/{id}/reject` | Admin, Librarian | Từ chối yêu cầu mượn (chuyển trạng thái sang `Rejected`). |
| 14 | `PUT` | `/api/borrows/{id}/return` | Admin, Librarian | Nhận trả sách. Backend tự động tính tiền phạt trễ hạn dựa trên cấu hình, cập nhật kho sách, và xuất hóa đơn trả. |
| 15 | `GET` | `/api/borrows/{id}/payment-qr` | Admin, Librarian | Sinh URL mã QR VietQR để thanh toán khoản phạt quá hạn. |
| 16 | `PUT` | `/api/borrows/{id}/pay-fine` | Admin, Librarian | Xác nhận độc giả đã thanh toán phí phạt (chuyển `IsFinePaid = true`). |

### B. Quản lý Quy tắc mượn trả (Borrow Settings API)

| STT | Method | URL Path | Roles | Chức năng (Description) |
|---|---|---|---|---|
| 17 | `GET` | `/api/borrow-settings` | Admin, Librarian | Xem các quy tắc mượn trả hiện tại (Số ngày mượn mặc định, giới hạn sách mượn...). |
| 18 | `PUT` | `/api/borrow-settings` | Admin | Cập nhật quy tắc mượn trả (Số lượng sách tối đa được mượn, Đơn giá phạt trễ hạn). |

### C. Quản lý Hóa đơn & Biên lai (Invoices API)

| STT | Method | URL Path | Roles | Chức năng (Description) |
|---|---|---|---|---|
| 19 | `GET` | `/api/invoices` | Admin, Librarian | Lấy toàn bộ danh sách hóa đơn biên lai trong hệ thống. |
| 20 | `GET` | `/api/invoices/{id}` | Admin, Librarian, Reader | Xem chi tiết hóa đơn theo ID (Reader chỉ được xem hóa đơn của mình). |
| 21 | `GET` | `/api/invoices/me` | Reader | Xem danh sách hóa đơn cá nhân của độc giả đang đăng nhập. |
| 22 | `GET` | `/api/invoices/reader/{readerId}` | Admin, Librarian | Xem danh sách hóa đơn của độc giả cụ thể. |
| 23 | `GET` | `/api/invoices/borrow/{borrowRecordId}`| Admin, Librarian, Reader | Xem hóa đơn liên quan đến một phiếu mượn cụ thể. |

### D. API Proxy dữ liệu trung gian (Proxy API)

| STT | Method | URL Path | Roles | Chức năng (Description) |
|---|---|---|---|---|
| 24 | `GET` | `/api/proxy/books` | Admin, Librarian | Tìm kiếm thông tin sách từ Catalog Service (Nhóm 1). Hỗ trợ query: `search`, `onlyAvailable`. |
| 25 | `GET` | `/api/proxy/readers` | Admin, Librarian | Tìm kiếm thông tin độc giả từ Identity Service (Nhóm 3). Hỗ trợ query: `search`. |

---

## 3. ĐẶC TẢ CHI TIẾT DỮ LIỆU TRUYỀN NHẬN (JSON DTOs)

### A. Đối tượng Phiếu mượn (BorrowResponse)
Trả về khi xem chi tiết hoặc danh sách phiếu mượn.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã phiếu mượn (uuid)
  "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã độc giả (uuid)
  "readerName": "Nguyễn Văn A", // Tên độc giả
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã sách (uuid)
  "bookTitle": "Lập trình hướng đối tượng", // Tên cuốn sách
  "borrowDate": "2026-06-28T13:30:00Z", // Ngày bắt đầu mượn (ISO 8601)
  "dueDate": "2026-07-12T13:30:00Z", // Ngày hẹn trả sách (ISO 8601)
  "returnDate": null, // Ngày trả thực tế (ISO 8601, null nếu chưa trả)
  "status": "Borrowed", // Trạng thái ('Requested', 'Borrowed', 'Returned', 'Rejected')
  "fineAmount": 0.0, // Số tiền phạt phát sinh (number)
  "isFinePaid": true, // Trạng thái thanh toán phạt (boolean)
  "isOverdue": false // Phiếu mượn có đang bị trễ hạn hay không (boolean)
}
```

---

### B. Request Body tạo yêu cầu mượn sách (`POST /api/borrows`)
Sử dụng khi Thủ thư tạo phiếu trực tiếp cho độc giả hoặc Độc giả tự đăng ký nhưng cung cấp rõ thông tin.

```json
{
  "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã độc giả (uuid - Bắt buộc)
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã sách (uuid - Bắt buộc)
  "borrowDate": "2026-06-28T13:30:00Z", // Ngày mượn (Tùy chọn, mặc định là thời gian hiện tại ở Backend nếu bỏ trống)
  "dueDate": "2026-07-12T13:30:00Z" // Hạn trả sách (Bắt buộc)
}
```

### C. Request Body Độc giả tự đăng ký mượn (`POST /api/borrows/request`)
Sử dụng khi Độc giả tự gửi yêu cầu mượn sách thông qua tài khoản cá nhân. Không cần truyền `readerId` (Backend tự lấy từ token).

```json
{
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã sách cần mượn (uuid - Bắt buộc)
  "requestedDays": 14 // Số ngày mượn mong muốn (mặc định 14 ngày nếu bỏ trống)
}
```

---

### D. Dữ liệu khi Duyệt/Trả/Thanh toán

* **Duyệt phiếu mượn (`PUT /api/borrows/{id}/approve`):**
  * Cần truyền body JSON để chỉ định hạn trả. Nếu muốn dùng hạn trả mặc định (+14 ngày), truyền `null` hoặc bỏ trống trường `dueDate`.
  * **Request Body:**
    ```json
    {
      "dueDate": "2026-07-15T10:00:00Z" // Tùy chọn (ISO 8601 hoặc null)
    }
    ```

* **Từ chối yêu cầu mượn (`PUT /api/borrows/{id}/reject`):**
  * Không yêu cầu Request Body.

* **Trả sách (`PUT /api/borrows/{id}/return`):**
  * Truyền thời gian trả sách thực tế. Nếu bỏ trống, Backend tự động lấy thời gian hiện tại trên hệ thống.
  * **Request Body:**
    ```json
    {
      "returnDate": "2026-06-28T20:30:00Z" // Tùy chọn (ISO 8601 hoặc null)
    }
    ```

* **Xác nhận đóng phạt (`PUT /api/borrows/{id}/pay-fine`):**
  * Không yêu cầu Request Body.

---

## 4. ĐẶC TẢ LUỒNG QR THANH TOÁN TIỀN PHẠT (VIETQR)

* **Phương thức & URL**: `GET` `/api/borrows/{id}/payment-qr`
* **Response Body JSON mẫu trả về**:

```json
{
  "borrowRecordId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã phiếu mượn (uuid)
  "readerName": "Nguyễn Văn A", // Tên độc giả đóng phạt
  "bookTitle": "Lập trình hướng đối tượng", // Tên sách trả trễ
  "fineAmount": 25000.0, // Số tiền phạt trễ hạn (decimal/number)
  "description": "Phi phat muon sach 3FA85F64", // Nội dung chuyển khoản chuẩn hóa (không dấu, giới hạn độ dài)
  "bankId": "MB", // Mã ngân hàng thụ hưởng nhận tiền phạt
  "accountNo": "0325442489", // Số tài khoản ngân hàng thụ hưởng của thư viện
  "accountName": "THU VIEN SO DNU", // Tên chủ tài khoản thụ hưởng
  "qrImageUrl": "https://img.vietqr.io/image/MB-0325442489-compact2.png?amount=25000&addInfo=Phi%20phat%20muon%20sach%203FA85F64&accountName=THU%20VIEN%20SO%20DNU" // Link ảnh QR đã sinh sẵn từ VietQR để Frontend nhúng trực tiếp vào thẻ <img src="..." />
}
```

---

## 5. CHI TIẾT CẤU HÌNH MƯỢN TRẢ (BORROW SETTINGS)

### A. Đối tượng Cấu hình (Response từ `GET /api/borrow-settings` & `PUT /api/borrow-settings`)
```json
{
  "maxBorrowingBooks": 5, // Số lượng cuốn sách tối đa độc giả được mượn cùng lúc (mặc định: 5)
  "finePerLateDay": 5000.0 // Đơn giá phạt quá hạn cho mỗi ngày trễ (mặc định: 5000 VNĐ/ngày)
}
```

### B. Request Body Cập nhật cấu hình (`PUT /api/borrow-settings`)
*(Lưu ý: Chỉ tài khoản quyền Admin mới gọi được API này)*
```json
{
  "maxBorrowingBooks": 5, // Số lượng sách tối đa (Bắt buộc, khoảng từ 1 đến 20)
  "finePerLateDay": 5000.0 // Phí phạt/ngày trễ (Bắt buộc, khoảng từ 0 đến 500,000 VNĐ)
}
```

---

## 6. ĐẶC TẢ HÓA ĐƠN & BIÊN LAI (INVOICE DTO)

### Đối tượng Hóa đơn (Response từ các Invoices API)
Hóa đơn tài chính được tạo tự động khi mượn, trả hoặc thu tiền phạt.

```json
{
  "id": "76d8b2d4-3ab9-4281-a968-3e4bfa9d0f1b", // Mã hóa đơn (uuid)
  "borrowRecordId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã phiếu mượn liên quan (uuid)
  "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Mã độc giả (uuid)
  "amount": 25000.0, // Số tiền hóa đơn (0 VNĐ nếu là mượn/trả đúng hạn, > 0 VNĐ nếu là biên lai thu phạt)
  "type": "FinePayment", // Phân loại hóa đơn ('Borrow' - Mượn trực tiếp, 'Return' - Biên lai trả, 'FinePayment' - Thu phạt trễ hạn, 'BorrowRequest' - Yêu cầu mượn, 'Reject' - Từ chối mượn)
  "description": "Hóa đơn thanh toán tiền phạt trễ hạn sách: Lập trình hướng đối tượng", // Chi tiết nội dung hóa đơn
  "createdAt": "2026-06-28T20:30:00Z" // Thời gian xuất hóa đơn (ISO 8601)
}
```
