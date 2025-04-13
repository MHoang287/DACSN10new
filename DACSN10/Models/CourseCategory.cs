namespace DACSN10.Models
{
    public class CourseCategory
    {
        public int CourseID { get; set; }
        public Course Course { get; set; }

        public int CategoryID { get; set; }
        public Category Category { get; set; }
    }
}
