using System;
using System.ComponentModel.DataAnnotations;

namespace PrintMate.Terminal.Models
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public int? AutoCloseSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public Notification()
        {
            CreatedAt = DateTime.Now;
            IsRead = false;
        }
    }
}
