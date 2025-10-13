
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Infrastructure.Data;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    // Get all notifications (optional: for a single user if needed)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await _context.Notification
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return Ok(notifications);
    }

    // Mark notification as read
    [HttpPost("mark-as-read/{id}")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _context.Notification.FindAsync(id);
        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok();
    }
}
