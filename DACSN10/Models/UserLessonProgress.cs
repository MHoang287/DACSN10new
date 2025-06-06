using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACSN10.Models
{
    public class UserLessonProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserID { get; set; }

        [Required]
        public int LessonID { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("LessonID")]
        public virtual Lesson Lesson { get; set; }
    }
}