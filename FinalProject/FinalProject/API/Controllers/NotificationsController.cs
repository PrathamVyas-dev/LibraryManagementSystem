using FinalProject.Application.Dto.Notification;
using FinalProject.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view all notifications

        public async Task<IActionResult> GetAllNotifications()
        {
            var notifications = await _notificationService.GetAllNotificationsAsync();
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own notifications

        public async Task<IActionResult> GetNotificationById(int id)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id);
            if (notification == null) return NotFound();
            return Ok(notification);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can create manual notifications

        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createDto)
        {
            await _notificationService.AddNotificationAsync(createDto);
            return CreatedAtAction(nameof(GetNotificationById), new { id = createDto.MemberID }, createDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can update notifications
        public async Task<IActionResult> UpdateNotification(int id, [FromBody] CreateNotificationDto updateDto)
        {
            await _notificationService.UpdateNotificationAsync(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can delete their notifications

        public async Task<IActionResult> DeleteNotification(int id)
        {
            await _notificationService.DeleteNotificationAsync(id);
            return NoContent();
        }

        [HttpGet("member/{memberId}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their notifications

        public async Task<IActionResult> GetNotificationsForMember(int memberId)
        {
            var notifications = await _notificationService.GetNotificationsForMemberAsync(memberId);
            return Ok(notifications);
        }

        [HttpGet("member/name/{name}")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view notifications by member name
        public async Task<IActionResult> GetNotificationsByMemberName(string name)
        {
            var notifications = await _notificationService.GetNotificationsByMemberNameAsync(name);
            if (notifications == null || !notifications.Any()) return NotFound(new { Message = "No notifications found for the specified member name." });
            return Ok(notifications);
        }

        [HttpPost("notify-due-books")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can trigger due book notifications

        public async Task<IActionResult> NotifyForDueBooks()
        {
            await _notificationService.NotifyForDueBooksAsync();
            return Ok(new { Message = "Notifications for due books sent successfully." });
        }

        [HttpPost("notify-overdue-books")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can trigger overdue book notifications
        public async Task<IActionResult> NotifyForOverdueBooks()
        {
            await _notificationService.NotifyForOverdueBooksAsync();
            return Ok(new { Message = "Notifications for overdue books sent successfully." });
        }

        [HttpPost("notify-fine-payment/{fineId}")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can trigger fine payment notifications
        public async Task<IActionResult> NotifyForFinePayment(int fineId)
        {
            await _notificationService.NotifyForFinePaymentAsync(fineId);
            return Ok(new { Message = "Notification for fine payment sent successfully." });
        }

        [HttpPost("notify-membership-status/{memberId}/{newStatus}")]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> NotifyForMembershipStatusChange(int memberId, string newStatus)
        {
            await _notificationService.NotifyForMembershipStatusChangeAsync(memberId, newStatus);
            return Ok(new { Message = "Notification for membership status change sent successfully." });
        }


        [HttpPost("perform-periodic-checks")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can perform periodic checks
        public async Task<IActionResult> PerformPeriodicChecks()
        {
            await _notificationService.PerformPeriodicChecksAsync();
            return Ok(new { Message = "Periodic checks completed successfully." });
        }
    }
}
