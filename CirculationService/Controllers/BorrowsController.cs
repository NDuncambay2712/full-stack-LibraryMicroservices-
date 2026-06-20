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
public class BorrowsController : ControllerBase
{
    private readonly CirculationDbContext _context;
    private readonly IdentityReportClient _identityReportClient;
    private readonly CatalogClient _catalogClient;
    private readonly IConfiguration _configuration;

    public BorrowsController(
        CirculationDbContext context,
        IdentityReportClient identityReportClient,
        CatalogClient catalogClient,
        IConfiguration configuration)
    {
        _context = context;
        _identityReportClient = identityReportClient;
        _catalogClient = catalogClient;
        _configuration = configuration;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(List<BorrowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBorrows([FromQuery] string? status)
    {
        var query = _context.BorrowRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
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

        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
    [Authorize(Roles = "Admin,Librarian")]
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

        var reader = await _identityReportClient.GetReaderStatusAsync(request.ReaderId);

        if (reader == null)
        {
            return BadRequest(new { message = "Không tìm thấy độc giả hoặc Identity Service không phản hồi" });
        }

        if (reader.IsLocked)
        {
            return BadRequest(new { message = "Độc giả đang bị khóa, không thể mượn sách" });
        }

        if (reader.IsCardExpired)
        {
            return BadRequest(new { message = "Thẻ thư viện đã hết hạn, không thể mượn sách" });
        }

        var maxBorrowingBooks = int.Parse(_configuration["BorrowSettings:MaxBorrowingBooks"] ?? "5");

        var currentBorrowingCount = await _context.BorrowRecords
            .CountAsync(x => x.ReaderId == request.ReaderId && x.Status == "Borrowed");

        if (currentBorrowingCount >= maxBorrowingBooks)
        {
            return BadRequest(new
            {
                message = $"Độc giả đã đạt giới hạn mượn tối đa {maxBorrowingBooks} sách"
            });
        }

        var book = await _catalogClient.GetBookAvailabilityAsync(request.BookId);

        if (book == null)
        {
            return BadRequest(new { message = "Không tìm thấy sách hoặc Catalog Service không phản hồi" });
        }

        if (!book.IsAvailable)
        {
            return BadRequest(new { message = "Sách đã hết, không thể mượn" });
        }

        var decreaseSuccess = await _catalogClient.DecreaseAvailableCopiesAsync(request.BookId);

        if (!decreaseSuccess)
        {
            return BadRequest(new { message = "Không thể giảm số lượng sách ở Catalog Service" });
        }

        var borrow = new BorrowRecord
        {
            ReaderId = reader.ReaderId,
            ReaderName = reader.Name,
            BookId = book.BookId,
            BookTitle = book.Title,
            BorrowDate = borrowDate,
            DueDate = request.DueDate,
            Status = "Borrowed",
            FineAmount = 0,
            IsFinePaid = true,
            CreatedAt = DateTime.UtcNow
        };

        var invoice = new Invoice
        {
            BorrowRecord = borrow,
            ReaderId = reader.ReaderId,
            Amount = 0,
            Type = "Borrow",
            Description = $"Biên lai mượn sách: {book.Title}",
            CreatedAt = DateTime.UtcNow
        };

        _context.BorrowRecords.Add(borrow);
        _context.Invoices.Add(invoice);

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
            message = "Tạo phiếu mượn thành công",
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

    private decimal CalculateFineAmount(DateTime dueDate, DateTime returnDate)
    {
        if (returnDate <= dueDate)
        {
            return 0;
        }

        var lateDays = (returnDate.Date - dueDate.Date).Days;
        var finePerLateDay = decimal.Parse(_configuration["BorrowSettings:FinePerLateDay"] ?? "5000");

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