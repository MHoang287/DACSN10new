namespace DACSN10.Models
{
    public class CourseFollow
    {
        public string UserID { get; set; }
        public User User { get; set; }

        public int CourseID { get; set; }
        public Course Course { get; set; }

        public DateTime FollowDate { get; set; }
    }
}