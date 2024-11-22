using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCoreSQLiteExample
{
    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public List<Subject> Subjects { get; set; } = new List<Subject>();
    }

    public class Subject
    {
        public int SubjectId { get; set; }
        public string Title { get; set; }
        public int MaximumCapacity { get; set; }
        public List<Student> Students { get; set; } = new List<Student>();
    }

    public class AppDbContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.Subjects)
                .WithMany(s => s.Students)
                .UsingEntity<Dictionary<string, object>>(
                    "StudentSubject",
                    j => j.HasOne<Subject>().WithMany().HasForeignKey("SubjectId"),
                    j => j.HasOne<Student>().WithMany().HasForeignKey("StudentId"));
        }
    }

    public class Repository
    {
        private readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        public void AddSubject(Subject subject)
        {
            if (subject == null) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(subject.Title)) throw new ArgumentException("Subject title cannot be null or empty.");

            _context.Subjects.Add(subject);
            _context.SaveChanges();
        }

        public void AddStudent(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            if (string.IsNullOrEmpty(student.Name)) throw new ArgumentException("Student name cannot be null or empty.");

            _context.Students.Add(student);
            _context.SaveChanges();
        }

        public void EnrollStudentToSubject(int studentId, int subjectId)
        {
            var student = _context.Students.Include(s => s.Subjects).FirstOrDefault(s => s.StudentId == studentId);
            var subject = _context.Subjects.Include(s => s.Students).FirstOrDefault(s => s.SubjectId == subjectId);

            if (student == null) throw new ArgumentException("Student not found.");
            if (subject == null) throw new ArgumentException("Subject not found.");
            if (subject.Students.Count >= subject.MaximumCapacity) throw new InvalidOperationException("Subject has reached its maximum capacity.");

            student.Subjects.Add(subject);
            subject.Students.Add(student);
            _context.SaveChanges();
        }

        public List<Subject> GetAllSubjects()
        {
            return _context.Subjects.Include(s => s.Students).ToList();
        }

        public List<Student> GetStudentsForSubject(int subjectId)
        {
            var subject = _context.Subjects.Include(s => s.Students).FirstOrDefault(s => s.SubjectId == subjectId);
            return subject?.Students ?? new List<Student>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=school.db");

            using var context = new AppDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();

            var repository = new Repository(context);

            var subject = new Subject
            {
                Title = "Mathematics",
                MaximumCapacity = 30
            };
            repository.AddSubject(subject);

            var student1 = new Student
            {
                Name = "John Doe",
                EnrollmentDate = DateTime.Now
            };
            var student2 = new Student
            {
                Name = "Jane Smith",
                EnrollmentDate = DateTime.Now
            };
            repository.AddStudent(student1);
            repository.AddStudent(student2);

            try
            {
                repository.EnrollStudentToSubject(student1.StudentId, subject.SubjectId);
                repository.EnrollStudentToSubject(student2.StudentId, subject.SubjectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during enrollment: {ex.Message}");
            }

            var subjects = repository.GetAllSubjects();
            foreach (var subj in subjects)
            {
                Console.WriteLine($"Subject: {subj.Title}, Maximum Capacity: {subj.MaximumCapacity}");
                Console.WriteLine("Enrolled Students:");
                foreach (var student in subj.Students)
                {
                    Console.WriteLine($"- {student.Name}");
                }
                Console.WriteLine();
            }
        }
    }
}
