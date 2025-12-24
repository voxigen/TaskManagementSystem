using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class TeacherReviewWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;
        private Tasks _task;
        private List<Submissions> _submissions;
        private int _currentIndex;
        private Submissions _currentSubmission;

        public TeacherReviewWindow(Users user, Tasks task)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            _task = task;
            _submissions = new List<Submissions>();
            _currentIndex = 0;

            LoadSubmissions();
            LoadTaskInfo();
            LoadCurrentSubmission();
            UpdateNavigationButtons();
        }

        private void LoadSubmissions()
        {
            _submissions = _context.Submissions
                .Where(s => s.TaskId == _task.Id)
                .OrderBy(s => s.SubmittedAt)
                .ToList();
        }

        private void LoadTaskInfo()
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == _task.CourseId);
            HeaderText.Text = $"Проверка работ: {_task.Title}";
            TaskInfoText.Text = $"Курс: {course?.Title} | Всего работ: {_submissions.Count}";
            MaxScoreText.Text = $"Макс. балл: {_task.MaxScore ?? 10}";
        }

        private void LoadCurrentSubmission()
        {
            if (_submissions.Count == 0)
            {
                MessageBox.Show("Нет работ для проверки");
                this.Close();
                return;
            }

            _currentSubmission = _submissions[_currentIndex];
            LoadSubmissionData();
        }

        private void LoadSubmissionData()
        {
            var student = _context.Users.FirstOrDefault(u => u.Id == _currentSubmission.StudentUserId);
            var studentRecord = _context.Students.FirstOrDefault(s => s.UserId == _currentSubmission.StudentUserId);

            StudentInfoText.Text = $"Студент: {student?.FullName ?? "Неизвестный"}\n" +
                                  $"Группа: {studentRecord?.StudentGroup ?? "Не указана"}";

            SubmitDateText.Text = $"Отправлено: {_currentSubmission.SubmittedAt:dd.MM.yyyy HH:mm}";
            AnswerText.Text = _currentSubmission.TextContent ?? "Текст ответа отсутствует";
            CommentTextBox.Text = _currentSubmission.TeacherComment ?? "";
            ScoreTextBox.Text = _currentSubmission.Score?.ToString() ?? "0";

            var files = _context.Files.Where(f => f.SubmissionId == _currentSubmission.Id).ToList();
            FilesListBox.ItemsSource = files;

            SetStatusRadioButton(_currentSubmission.Status);
        }

        private void SetStatusRadioButton(string status)
        {
            StatusSubmitted.IsChecked = status == "Submitted" || status == "Under Review";
            StatusReturned.IsChecked = status == "Returned" || status == "Rejected";
            StatusCompleted.IsChecked = status == "Completed" || string.IsNullOrEmpty(status);
        }

        private string GetSelectedStatus()
        {
            if (StatusSubmitted.IsChecked == true) return "Under Review";
            if (StatusReturned.IsChecked == true) return "Returned";
            return "Completed";
        }

        private void UpdateNavigationButtons()
        {
            PrevButton.IsEnabled = _currentIndex > 0;
            NextButton.IsEnabled = _currentIndex < _submissions.Count - 1;

            if (_submissions.Count > 0)
            {
                HeaderText.Text = $"Проверка работ: {_task.Title} ({_currentIndex + 1}/{_submissions.Count})";
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                LoadCurrentSubmission();
                UpdateNavigationButtons();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _submissions.Count - 1)
            {
                _currentIndex++;
                LoadCurrentSubmission();
                UpdateNavigationButtons();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var submission = _context.Submissions.FirstOrDefault(s => s.Id == _currentSubmission.Id);
                if (submission != null)
                {
                    submission.TeacherComment = CommentTextBox.Text.Trim();

                    if (decimal.TryParse(ScoreTextBox.Text, out decimal score))
                    {
                        submission.Score = score;
                    }

                    submission.Status = GetSelectedStatus();
                    _context.SaveChanges();

                    SendNotificationToStudent(submission);

                    MessageBox.Show("Оценка сохранена!");

                    if (_currentIndex < _submissions.Count - 1)
                    {
                        _currentIndex++;
                        LoadCurrentSubmission();
                        UpdateNavigationButtons();
                    }
                    else
                    {
                        MessageBox.Show("Все работы проверены!");
                        DialogResult = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении оценки: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            if (!string.IsNullOrWhiteSpace(ScoreTextBox.Text))
            {
                if (!decimal.TryParse(ScoreTextBox.Text, out decimal score))
                {
                    MessageBox.Show("Введите корректную оценку");
                    ScoreTextBox.Focus();
                    return false;
                }

                if (score < 0 || (_task.MaxScore.HasValue && score > _task.MaxScore.Value))
                {
                    MessageBox.Show($"Оценка должна быть от 0 до {_task.MaxScore ?? 10}");
                    ScoreTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void SendNotificationToStudent(Submissions submission)
        {
            try
            {
                var notification = new Notifications
                {
                    UserId = submission.StudentUserId,
                    Type = "Grade",
                    Title = $"Проверена работа: {_task.Title}",
                    Message = $"Преподаватель поставил оценку: {submission.Score}/{_task.MaxScore ?? 10}",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    RelatedTaskId = _task.Id,
                    RelatedSubmissionId = submission.Id
                };
                _context.Notifications.Add(notification);
                _context.SaveChanges();
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