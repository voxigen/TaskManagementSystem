using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace TaskManagementSystem
{
    public partial class StudentSubmitWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;
        private Tasks _task;
        private List<string> _attachedFiles;

        public StudentSubmitWindow(Users user, Tasks task)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            _task = task;
            _attachedFiles = new List<string>();

            LoadTaskInfo();
        }

        private void LoadTaskInfo()
        {
            HeaderText.Text = $"Сдача работы: {_task.Title}";

            var course = _context.Courses.FirstOrDefault(c => c.Id == _task.CourseId);
            TaskInfoText.Text = $"Курс: {course?.Title ?? "Неизвестный курс"}";

            int daysLeft = (int)(_task.Deadline - DateTime.Now).TotalDays;
            DeadlineText.Text = $"Срок сдачи: {_task.Deadline:dd.MM.yyyy}";

            if (daysLeft < 0)
                StatusText.Text = $"⚠️ Просрочено на {-daysLeft} дней";
            else if (daysLeft == 0)
                StatusText.Text = "⚠️ Сдать сегодня";
            else if (daysLeft <= 3)
                StatusText.Text = $"⚠️ Осталось {daysLeft} дня";
            else
                StatusText.Text = $"✅ Осталось {daysLeft} дней";
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*|" +
                        "Документы (*.doc;*.docx;*.pdf;*.txt)|*.doc;*.docx;*.pdf;*.txt|" +
                        "Архивы (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|" +
                        "Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif",
                FilterIndex = 1,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _attachedFiles.Add(filename);
                    FilesListBox.Items.Add(System.IO.Path.GetFileName(filename));
                }
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AnswerTextBox.Text) && _attachedFiles.Count == 0)
            {
                MessageBox.Show("Добавьте текст ответа или прикрепите файлы");
                return;
            }

            try
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
                if (student == null)
                {
                    MessageBox.Show("Ошибка: студент не найден");
                    return;
                }

                var existingSubmission = _context.Submissions
                    .FirstOrDefault(s => s.TaskId == _task.Id && s.StudentUserId == student.UserId);

                if (existingSubmission != null)
                {
                    var result = MessageBox.Show("У вас уже есть отправленная работа. Перезаписать?",
                        "Подтверждение", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                        return;

                    _context.Submissions.Remove(existingSubmission);
                    _context.SaveChanges();
                }

                var submission = new Submissions
                {
                    TaskId = _task.Id,
                    StudentUserId = student.UserId,
                    TextContent = AnswerTextBox.Text.Trim(),
                    SubmittedAt = DateTime.Now,
                    Status = "Submitted"
                };

                _context.Submissions.Add(submission);
                _context.SaveChanges();

                foreach (var filePath in _attachedFiles)
                {
                    var file = new Files
                    {
                        FileName = System.IO.Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileSize = new System.IO.FileInfo(filePath).Length,
                        UploadedByUserId = _currentUser.Id,
                        SubmissionId = submission.Id,
                        UploadedAt = DateTime.Now
                    };
                    _context.Files.Add(file);
                }

                if (_attachedFiles.Count > 0)
                {
                    _context.SaveChanges();
                }

                SendNotificationToTeacher(submission.Id);

                MessageBox.Show("Работа успешно отправлена на проверку!");
                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке работы: {ex.Message}");
            }
        }

        private void SendNotificationToTeacher(int submissionId)
        {
            try
            {
                var teacherId = _context.Tasks
                    .Where(t => t.Id == _task.Id)
                    .Select(t => t.TeacherUserId)
                    .FirstOrDefault();

                if (teacherId > 0)
                {
                    var notification = new Notifications
                    {
                        UserId = teacherId,
                        Type = "NewSubmission",
                        Title = $"Новая работа по заданию: {_task.Title}",
                        Message = $"Студент {_currentUser.FullName} отправил работу",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        RelatedTaskId = _task.Id,
                        RelatedSubmissionId = submissionId
                    };
                    _context.Notifications.Add(notification);
                    _context.SaveChanges();
                }
            }
            catch
            {
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}