using BookcaseAPI.Models;
using Xunit;

namespace BookcaseAPI.Tests.Models
{
    public class ModelTests
    {
        [Fact]
        public void Client_Defaults_AreExpected()
        {
            var model = new Client();

            Assert.Equal(string.Empty, model.Username);
            Assert.Equal(string.Empty, model.PasswordHash);
            Assert.Equal("User", model.Role);
            Assert.NotNull(model.Applications);
        }

        [Fact]
        public void Client_Allows_SettingProperties()
        {
            var model = new Client
            {
                Id = 1,
                Username = "user",
                PasswordHash = "hash",
                Role = "Admin"
            };

            Assert.Equal(1, model.Id);
            Assert.Equal("user", model.Username);
            Assert.Equal("hash", model.PasswordHash);
            Assert.Equal("Admin", model.Role);
        }

        [Fact]
        public void Exam_Defaults_AreExpected()
        {
            var model = new Exam();

            Assert.Equal(string.Empty, model.Address);
            Assert.Equal(string.Empty, model.TestName);
            Assert.NotNull(model.ApplicationExams);
            Assert.NotNull(model.MajorExams);
        }

        [Fact]
        public void Exam_Allows_SettingProperties()
        {
            var model = new Exam
            {
                Id = 10,
                Date = new DateTime(2024, 1, 1),
                Address = "Address",
                TestName = "Test",
                ClientId = 3
            };

            Assert.Equal(10, model.Id);
            Assert.Equal(new DateTime(2024, 1, 1), model.Date);
            Assert.Equal("Address", model.Address);
            Assert.Equal("Test", model.TestName);
            Assert.Equal(3, model.ClientId);
        }

        [Fact]
        public void Major_Defaults_AreExpected()
        {
            var model = new Major();

            Assert.Equal(string.Empty, model.Name);
            Assert.Equal(string.Empty, model.UniversityName);
            Assert.Equal(string.Empty, model.Address);
            Assert.Equal(string.Empty, model.Duration);
            Assert.Equal(string.Empty, model.Language);
            Assert.Equal(string.Empty, model.GradingSystem);
            Assert.Equal(string.Empty, model.Notes);
            Assert.Equal(MajorStatus.Liked, model.Status);
            Assert.NotNull(model.Applications);
            Assert.NotNull(model.MajorExams);
        }

        [Fact]
        public void Major_Allows_SettingProperties()
        {
            var model = new Major
            {
                Id = 5,
                Name = "CS",
                UniversityName = "Uni",
                Address = "Addr",
                Duration = "4y",
                Language = "EN",
                GradingSystem = "G",
                Notes = "Notes",
                ClientId = 2,
                Status = MajorStatus.Applied
            };

            Assert.Equal(5, model.Id);
            Assert.Equal("CS", model.Name);
            Assert.Equal("Uni", model.UniversityName);
            Assert.Equal("Addr", model.Address);
            Assert.Equal("4y", model.Duration);
            Assert.Equal("EN", model.Language);
            Assert.Equal("G", model.GradingSystem);
            Assert.Equal("Notes", model.Notes);
            Assert.Equal(2, model.ClientId);
            Assert.Equal(MajorStatus.Applied, model.Status);
        }

        [Fact]
        public void Application_Defaults_AreExpected()
        {
            var model = new Application();

            Assert.Equal(string.Empty, model.Stage);
            Assert.Equal(string.Empty, model.Notes);
            Assert.NotNull(model.ApplicationExams);
        }

        [Fact]
        public void Application_Allows_SettingProperties()
        {
            var model = new Application
            {
                Id = 7,
                MajorId = 1,
                StudentId = 2,
                Deadline = new DateTime(2024, 6, 1),
                Stage = "Stage",
                Notes = "Notes"
            };

            Assert.Equal(7, model.Id);
            Assert.Equal(1, model.MajorId);
            Assert.Equal(2, model.StudentId);
            Assert.Equal(new DateTime(2024, 6, 1), model.Deadline);
            Assert.Equal("Stage", model.Stage);
            Assert.Equal("Notes", model.Notes);
        }

        [Fact]
        public void ApplicationExam_Allows_SettingProperties()
        {
            var model = new ApplicationExam
            {
                ApplicationId = 1,
                ExamId = 2,
                Application = new Application { Id = 1 },
                Exam = new Exam { Id = 2 }
            };

            Assert.Equal(1, model.ApplicationId);
            Assert.Equal(2, model.ExamId);
            Assert.Equal(1, model.Application.Id);
            Assert.Equal(2, model.Exam.Id);
        }

        [Fact]
        public void MajorExam_Allows_SettingProperties()
        {
            var model = new MajorExam
            {
                MajorId = 3,
                ExamId = 4,
                Major = new Major { Id = 3 },
                Exam = new Exam { Id = 4 }
            };

            Assert.Equal(3, model.MajorId);
            Assert.Equal(4, model.ExamId);
            Assert.Equal(3, model.Major.Id);
            Assert.Equal(4, model.Exam.Id);
        }

        [Fact]
        public void MajorStatus_DefaultValue_IsLiked()
        {
            var status = default(MajorStatus);

            Assert.Equal(MajorStatus.Liked, status);
        }
    }
}