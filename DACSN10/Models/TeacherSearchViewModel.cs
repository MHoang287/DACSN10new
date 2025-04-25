namespace DACSN10.Models
{
    public class TeacherSearchViewModel
    {
        public User Teacher { get; set; }
        public List<Course> Courses { get; set; }
        public bool IsFollowed { get; set; }
    }
}