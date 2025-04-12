namespace DACSN10.Models
{
    public class Submission
    {
        public int SubmissionID { get; set; }
        public DateTime NgayNop { get; set; }
        public string FileNop { get; set; }
        public double? Diem { get; set; }

        public int AssignmentID { get; set; }
        public Assignment Assignment { get; set; }

        public string UserID { get; set; }
        public User User { get; set; }
    }

}
