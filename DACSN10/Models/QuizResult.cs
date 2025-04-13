using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class QuizResult
    {
        [Key]
        public int ResultID { get; set; }

        public string UserID { get; set; }
        public User User { get; set; }

        public int QuizID { get; set; }
        public Quiz Quiz { get; set; }

        public double Score { get; set; }
        public DateTime TakenAt { get; set; }
    }
}
