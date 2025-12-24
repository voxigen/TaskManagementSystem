using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class UserManagementWindow : Window
    {
        private TaskManagementSystemEntities3 _context;

        public UserManagementWindow()
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();

            LoadUsers();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            NewRoleComboBox.SelectionChanged += (s, e) =>
            {
                var selectedRole = (NewRoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (selectedRole == "Student")
                {
                    StudentFields.Visibility = Visibility.Visible;
                    TeacherFields.Visibility = Visibility.Collapsed;
                }
                else if (selectedRole == "Teacher")
                {
                    StudentFields.Visibility = Visibility.Collapsed;
                    TeacherFields.Visibility = Visibility.Visible;
                }
                else
                {
                    StudentFields.Visibility = Visibility.Collapsed;
                    TeacherFields.Visibility = Visibility.Collapsed;
                }
            };
        }

        private void LoadUsers()
        {
            try
            {
                var users = _context.Users
                    .OrderBy(u => u.Id)
                    .ToList();

                UsersDataGrid.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewLoginTextBox.Text))
                {
                    MessageBox.Show("Введите логин пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewLoginTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    MessageBox.Show("Введите пароль пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewFullNameTextBox.Text))
                {
                    MessageBox.Show("Введите ФИО пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewFullNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewEmailTextBox.Text))
                {
                    MessageBox.Show("Введите email пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewEmailTextBox.Focus();
                    return;
                }

                var selectedRole = (NewRoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrWhiteSpace(selectedRole))
                {
                    MessageBox.Show("Выберите роль пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existingUser = _context.Users.FirstOrDefault(u => u.Login == NewLoginTextBox.Text);
                if (existingUser != null)
                {
                    MessageBox.Show($"Пользователь с логином '{NewLoginTextBox.Text}' уже существует",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string passwordHash = PasswordHasher.ComputeHash(NewPasswordBox.Password);

                var newUser = new Users
                {
                    Login = NewLoginTextBox.Text.Trim(),
                    PasswordHash = passwordHash,
                    FullName = NewFullNameTextBox.Text.Trim(),
                    Email = NewEmailTextBox.Text.Trim(),
                    Role = selectedRole,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                if (selectedRole == "Student")
                {
                    if (string.IsNullOrWhiteSpace(NewGroupTextBox.Text))
                    {
                        MessageBox.Show("Для студента необходимо указать группу",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _context.Users.Remove(newUser);
                        _context.SaveChanges();
                        return;
                    }

                    var newStudent = new Students
                    {
                        UserId = newUser.Id,
                        StudentGroup = NewGroupTextBox.Text.Trim()
                    };
                    _context.Students.Add(newStudent);
                }
                else if (selectedRole == "Teacher")
                {
                    if (string.IsNullOrWhiteSpace(NewDepartmentTextBox.Text))
                    {
                        MessageBox.Show("Для преподавателя необходимо указать отделение",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _context.Users.Remove(newUser);
                        _context.SaveChanges();
                        return;
                    }

                    var newTeacher = new Teachers
                    {
                        UserId = newUser.Id,
                        Department = NewDepartmentTextBox.Text.Trim()
                    };
                    _context.Teachers.Add(newTeacher);
                }

                _context.SaveChanges();

                ClearForm();

                LoadUsers();

                MessageBox.Show($"Пользователь '{newUser.Login}' успешно создан!\n" +
                               $"Роль: {newUser.Role}\n" +
                               $"Пароль: {NewPasswordBox.Password}\n\n" +
                               "Запомните пароль для тестирования авторизации!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UsersDataGrid.SelectedItem as Users;
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedUser.Login == "admin" && selectedUser.Role == "Administrator")
            {
                MessageBox.Show("Нельзя удалить основного администратора системы",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string warningMessage = $"Вы уверены, что хотите удалить пользователя?\n\n" +
                                  $"Логин: {selectedUser.Login}\n" +
                                  $"ФИО: {selectedUser.FullName}\n" +
                                  $"Роль: {selectedUser.Role}\n\n";

            if (selectedUser.Role == "Student")
            {
                warningMessage += "⚠️ БУДУТ УДАЛЕНЫ:\n" +
                               "• Запись студента\n" +
                               "• Все зачисления на курсы\n" +
                               "• Все отправленные работы и оценки\n" +
                               "• Все прикрепленные файлы\n" +
                               "• Все уведомления\n\n";
            }
            else if (selectedUser.Role == "Teacher")
            {
                warningMessage += "⚠️ БУДУТ УДАЛЕНЫ:\n" +
                               "• Запись преподавателя\n" +
                               "• Все прикрепления к курсам\n" +
                               "• Все созданные задания\n" +
                               "• Все проверенные работы и оценки\n" +
                               "• Все комментарии к работам\n" +
                               "• Все прикрепленные файлы\n" +
                               "• Все уведомления\n\n";
            }
            else if (selectedUser.Role == "Administrator")
            {
                warningMessage += "⚠️ БУДУТ УДАЛЕНЫ:\n" +
                               "• Запись администратора\n" +
                               "• Все уведомления\n" +
                               "• Все действия в системе\n\n";
            }

            warningMessage += "❗ Это действие нельзя отменить!";

            var result = MessageBox.Show(warningMessage,
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (selectedUser.Role == "Student")
                    {
                        DeleteStudentData(selectedUser.Id);
                    }
                    else if (selectedUser.Role == "Teacher")
                    {
                        DeleteTeacherData(selectedUser.Id);
                    }
                    else if (selectedUser.Role == "Administrator")
                    {
                        DeleteAdministratorData(selectedUser.Id);
                    }

                    var userToDelete = _context.Users.FirstOrDefault(u => u.Id == selectedUser.Id);
                    if (userToDelete != null)
                    {
                        _context.Users.Remove(userToDelete);
                    }

                    _context.SaveChanges();

                    MessageBox.Show($"Пользователь '{selectedUser.Login}' успешно удален",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteStudentData(int userId)
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == userId);
            if (student != null)
            {
                var submissions = _context.Submissions.Where(s => s.StudentUserId == userId).ToList();

                foreach (var submission in submissions)
                {
                    var submissionFiles = _context.Files.Where(f => f.SubmissionId == submission.Id).ToList();
                    foreach (var file in submissionFiles)
                    {
                        _context.Files.Remove(file);
                    }

                    _context.Submissions.Remove(submission);
                }

                _context.Database.ExecuteSqlCommand("DELETE FROM CourseStudents WHERE StudentUserId = {0}", userId);

                var studentNotifications = _context.Notifications.Where(n => n.UserId == userId).ToList();
                foreach (var notification in studentNotifications)
                {
                    _context.Notifications.Remove(notification);
                }

                _context.Students.Remove(student);
            }
        }

        private void DeleteTeacherData(int userId)
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == userId);
            if (teacher != null)
            {
                var teacherTasks = _context.Tasks.Where(t => t.TeacherUserId == userId).ToList();

                foreach (var task in teacherTasks)
                {
                    var taskSubmissions = _context.Submissions.Where(s => s.TaskId == task.Id).ToList();

                    foreach (var submission in taskSubmissions)
                    {
                        var submissionFiles = _context.Files.Where(f => f.SubmissionId == submission.Id).ToList();
                        foreach (var file in submissionFiles)
                        {
                            _context.Files.Remove(file);
                        }

                        var submissionNotifications = _context.Notifications.Where(n => n.RelatedSubmissionId == submission.Id).ToList();
                        foreach (var notification in submissionNotifications)
                        {
                            _context.Notifications.Remove(notification);
                        }

                        _context.Submissions.Remove(submission);
                    }

                    var taskFiles = _context.Files.Where(f => f.TaskId == task.Id).ToList();
                    foreach (var file in taskFiles)
                    {
                        _context.Files.Remove(file);
                    }

                    var taskNotifications = _context.Notifications.Where(n => n.RelatedTaskId == task.Id).ToList();
                    foreach (var notification in taskNotifications)
                    {
                        _context.Notifications.Remove(notification);
                    }

                    _context.Tasks.Remove(task);
                }

                _context.Database.ExecuteSqlCommand("DELETE FROM CourseTeachers WHERE TeacherUserId = {0}", userId);

                var teacherFiles = _context.Files.Where(f => f.UploadedByUserId == userId).ToList();
                foreach (var file in teacherFiles)
                {
                    _context.Files.Remove(file);
                }

                var teacherNotifications = _context.Notifications.Where(n => n.UserId == userId).ToList();
                foreach (var notification in teacherNotifications)
                {
                    _context.Notifications.Remove(notification);
                }

                _context.Teachers.Remove(teacher);
            }
        }

        private void DeleteAdministratorData(int userId)
        {
            var adminTasks = _context.Tasks.Where(t => t.TeacherUserId == userId).ToList();

            foreach (var task in adminTasks)
            {
                var taskSubmissions = _context.Submissions.Where(s => s.TaskId == task.Id).ToList();

                foreach (var submission in taskSubmissions)
                {
                    var submissionFiles = _context.Files.Where(f => f.SubmissionId == submission.Id).ToList();
                    foreach (var file in submissionFiles)
                    {
                        _context.Files.Remove(file);
                    }

                    var submissionNotifications = _context.Notifications.Where(n => n.RelatedSubmissionId == submission.Id).ToList();
                    foreach (var notification in submissionNotifications)
                    {
                        _context.Notifications.Remove(notification);
                    }

                    _context.Submissions.Remove(submission);
                }

                var taskFiles = _context.Files.Where(f => f.TaskId == task.Id).ToList();
                foreach (var file in taskFiles)
                {
                    _context.Files.Remove(file);
                }

                var taskNotifications = _context.Notifications.Where(n => n.RelatedTaskId == task.Id).ToList();
                foreach (var notification in taskNotifications)
                {
                    _context.Notifications.Remove(notification);
                }

                _context.Tasks.Remove(task);
            }

            var adminFiles = _context.Files.Where(f => f.UploadedByUserId == userId).ToList();
            foreach (var file in adminFiles)
            {
                _context.Files.Remove(file);
            }

            var adminNotifications = _context.Notifications.Where(n => n.UserId == userId).ToList();
            foreach (var notification in adminNotifications)
            {
                _context.Notifications.Remove(notification);
            }
        }

        private void ClearForm()
        {
            NewLoginTextBox.Clear();
            NewPasswordBox.Clear();
            NewFullNameTextBox.Clear();
            NewEmailTextBox.Clear();
            NewGroupTextBox.Clear();
            NewDepartmentTextBox.Clear();
            NewRoleComboBox.SelectedIndex = 0;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }
}