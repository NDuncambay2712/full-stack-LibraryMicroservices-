using CirculationService.Data;
using CirculationService.DTOs.Borrows;
using CirculationService.DTOs.External;
using CirculationService.Models;
using CirculationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CirculationService.Controllers;

[ApiController]
[Route("api/borrows")]
[Route("api/circulation/borrows")]
public class BorrowsController : ControllerBase
{
    private readonly CirculationDbContext _context;
    private readonly IdentityReportClient _identityReportClient;
    private readonly CatalogClient _catalogClient;
    private readonly BorrowSettingsService _borrowSettings;
    private readonly IConfiguration _configuration;

    public BorrowsController(
        CirculationDbContext context,
        IdentityReportClient identityReportClient,
        CatalogClient catalogClient,
        BorrowSettingsService borrowSettings,
        IConfiguration configuration)
    {
        _context = context;
        _identityReportClient = identityReportClient;
        _catalogClient = catalogClient;
        _borrowSettings = borrowSettings;
        _configuration = configuration;
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/borrows?status=&search=&fromDate=&toDate=
    // Lấy danh sách phiếu mượn, hỗ trợ:
    //   status   — lọc theo trạng thái (Borrowed / Returned)
    //   search   — tìm theo tên/mã độc giả, tên/mã sách, hoặc mã phiếu
    //   fromDate — lọc phiếu mượn từ ngày (theo BorrowDate)
    //   toDate   — lọc phiếu mượn đến ngày (theo BorrowDate)
    // ─────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(List<BorrowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBorrows(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.BorrowRecords.AsQueryable();

        // Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        // Tìm kiếm text: mã phiếu (guid), tên/mã độc giả, tên/mã sách
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.ReaderName.ToLower().Contains(searchLower) ||
                x.BookTitle.ToLower().Contains(searchLower) ||
                x.ReaderId.ToString().ToLower().Contains(searchLower) ||
                x.BookId.ToString().ToLower().Contains(searchLower) ||
                x.Id.ToString().ToLower().Contains(searchLower));
        }

        // Lọc từ ngày mượn
        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(x => x.BorrowDate.Date >= from);
        }

        // Lọc đến ngày mượn
        if (toDate.HasValue)
        {
            var to = toDate.Value.Date;
            query = query.Where(x => x.BorrowDate.Date <= to);
        }

        var now = DateTime.UtcNow;

        var result = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = x.Status == "Borrowed" && x.DueDate < now
            })
            .ToListAsync();

        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/borrows/stats
    // Trả về thống kê tổng quan cho 4 card trên UI:
    //   Total, Borrowing, Returned, UnpaidFine, Overdue, TotalUnpaidFineAmount
    // ─────────────────────────────────────────────────────────────
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(BorrowStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;

        var total       = await _context.BorrowRecords.CountAsync();
        var borrowing   = await _context.BorrowRecords.CountAsync(x => x.Status == "Borrowed");
        var returned    = await _context.BorrowRecords.CountAsync(x => x.Status == "Returned");
        var unpaidFine  = await _context.BorrowRecords.CountAsync(x => x.FineAmount > 0 && !x.IsFinePaid);
        var overdue     = await _context.BorrowRecords.CountAsync(x => x.Status == "Borrowed" && x.DueDate < now);
        var totalUnpaid = await _context.BorrowRecords
            .Where(x => x.FineAmount > 0 && !x.IsFinePaid)
            .SumAsync(x => x.FineAmount);

        return Ok(new BorrowStatsResponse
        {
            Total                 = total,
            Borrowing             = borrowing,
            Returned              = returned,
            UnpaidFine            = unpaidFine,
            Overdue               = overdue,
            TotalUnpaidFineAmount = totalUnpaid
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BorrowResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBorrowById(Guid id)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (role == "Reader" && borrow.ReaderId.ToString() != userId)
        {
            return Forbid();
        }

        return Ok(ToResponse(borrow));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Reader")]
    [ProducesResponseType(typeof(List<BorrowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBorrows()
    {
        // Đọc userId từ claim "userId" theo contract JWT của Nhóm 3
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ" });
        }

        var readerId = Guid.Parse(userId);
        var now = DateTime.UtcNow;

        var result = await _context.BorrowRecords
            .Where(x => x.ReaderId == readerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = x.Status == "Borrowed" && x.DueDate < now
            })
            .ToListAsync();

        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/borrows/book/{bookId}
    // Lấy tất cả phiếu mượn theo mã sách — chức năng tìm theo ID sách
    // ─────────────────────────────────────────────────────────────
    [HttpGet("book/{bookId:guid}")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(List<BorrowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBorrowsByBook(Guid bookId)
    {
        var now = DateTime.UtcNow;

        var result = await _context.BorrowRecords
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = x.Status == "Borrowed" && x.DueDate < now
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("reader/{readerId:guid}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetBorrowsByReader(Guid readerId)
    {
        var now = DateTime.UtcNow;

        var result = await _context.BorrowRecords
            .Where(x => x.ReaderId == readerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = x.Status == "Borrowed" && x.DueDate < now
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("overdue")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetOverdueBorrows()
    {
        var now = DateTime.UtcNow;

        var result = await _context.BorrowRecords
            .Where(x => x.Status == "Borrowed" && x.DueDate < now)
            .OrderBy(x => x.DueDate)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = true
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("fines")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetUnpaidFines()
    {
        var result = await _context.BorrowRecords
            .Where(x => x.FineAmount > 0 && !x.IsFinePaid)
            .OrderByDescending(x => x.ReturnDate)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = false
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("reader/{readerId:guid}/fines")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetUnpaidFinesByReader(Guid readerId)
    {
        var result = await _context.BorrowRecords
            .Where(x => x.ReaderId == readerId && x.FineAmount > 0 && !x.IsFinePaid)
            .OrderByDescending(x => x.ReturnDate)
            .Select(x => new BorrowResponse
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                ReaderName = x.ReaderName,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                FineAmount = x.FineAmount,
                IsFinePaid = x.IsFinePaid,
                IsOverdue = false
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian,Reader")]
    public async Task<IActionResult> CreateBorrow(CreateBorrowRequest request)
    {
        if (request.ReaderId == Guid.Empty)
        {
            return BadRequest(new { message = "ReaderId không hợp lệ" });
        }

        if (request.BookId == Guid.Empty)
        {
            return BadRequest(new { message = "BookId không hợp lệ" });
        }

        var borrowDate = request.BorrowDate ?? DateTime.UtcNow;

        if (request.DueDate <= borrowDate)
        {
            return BadRequest(new { message = "Ngày hẹn trả phải lớn hơn ngày mượn" });
        }

        // Lấy vai trò và userId từ JWT Token
        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        // NẾU LÀ ĐỘC GIẢ (READER)
        if (role == "Reader")
        {
            // Độc giả chỉ được phép tạo yêu cầu mượn cho chính mình
            if (string.IsNullOrWhiteSpace(userId) || Guid.Parse(userId) != request.ReaderId)
            {
                return Forbid();
            }

            var reader = await _identityReportClient.GetReaderStatusAsync(request.ReaderId);
            if (reader == null)
            {
                return BadRequest(new { message = "Không tìm thấy độc giả hoặc Identity Service không phản hồi" });
            }

            if (reader.IsLocked)
            {
                return BadRequest(new { message = "Độc giả đang bị khóa, không thể gửi yêu cầu mượn sách" });
            }

            if (reader.IsCardExpired)
            {
                return BadRequest(new { message = "Thẻ thư viện đã hết hạn, không thể gửi yêu cầu mượn sách" });
            }

            var maxBorrowingBooks = _borrowSettings.MaxBorrowingBooks;
            var currentBorrowingCount = await _context.BorrowRecords
                .CountAsync(x => x.ReaderId == request.ReaderId && (x.Status == "Borrowed" || x.Status == "Requested"));

            if (currentBorrowingCount >= maxBorrowingBooks)
            {
                return BadRequest(new
                {
                    message = $"Bạn đã đạt giới hạn mượn/yêu cầu tối đa {maxBorrowingBooks} sách"
                });
            }

            var book = await _catalogClient.GetBookAvailabilityAsync(request.BookId);
            if (book == null)
            {
                return BadRequest(new { message = "Không tìm thấy sách hoặc Catalog Service không phản hồi" });
            }

            if (!book.IsAvailable)
            {
                return BadRequest(new { message = "Sách đã hết bản sao khả dụng, không thể mượn" });
            }

            // Tạo phiếu mượn dưới dạng Requested (Chờ duyệt)
            var borrow = new BorrowRecord
            {
                ReaderId = reader.ReaderId,
                ReaderName = reader.Name,
                BookId = book.BookId,
                BookTitle = book.Title,
                BorrowDate = borrowDate,
                DueDate = request.DueDate,
                Status = "Requested",
                FineAmount = 0,
                IsFinePaid = true,
                CreatedAt = DateTime.UtcNow
            };

            var invoice = new Invoice
            {
                BorrowRecord = borrow,
                ReaderId = reader.ReaderId,
                Amount = 0,
                Type = "BorrowRequest",
                Description = $"Yêu cầu mượn sách: {book.Title}",
                CreatedAt = DateTime.UtcNow
            };

            _context.BorrowRecords.Add(borrow);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Gửi yêu cầu mượn sách thành công, đang chờ thủ thư phê duyệt",
                reportSent = false,
                data = ToResponse(borrow)
            });
        }

        // NẾU LÀ THỦ THƯ/ADMIN (LIBRARIAN/ADMIN)
        var readerAdmin = await _identityReportClient.GetReaderStatusAsync(request.ReaderId);
        if (readerAdmin == null)
        {
            return BadRequest(new { message = "Không tìm thấy độc giả hoặc Identity Service không phản hồi" });
        }

        if (readerAdmin.IsLocked)
        {
            return BadRequest(new { message = "Độc giả đang bị khóa, không thể mượn sách" });
        }

        if (readerAdmin.IsCardExpired)
        {
            return BadRequest(new { message = "Thẻ thư viện đã hết hạn, không thể mượn sách" });
        }

        var maxBooksAdmin = _borrowSettings.MaxBorrowingBooks;
        var currentCountAdmin = await _context.BorrowRecords
            .CountAsync(x => x.ReaderId == request.ReaderId && x.Status == "Borrowed");

        if (currentCountAdmin >= maxBooksAdmin)
        {
            return BadRequest(new
            {
                message = $"Độc giả đã đạt giới hạn mượn tối đa {maxBooksAdmin} sách"
            });
        }

        var bookAdmin = await _catalogClient.GetBookAvailabilityAsync(request.BookId);
        if (bookAdmin == null)
        {
            return BadRequest(new { message = "Không tìm thấy sách hoặc Catalog Service không phản hồi" });
        }

        if (!bookAdmin.IsAvailable)
        {
            return BadRequest(new { message = "Sách đã hết, không thể mượn" });
        }

        var decreaseSuccess = await _catalogClient.DecreaseAvailableCopiesAsync(request.BookId);
        if (!decreaseSuccess)
        {
            return BadRequest(new { message = "Không thể giảm số lượng sách ở Catalog Service" });
        }

        var borrowRecord = new BorrowRecord
        {
            ReaderId = readerAdmin.ReaderId,
            ReaderName = readerAdmin.Name,
            BookId = bookAdmin.BookId,
            BookTitle = bookAdmin.Title,
            BorrowDate = borrowDate,
            DueDate = request.DueDate,
            Status = "Borrowed",
            FineAmount = 0,
            IsFinePaid = true,
            CreatedAt = DateTime.UtcNow
        };

        var invoiceRecord = new Invoice
        {
            BorrowRecord = borrowRecord,
            ReaderId = readerAdmin.ReaderId,
            Amount = 0,
            Type = "Borrow",
            Description = $"Biên lai mượn sách: {bookAdmin.Title}",
            CreatedAt = DateTime.UtcNow
        };

        _context.BorrowRecords.Add(borrowRecord);
        _context.Invoices.Add(invoiceRecord);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            await _catalogClient.IncreaseAvailableCopiesAsync(request.BookId);
            throw;
        }

        var reportSent = await _identityReportClient.SendBookBorrowedEventAsync(
            new BookBorrowedEventRequest
            {
                BorrowId = borrowRecord.Id,
                BookId = borrowRecord.BookId,
                BookTitle = borrowRecord.BookTitle,
                ReaderId = borrowRecord.ReaderId,
                ReaderName = borrowRecord.ReaderName,
                BorrowDate = borrowRecord.BorrowDate,
                DueDate = borrowRecord.DueDate
            });

        return Ok(new
        {
            message = "Tạo phiếu mượn thành công",
            reportSent,
            data = ToResponse(borrowRecord),
            invoice = new InvoiceResponse
            {
                Id = invoiceRecord.Id,
                BorrowRecordId = invoiceRecord.BorrowRecordId,
                ReaderId = invoiceRecord.ReaderId,
                Amount = invoiceRecord.Amount,
                Type = invoiceRecord.Type,
                Description = invoiceRecord.Description,
                CreatedAt = invoiceRecord.CreatedAt
            }
        });
    }

    [HttpPut("{id:guid}/return")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> ReturnBook(Guid id, ReturnBookRequest request)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        if (borrow.Status == "Returned")
        {
            return BadRequest(new { message = "Phiếu mượn này đã được trả trước đó" });
        }

        var returnDate = request.ReturnDate ?? DateTime.UtcNow;

        if (returnDate < borrow.BorrowDate)
        {
            return BadRequest(new { message = "Ngày trả không được nhỏ hơn ngày mượn" });
        }

        borrow.ReturnDate = returnDate;
        borrow.Status = "Returned";
        borrow.FineAmount = CalculateFineAmount(borrow.DueDate, returnDate);
        borrow.IsFinePaid = borrow.FineAmount == 0;
        borrow.UpdatedAt = DateTime.UtcNow;

        var invoice = new Invoice
        {
            BorrowRecordId = borrow.Id,
            ReaderId = borrow.ReaderId,
            Amount = 0,
            Type = "Return",
            Description = $"Biên lai trả sách: {borrow.BookTitle}" + (borrow.FineAmount > 0 ? $". Phát sinh tiền phạt: {borrow.FineAmount} VNĐ" : ""),
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        var catalogUpdated = await _catalogClient.IncreaseAvailableCopiesAsync(borrow.BookId);

        var reportSent = await _identityReportClient.SendBookReturnedEventAsync(
            new BookReturnedEventRequest
            {
                BorrowId = borrow.Id,
                BookId = borrow.BookId,
                BookTitle = borrow.BookTitle,
                ReaderId = borrow.ReaderId,
                ReaderName = borrow.ReaderName,
                ReturnDate = returnDate,
                FineAmount = borrow.FineAmount
            });

        return Ok(new
        {
            message = "Trả sách thành công",
            catalogUpdated,
            reportSent,
            data = ToResponse(borrow),
            invoice = new InvoiceResponse
            {
                Id = invoice.Id,
                BorrowRecordId = invoice.BorrowRecordId,
                ReaderId = invoice.ReaderId,
                Amount = invoice.Amount,
                Type = invoice.Type,
                Description = invoice.Description,
                CreatedAt = invoice.CreatedAt
            }
        });
    }

    [HttpPut("{id:guid}/pay-fine")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> PayFine(Guid id)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        if (borrow.FineAmount <= 0)
        {
            return BadRequest(new { message = "Phiếu mượn này không có phí phạt" });
        }

        if (borrow.IsFinePaid)
        {
            return BadRequest(new { message = "Phí phạt đã được thanh toán trước đó" });
        }

        borrow.IsFinePaid = true;
        borrow.UpdatedAt = DateTime.UtcNow;

        var invoice = new Invoice
        {
            BorrowRecordId = borrow.Id,
            ReaderId = borrow.ReaderId,
            Amount = borrow.FineAmount,
            Type = "FinePayment",
            Description = $"Hóa đơn thanh toán tiền phạt trễ hạn sách: {borrow.BookTitle}",
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Thanh toán phí phạt thành công",
            borrowId = borrow.Id,
            fineAmount = borrow.FineAmount,
            isFinePaid = borrow.IsFinePaid,
            invoice = new InvoiceResponse
            {
                Id = invoice.Id,
                BorrowRecordId = invoice.BorrowRecordId,
                ReaderId = invoice.ReaderId,
                Amount = invoice.Amount,
                Type = invoice.Type,
                Description = invoice.Description,
                CreatedAt = invoice.CreatedAt
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/borrows/{id}/payment-qr
    // Tạo mã QR VietQR để độc giả quét thanh toán phí phạt qua ngân hàng.
    // Phân quyền: Admin + Librarian (Thủ thư in/hiển thị QR cho độc giả).
    //
    // Cách dùng ở frontend:
    //   <img :src="qrImageUrl" />   → hiển thị ảnh QR trực tiếp
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/payment-qr")]
    [Authorize(Roles = "Admin,Librarian,Reader")]
    [ProducesResponseType(typeof(FinePaymentQrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinePaymentQr(Guid id)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        // Kiểm tra phân quyền: Nếu là Reader, chỉ được lấy mã QR thanh toán cho chính mình
        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");
        if (role == "Reader")
        {
            var userId = User.FindFirstValue("userId")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || borrow.ReaderId.ToString() != userId)
            {
                return Forbid();
            }
        }

        if (borrow.FineAmount <= 0)
        {
            return BadRequest(new { message = "Phiếu mượn này không có phí phạt" });
        }

        if (borrow.IsFinePaid)
        {
            return BadRequest(new { message = "Phí phạt đã được thanh toán trước đó" });
        }

        // Đọc cấu hình tài khoản ngân hàng từ appsettings.json
        var bankId      = _configuration["BankPayment:BankId"]     ?? "MB";
        var accountNo   = _configuration["BankPayment:AccountNo"]   ?? "0";
        var accountName = _configuration["BankPayment:AccountName"] ?? "THU VIEN SO";
        var template    = _configuration["BankPayment:Template"]    ?? "compact2";

        // Nội dung chuyển khoản: ngắn gọn, không dấu (giới hạn ~50 ký tự)
        var shortBorrowId = borrow.Id.ToString()[..8].ToUpper();
        var description   = $"Phi phat muon sach {shortBorrowId}";
        var amountInt     = (long)Math.Ceiling(borrow.FineAmount);

        // Tạo URL ảnh QR VietQR (miễn phí, không cần đăng ký)
        // Format: https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png
        //         ?amount={amount}&addInfo={description}&accountName={accountName}
        var encodedDesc = Uri.EscapeDataString(description);
        var encodedName = Uri.EscapeDataString(accountName);
        var qrImageUrl  = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png" +
                          $"?amount={amountInt}" +
                          $"&addInfo={encodedDesc}" +
                          $"&accountName={encodedName}";

        return Ok(new FinePaymentQrResponse
        {
            BorrowRecordId = borrow.Id,
            ReaderName     = borrow.ReaderName,
            BookTitle      = borrow.BookTitle,
            FineAmount     = borrow.FineAmount,
            Description    = description,
            BankId         = bankId,
            AccountNo      = accountNo,
            AccountName    = accountName,
            QrImageUrl     = qrImageUrl
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/borrows/request
    // Cho phép độc giả tự gửi yêu cầu mượn sách.
    // ─────────────────────────────────────────────────────────────────────
    [HttpPost("request")]
    [Authorize(Roles = "Reader")]
    [ProducesResponseType(typeof(BorrowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestBorrow(ReaderBorrowRequest request)
    {
        if (request.BookId == Guid.Empty)
        {
            return BadRequest(new { message = "BookId không hợp lệ" });
        }

        // Lấy userId từ JWT Token
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin độc giả" });
        }

        var readerId = Guid.Parse(userId);

        // Gọi Identity Service để kiểm tra thẻ độc giả
        var reader = await _identityReportClient.GetReaderStatusAsync(readerId);

        if (reader == null)
        {
            return BadRequest(new { message = "Không tìm thấy độc giả hoặc Identity Service không phản hồi" });
        }

        if (reader.IsLocked)
        {
            return BadRequest(new { message = "Thẻ độc giả đang bị khóa, không thể yêu cầu mượn sách" });
        }

        if (reader.IsCardExpired)
        {
            return BadRequest(new { message = "Thẻ thư viện đã hết hạn, không thể yêu cầu mượn sách" });
        }

        // Kiểm tra giới hạn mượn (chỉ tính những cuốn đang mượn 'Borrowed' hoặc đang yêu cầu 'Requested')
        var maxBorrowingBooks = _borrowSettings.MaxBorrowingBooks;
        var currentBorrowingCount = await _context.BorrowRecords
            .CountAsync(x => x.ReaderId == readerId && (x.Status == "Borrowed" || x.Status == "Requested"));

        if (currentBorrowingCount >= maxBorrowingBooks)
        {
            return BadRequest(new
            {
                message = $"Bạn đã vượt quá giới hạn yêu cầu/mượn sách tối đa là {maxBorrowingBooks} cuốn"
            });
        }

        // Kiểm tra tính khả dụng của sách
        var book = await _catalogClient.GetBookAvailabilityAsync(request.BookId);

        if (book == null)
        {
            return BadRequest(new { message = "Không tìm thấy sách hoặc Catalog Service không phản hồi" });
        }

        if (!book.IsAvailable)
        {
            return BadRequest(new { message = "Sách đã hết bản sao khả dụng, không thể mượn" });
        }

        // Tạo phiếu mượn với trạng thái Requested (Chưa trừ số lượng sách khả dụng trong Catalog tại đây)
        var borrow = new BorrowRecord
        {
            ReaderId = reader.ReaderId,
            ReaderName = reader.Name,
            BookId = book.BookId,
            BookTitle = book.Title,
            BorrowDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(request.RequestedDays > 0 ? request.RequestedDays : 14),
            Status = "Requested",
            FineAmount = 0,
            IsFinePaid = true,
            CreatedAt = DateTime.UtcNow
        };

        var invoice = new Invoice
        {
            BorrowRecord = borrow,
            ReaderId = reader.ReaderId,
            Amount = 0,
            Type = "BorrowRequest",
            Description = $"Yêu cầu đăng ký mượn sách: {book.Title}",
            CreatedAt = DateTime.UtcNow
        };

        _context.BorrowRecords.Add(borrow);
        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        return Ok(ToResponse(borrow));
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUT /api/borrows/{id}/approve
    // Phê duyệt yêu cầu mượn sách của độc giả (Librarian/Admin).
    // ─────────────────────────────────────────────────────────────────────
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(BorrowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveBorrow(Guid id, ApproveBorrowRequest request)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        if (borrow.Status != "Requested")
        {
            return BadRequest(new { message = $"Phiếu mượn không ở trạng thái chờ duyệt. Trạng thái hiện tại: {borrow.Status}" });
        }

        // Kiểm tra lại tính khả dụng của sách trước khi duyệt
        var book = await _catalogClient.GetBookAvailabilityAsync(borrow.BookId);

        if (book == null)
        {
            return BadRequest(new { message = "Không tìm thấy sách hoặc Catalog Service không phản hồi" });
        }

        if (!book.IsAvailable)
        {
            return BadRequest(new { message = "Sách đã hết bản sao khả dụng, không thể phê duyệt mượn" });
        }

        // Tiến hành trừ bản sao sách khả dụng tại Catalog Service
        var decreaseSuccess = await _catalogClient.DecreaseAvailableCopiesAsync(borrow.BookId);

        if (!decreaseSuccess)
        {
            return BadRequest(new { message = "Không thể giảm số lượng sách ở Catalog Service" });
        }

        // Cập nhật trạng thái phiếu mượn sang Borrowed
        borrow.Status = "Borrowed";
        borrow.BorrowDate = DateTime.UtcNow;
        borrow.DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(14);
        borrow.UpdatedAt = DateTime.UtcNow;

        // Tạo biên lai mượn sách chính thức
        var invoice = new Invoice
        {
            BorrowRecordId = borrow.Id,
            ReaderId = borrow.ReaderId,
            Amount = 0,
            Type = "Borrow",
            Description = $"Biên lai phê duyệt mượn sách: {borrow.BookTitle}",
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Rollback giảm bản sao nếu lưu DB lỗi
            await _catalogClient.IncreaseAvailableCopiesAsync(borrow.BookId);
            throw;
        }

        // Gửi sự kiện mượn sách tới Identity/Report Service
        var reportSent = await _identityReportClient.SendBookBorrowedEventAsync(
            new BookBorrowedEventRequest
            {
                BorrowId = borrow.Id,
                BookId = borrow.BookId,
                BookTitle = borrow.BookTitle,
                ReaderId = borrow.ReaderId,
                ReaderName = borrow.ReaderName,
                BorrowDate = borrow.BorrowDate,
                DueDate = borrow.DueDate
            });

        return Ok(new
        {
            message = "Phê duyệt yêu cầu mượn sách thành công",
            reportSent,
            data = ToResponse(borrow)
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUT /api/borrows/{id}/reject
    // Từ chối yêu cầu mượn sách của độc giả (Librarian/Admin).
    // ─────────────────────────────────────────────────────────────────────
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(BorrowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectBorrow(Guid id)
    {
        var borrow = await _context.BorrowRecords.FirstOrDefaultAsync(x => x.Id == id);

        if (borrow == null)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn" });
        }

        if (borrow.Status != "Requested")
        {
            return BadRequest(new { message = $"Phiếu mượn không ở trạng thái chờ duyệt. Trạng thái hiện tại: {borrow.Status}" });
        }

        // Cập nhật trạng thái phiếu mượn sang Rejected
        borrow.Status = "Rejected";
        borrow.UpdatedAt = DateTime.UtcNow;

        var invoice = new Invoice
        {
            BorrowRecordId = borrow.Id,
            ReaderId = borrow.ReaderId,
            Amount = 0,
            Type = "Reject",
            Description = $"Từ chối yêu cầu mượn sách: {borrow.BookTitle}",
            CreatedAt = DateTime.UtcNow
        };
        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Từ chối yêu cầu mượn sách thành công",
            data = ToResponse(borrow)
        });
    }

    private decimal CalculateFineAmount(DateTime dueDate, DateTime returnDate)
    {
        if (returnDate <= dueDate)
        {
            return 0;
        }

        var lateDays = (returnDate.Date - dueDate.Date).Days;
        var finePerLateDay = _borrowSettings.FinePerLateDay;

        return lateDays * finePerLateDay;
    }

    private static BorrowResponse ToResponse(BorrowRecord x)
    {
        return new BorrowResponse
        {
            Id = x.Id,
            ReaderId = x.ReaderId,
            ReaderName = x.ReaderName,
            BookId = x.BookId,
            BookTitle = x.BookTitle,
            BorrowDate = x.BorrowDate,
            DueDate = x.DueDate,
            ReturnDate = x.ReturnDate,
            Status = x.Status,
            FineAmount = x.FineAmount,
            IsFinePaid = x.IsFinePaid,
            IsOverdue = x.Status == "Borrowed" && x.DueDate < DateTime.UtcNow
        };
    }
}