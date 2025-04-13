namespace DACSN10.Models
{
    public class FavoriteCourse
    {
        public string UserID { get; set; }
        public User User { get; set; }

        public int CourseID { get; set; }
        public Course Course { get; set; }
    }
}
