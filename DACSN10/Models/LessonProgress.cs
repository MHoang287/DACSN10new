namespace DACSN10.Models
{
    public class LessonProgress
    {
        public int LessonProgressID { get; set; }
        public int LessonID { get; set; }
        public string UserID { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double WatchedSeconds { get; set; } // số giây đã xem với video
        public Lesson Lesson { get; set; }
        public User User { get; set; }
    }
}
