using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Models
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CourseCategory> CourseCategories { get; set; }
        public DbSet<FavoriteCourse> FavoriteCourses { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<CourseFollow> CourseFollows { get; set; } // Thêm mới

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.User)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.User)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseCategory>()
                .HasKey(cc => new { cc.CourseID, cc.CategoryID });

            modelBuilder.Entity<CourseCategory>()
                .HasOne(cc => cc.Course)
                .WithMany(c => c.CourseCategories)
                .HasForeignKey(cc => cc.CourseID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseCategory>()
                .HasOne(cc => cc.Category)
                .WithMany(c => c.CourseCategories)
                .HasForeignKey(cc => cc.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteCourse>()
                .HasKey(fc => new { fc.UserID, fc.CourseID });

            modelBuilder.Entity<FavoriteCourse>()
                .HasOne(fc => fc.User)
                .WithMany(u => u.FavoriteCourses)
                .HasForeignKey(fc => fc.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FavoriteCourse>()
                .HasOne(fc => fc.Course)
                .WithMany(c => c.FavoriteCourses)
                .HasForeignKey(fc => fc.CourseID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowerID, f.FollowedTeacherID });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowedTeacher)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedTeacherID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CourseID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.User)
                .WithMany(u => u.QuizResults)
                .HasForeignKey(qr => qr.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.Quiz)
                .WithMany(qz => qz.QuizResults)
                .HasForeignKey(qr => qr.QuizID)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho CourseFollow
            modelBuilder.Entity<CourseFollow>()
                .HasKey(cf => new { cf.UserID, cf.CourseID });

            modelBuilder.Entity<CourseFollow>()
                .HasOne(cf => cf.User)
                .WithMany()
                .HasForeignKey(cf => cf.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseFollow>()
                .HasOne(cf => cf.Course)
                .WithMany()
                .HasForeignKey(cf => cf.CourseID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}