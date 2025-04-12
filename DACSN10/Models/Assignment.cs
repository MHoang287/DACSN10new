namespace DACSN10.Models
{
    public class Assignment
    {
        public int AssignmentID { get; set; }
        public string TenBaiTap { get; set; }
        public string MoTa { get; set; }
        public DateTime HanNop { get; set; }

        public int CourseID { get; set; }
        public Course Course { get; set; }

        public ICollection<Submission> Submissions { get; set; }
    }

}
