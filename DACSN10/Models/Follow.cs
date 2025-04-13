namespace DACSN10.Models
{
    public class Follow
    {
        public string FollowerID { get; set; }
        public User Follower { get; set; }

        public string FollowedTeacherID { get; set; }
        public User FollowedTeacher { get; set; }
    }
}
