using CirculationService.DTOs.External;
using CirculationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CirculationService.Controllers;

/// <summary>
/// Proxy Controller — Nhóm 2 gọi Nhóm 1 và Nhóm 3 thay cho frontend.
///
/// Frontend chỉ cần gọi Nhóm 2:
///   GET /api/proxy/books?search=...     → lấy sách từ Nhóm 1
///   GET /api/proxy/readers?search=...   → lấy độc giả từ Nhóm 3
///
/// ⚠️ NẾU ENDPOINT NHÓM 1/3 KHÁC: sửa trong CatalogClient.cs và IdentityReportClient.cs
/// </summary>
[ApiController]
[Route("api/proxy")]
public class ProxyController : ControllerBase
{
    private readonly CatalogClient _catalogClient;
    private readonly IdentityReportClient _identityReportClient;

    public ProxyController(CatalogClient catalogClient, IdentityReportClient identityReportClient)
    {
        _catalogClient         = catalogClient;
        _identityReportClient  = identityReportClient;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/proxy/books?search=&onlyAvailable=true
    // Lấy danh sách sách từ Nhóm 1 (Catalog Service).
    // Dùng khi Thủ thư chọn sách để tạo phiếu mượn.
    //   search        — tìm theo tên sách / tác giả / ISBN
    //   onlyAvailable — chỉ lấy sách còn đang có thể mượn (mặc định: false)
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("books")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(List<BookSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] bool onlyAvailable = false)
    {
        // Lấy JWT token từ request hiện tại để forward sang Nhóm 1
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        var books = await _catalogClient.GetBooksAsync(search, onlyAvailable, token);

        return Ok(books);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/proxy/readers?search=
    // Lấy danh sách độc giả từ Nhóm 3 (Identity Service).
    // Dùng khi Thủ thư chọn độc giả để tạo phiếu mượn.
    //   search — tìm theo tên / email / số thẻ độc giả
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("readers")]
    [Authorize(Roles = "Admin,Librarian")]
    [ProducesResponseType(typeof(List<ReaderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReaders([FromQuery] string? search = null)
    {
        // Lấy JWT token từ request hiện tại để forward sang Nhóm 3
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        var readers = await _identityReportClient.GetReadersAsync(search, token);

        return Ok(readers);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/proxy/readers-raw — Debug: xem raw JSON từ Nhóm 3
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("readers-raw")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetReadersRaw([FromQuery] string? search = null)
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var raw = await _identityReportClient.GetReadersRawAsync(search, token);
        return Content(raw, "application/json");
    }
}
