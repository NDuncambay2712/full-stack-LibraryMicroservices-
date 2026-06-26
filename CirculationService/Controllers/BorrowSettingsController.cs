using CirculationService.DTOs.Borrows;
using CirculationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CirculationService.Controllers;

/// <summary>
/// Quản lý quy tắc mượn trả sách.
///
/// Phân quyền:
///   GET  → Admin + Librarian (xem cấu hình hiện tại)
///   PUT  → Admin only        (cấu hình quy tắc mượn trả)
/// </summary>
[ApiController]
[Route("api/borrow-settings")]
public class BorrowSettingsController : ControllerBase
{
    private readonly BorrowSettingsService _settings;

    public BorrowSettingsController(BorrowSettingsService settings)
    {
        _settings = settings;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/borrow-settings
    // Admin & Librarian xem cấu hình quy tắc hiện tại
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(BorrowSettingsResponse), StatusCodes.Status200OK)]
    public IActionResult GetSettings()
    {
        return Ok(new BorrowSettingsResponse
        {
            MaxBorrowingBooks = _settings.MaxBorrowingBooks,
            FinePerLateDay    = _settings.FinePerLateDay
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUT /api/borrow-settings
    // CHỈ Admin được cấu hình quy tắc mượn trả:
    //   - MaxBorrowingBooks : số sách tối đa được mượn cùng lúc
    //   - FinePerLateDay    : tiền phạt mỗi ngày quá hạn (VNĐ)
    // ─────────────────────────────────────────────────────────────────────
    [HttpPut]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BorrowSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult UpdateSettings([FromBody] UpdateBorrowSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settings.Update(request.MaxBorrowingBooks, request.FinePerLateDay);

        return Ok(new
        {
            message = "Cập nhật quy tắc mượn trả thành công",
            data = new BorrowSettingsResponse
            {
                MaxBorrowingBooks = _settings.MaxBorrowingBooks,
                FinePerLateDay    = _settings.FinePerLateDay
            }
        });
    }
}
