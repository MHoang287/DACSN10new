namespace DACSN10.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public ICollection<CourseCategory> CourseCategories { get; set; }
    }
}
