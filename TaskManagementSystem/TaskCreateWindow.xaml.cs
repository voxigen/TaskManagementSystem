using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace TaskManagementSystem
{
    public partial class TaskCreateWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;
        private List<string> attachedFiles;
        private Teachers teacher;

        public TaskCreateWindow(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            attachedFiles = new List<string>();

            InitializeWindow();
            LoadCourses();
        }

        private void InitializeWindow()
        {
            if (_currentUser.Role == "Teacher")
            {
                teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
                if (teacher != null)
                {
                    TeacherInfoText.Text = $"Преподаватель: {_currentUser.FullName}";
                }
            }
            else if (_currentUser.Role == "Administrator")
            {
                TeacherInfoText.Text = $"Администратор: {_currentUser.FullName}";
            }

           
            DeadlineDatePicker.SelectedDate = DateTime.Now.AddDays(7);
        }

        private void LoadCourses()
        {
            try
            {
                if (_currentUser.Role == "Teacher" && teacher != null)
                {
                 
                    var courseIds = _context.Database.SqlQuery<int>(
                        "SELECT CourseId FROM CourseTeachers WHERE TeacherUserId = {0}",
                        teacher.UserId).ToList();

                    var courses = _context.Courses
                        .Where(c => courseIds.Contains(c.Id))
                        .ToList();

                    CourseComboBox.ItemsSource = courses;
                }
                else if (_currentUser.Role == "Administrator")
                {
                   
                    var courses = _context.Courses.ToList();
                    CourseComboBox.ItemsSource = courses;
                }

                if (CourseComboBox.Items.Count > 0)
                {
                    CourseComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке курсов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    attachedFiles.Add(filename);
                    FilesListBox.Items.Add(System.IO.Path.GetFileName(filename));
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var selectedCourse = CourseComboBox.SelectedItem as Courses;
                if (selectedCourse == null)
                {
                    MessageBox.Show("Выберите курс");
                    return;
                }

                int teacherUserId;
                if (_currentUser.Role == "Teacher" && teacher != null)
                {
                    teacherUserId = teacher.UserId;
                }
                else if (_currentUser.Role == "Administrator")
                {
                    var courseTeacherId = _context.Database.SqlQuery<int?>(
                        "SELECT TOP 1 TeacherUserId FROM CourseTeachers WHERE CourseId = {0}",
                        selectedCourse.Id).FirstOrDefault();

                    teacherUserId = courseTeacherId ?? _currentUser.Id;
                }
                else
                {
                    MessageBox.Show("Невозможно определить преподавателя для задания");
                    return;
                }

                var newTask = new Tasks
                {
                    Title = TitleTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text.Trim(),
                    Deadline = DeadlineDatePicker.SelectedDate ?? DateTime.Now.AddDays(7),
                    MaxScore = int.TryParse(MaxScoreTextBox.Text, out int maxScore) ? maxScore : 10,
                    CourseId = selectedCourse.Id,
                    TeacherUserId = teacherUserId
                };

                _context.Tasks.Add(newTask);
                _context.SaveChanges();

                foreach (var filePath in attachedFiles)
                {
                    var file = new Files
                    {
                        FileName = System.IO.Path.GetFileName(filePath),
                        FilePath = filePath,
                        FileSize = new System.IO.FileInfo(filePath).Length,
                        UploadedByUserId = _currentUser.Id,
                        TaskId = newTask.Id,
                        UploadedAt = DateTime.Now
                    };
                    _context.Files.Add(file);
                }

                if (attachedFiles.Count > 0)
                {
                    _context.SaveChanges();
                }

                SendNotificationsToStudents(selectedCourse.Id, newTask.Id, newTask.Title);

                MessageBox.Show($"Задание '{newTask.Title}' успешно создано!");

                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании задания: {ex.Message}");
            }
        }

        private void SendNotificationsToStudents(int courseId, int taskId, string taskTitle)
        {
            try
            {
          
                var studentIds = _context.Database.SqlQuery<int>(
                    "SELECT StudentUserId FROM CourseStudents WHERE CourseId = {0}",
                    courseId).ToList();

                foreach (var studentId in studentIds)
                {
                    var notification = new Notifications
                    {
                        UserId = studentId,
                        Type = "NewTask",
                        Title = $"Новое задание: {taskTitle}",
                        Message = $"Добавлено новое задание по курсу. Дедлайн: {DateTime.Now.AddDays(7):dd.MM.yyyy}",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        RelatedTaskId = taskId
                    };
                    _context.Notifications.Add(notification);
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
           
                Console.WriteLine($"Ошибка при отправке уведомлений: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            if (CourseComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите курс");
                CourseComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название задания");
                TitleTextBox.Focus();
                return false;
            }

            if (DeadlineDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите срок выполнения");
                DeadlineDatePicker.Focus();
                return false;
            }

            if (DeadlineDatePicker.SelectedDate < DateTime.Now.Date)
            {
                MessageBox.Show("Срок выполнения не может быть в прошлом");
                DeadlineDatePicker.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MaxScoreTextBox.Text))
            {
                if (!int.TryParse(MaxScoreTextBox.Text, out int score))
                {
                    MessageBox.Show("Введите корректное значение максимального балла");
                    MaxScoreTextBox.Focus();
                    return false;
                }

                if (score <= 0)
                {
                    MessageBox.Show("Максимальный балл должен быть положительным числом");
                    MaxScoreTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}