
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.RTC;
using System;
using System.Linq;
using System.Security.Claims;


namespace EF_core_assignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "User")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _context;


        public AssetsController(IAssetService assetService, AppDbContext context,  IHubContext<NotificationHub> hubContext)
        {
            _assetService = assetService;
            _hubContext = hubContext;
            _context = context;
        }


   

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                IEnumerable<Asset> assets;

                if (isAdmin)
                {
                    assets = _assetService.GetAll();    
                }
                else
                {
                    assets = _assetService.GetAll().Where(s => s.UserId == userId);
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                // You could log the exception here
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            try
            {
                var asset = _assetService.GetById(id);
                if (asset == null) return NotFound($"Asset with id {id} not found.");
                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async  Task<IActionResult> Create([FromBody] AssetDto dto)
        {
            try
            {

                if (!ModelState.IsValid)
                {

          
                    return BadRequest(ModelState);
                
                }

                if(dto == null)
                {
                    return BadRequest("Device Data Cannot be null..");
                }

                

                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"{claim.Type} : {claim.Value}");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                var asset = new Asset
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    UserId = userId,
                    Signals = dto.Signals.Select(s => new Signal
                    {
                        Name = s.Name,
                        Description = s.Description
                    }).ToList()
                };

              


                var created = _assetService.Create(asset);
                // Notification
                var userName = User.Identity?.Name ?? "Unknown";
                var notification = new Notification
                {
                    UserName = userName,
                    Message = $"created asset '{asset.Name}'"
                };
                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", userName, notification.Message, notification.Id);



                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create asset: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AssetDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedAsset = new Asset
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Signals = dto.Signals.Select(s => new Signal
                    {
                        Name = s.Name,
                        Description = s.Description
                    }).ToList()
                };

                var updated = _assetService.Update(id, updatedAsset);
                if (updated == null) return NotFound($"Asset with id {id} not found.");


                // Notification
                var userName = User.Identity?.Name ?? "Unknown";
                var notification = new Notification
                {
                    UserName = userName,
                    Message = $"updated asset '{updated.Name}'"
                };
                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", userName, notification.Message, notification.Id);


                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update asset: {ex.Message}");
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Delete(Guid id)
            {
                try
                {
                    if (!_assetService.Delete(id)) return NotFound($"Asset with id {id} not found.");

                var userName = User.Identity?.Name ?? "Unknown";
                var notification = new Notification
                {
                    UserName = userName,
                    Message = $"deleted asset with id '{id}'"
                };
                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", userName, notification.Message, notification.Id);


                return Ok("Deleted successfully");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Failed to delete asset: {ex.Message}");
                }
            }
    }
}
