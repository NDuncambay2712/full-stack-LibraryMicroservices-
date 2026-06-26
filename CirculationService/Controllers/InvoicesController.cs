using CirculationService.Data;
using CirculationService.DTOs.Borrows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CirculationService.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly CirculationDbContext _context;

    public InvoicesController(CirculationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetAllInvoices()
    {
        var invoices = await _context.Invoices
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BorrowRecordId = x.BorrowRecordId,
                ReaderId = x.ReaderId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Không tìm thấy hóa đơn" });
        }

        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (role == "Reader" && invoice.ReaderId.ToString() != userId)
        {
            return Forbid();
        }

        return Ok(new InvoiceResponse
        {
            Id = invoice.Id,
            BorrowRecordId = invoice.BorrowRecordId,
            ReaderId = invoice.ReaderId,
            Amount = invoice.Amount,
            Type = invoice.Type,
            Description = invoice.Description,
            CreatedAt = invoice.CreatedAt
        });
    }

    [HttpGet("me")]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> GetMyInvoices()
    {
        var userId = User.FindFirstValue("userId")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var readerId = Guid.Parse(userId);

        var invoices = await _context.Invoices
            .Where(x => x.ReaderId == readerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BorrowRecordId = x.BorrowRecordId,
                ReaderId = x.ReaderId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("reader/{readerId:guid}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetInvoicesByReader(Guid readerId)
    {
        var invoices = await _context.Invoices
            .Where(x => x.ReaderId == readerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BorrowRecordId = x.BorrowRecordId,
                ReaderId = x.ReaderId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("borrow/{borrowRecordId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInvoicesByBorrowRecord(Guid borrowRecordId)
    {
        var invoicesQuery = _context.Invoices.Where(x => x.BorrowRecordId == borrowRecordId);

        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");
        if (role == "Reader")
        {
            var userId = User.FindFirstValue("userId")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var readerId = Guid.Parse(userId);
                invoicesQuery = invoicesQuery.Where(x => x.ReaderId == readerId);
            }
        }

        var invoices = await invoicesQuery
            .OrderBy(x => x.CreatedAt)
            .Select(x => new InvoiceResponse
            {
                Id = x.Id,
                BorrowRecordId = x.BorrowRecordId,
                ReaderId = x.ReaderId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(invoices);
    }
}
