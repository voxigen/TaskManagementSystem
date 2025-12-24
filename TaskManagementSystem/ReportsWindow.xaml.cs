using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class ReportsWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private DataTable _currentReportData;
        private bool _isInitialized = false;
        private Users _currentUser;

        public ReportsWindow(Users currentUser = null)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = currentUser;

          
            if (_currentUser?.Role != "Administrator")
            {
                ReportTypeUsers.Visibility = Visibility.Collapsed;
            }

           
            this.Loaded += (s, e) => InitializeControls();

            StartDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
        }

        private void InitializeControls()
        {
            _isInitialized = true;

            PeriodAll.Checked += Period_Checked;
            PeriodMonth.Checked += Period_Checked;
            PeriodWeek.Checked += Period_Checked;
            PeriodCustom.Checked += Period_Checked;

            ReportTypeCourses.Checked += ReportType_Checked;
            ReportTypeTasks.Checked += ReportType_Checked;
            ReportTypeUsers.Checked += ReportType_Checked;
            ReportTypeSubmissions.Checked += ReportType_Checked;
        }

        private void Period_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            CustomPeriodPanel.Visibility = PeriodCustom.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ReportType_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            ExportButton.IsEnabled = false;
            ExportPDFButton.IsEnabled = false;
        }

        private void CalculatePeriod()
        {
            if (PeriodAll.IsChecked == true)
            {
                _startDate = null;
                _endDate = null;
            }
            else if (PeriodMonth.IsChecked == true)
            {
                _startDate = DateTime.Now.AddMonths(-1);
                _endDate = DateTime.Now;
            }
            else if (PeriodWeek.IsChecked == true)
            {
                _startDate = DateTime.Now.AddDays(-7);
                _endDate = DateTime.Now;
            }
            else if (PeriodCustom.IsChecked == true)
            {
                if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите даты для произвольного периода");
                    return;
                }
                _startDate = StartDatePicker.SelectedDate.Value;
                _endDate = EndDatePicker.SelectedDate.Value;

                if (_startDate > _endDate)
                {
                    MessageBox.Show("Начальная дата не может быть позже конечной");
                    return;
                }
            }
        }

        private void GenerateCoursesReport()
        {
            ReportTitle.Text = "Отчет по курсам";
            ReportInfo.Text = _startDate.HasValue ? $"Период: {_startDate:dd.MM.yyyy} - {_endDate:dd.MM.yyyy}" : "За все время";

            _currentReportData = new DataTable();
            _currentReportData.Columns.Add("ID", typeof(int));
            _currentReportData.Columns.Add("Название курса", typeof(string));
            _currentReportData.Columns.Add("Описание", typeof(string));
            _currentReportData.Columns.Add("Студентов", typeof(int));
            _currentReportData.Columns.Add("Преподавателей", typeof(int));
            _currentReportData.Columns.Add("Заданий", typeof(int));
            _currentReportData.Columns.Add("Отправок", typeof(int));
            _currentReportData.Columns.Add("Средний балл", typeof(double));

            var courses = _context.Courses.ToList();
            int totalStudents = 0;
            int totalTeachers = 0;
            int totalTasks = 0;
            int totalSubmissions = 0;
            double totalAvgScore = 0;
            int coursesWithScore = 0;

            foreach (var course in courses)
            {
                var studentCount = _context.Database.SqlQuery<int?>(
                    "SELECT COUNT(*) FROM CourseStudents WHERE CourseId = {0}",
                    course.Id).FirstOrDefault() ?? 0;

                var teacherCount = _context.Database.SqlQuery<int?>(
                    "SELECT COUNT(*) FROM CourseTeachers WHERE CourseId = {0}",
                    course.Id).FirstOrDefault() ?? 0;

                var taskCount = _context.Tasks.Count(t => t.CourseId == course.Id);

                int submissionCount;
                if (_startDate.HasValue && _endDate.HasValue)
                {
                    submissionCount = _context.Submissions
                        .Count(s => s.Tasks.CourseId == course.Id &&
                               s.SubmittedAt >= _startDate && s.SubmittedAt <= _endDate);
                }
                else
                {
                    submissionCount = _context.Submissions
                        .Count(s => s.Tasks.CourseId == course.Id);
                }

                double avgScore;
                if (_startDate.HasValue && _endDate.HasValue)
                {
                    avgScore = _context.Database.SqlQuery<double?>(
                        "SELECT AVG(CAST(Score AS FLOAT)) FROM Submissions s " +
                        "INNER JOIN Tasks t ON s.TaskId = t.Id " +
                        "WHERE t.CourseId = {0} AND s.SubmittedAt >= {1} AND s.SubmittedAt <= {2} AND s.Score IS NOT NULL",
                        course.Id, _startDate, _endDate).FirstOrDefault() ?? 0;
                }
                else
                {
                    avgScore = _context.Database.SqlQuery<double?>(
                        "SELECT AVG(CAST(Score AS FLOAT)) FROM Submissions s " +
                        "INNER JOIN Tasks t ON s.TaskId = t.Id " +
                        "WHERE t.CourseId = {0} AND s.Score IS NOT NULL",
                        course.Id).FirstOrDefault() ?? 0;
                }

                _currentReportData.Rows.Add(
                    course.Id,
                    course.Title,
                    course.Description ?? "",
                    studentCount,
                    teacherCount,
                    taskCount,
                    submissionCount,
                    Math.Round(avgScore, 2)
                );

                totalStudents += studentCount;
                totalTeachers += teacherCount;
                totalTasks += taskCount;
                totalSubmissions += submissionCount;
                if (avgScore > 0)
                {
                    totalAvgScore += avgScore;
                    coursesWithScore++;
                }
            }

            ReportDataGrid.ItemsSource = _currentReportData.DefaultView;
            SetupGridColumns();

            double overallAvgScore = coursesWithScore > 0 ? totalAvgScore / coursesWithScore : 0;
            SummaryText.Text = $"Всего курсов: {courses.Count}\n" +
                              $"Всего студентов: {totalStudents}\n" +
                              $"Всего преподавателей: {totalTeachers}\n" +
                              $"Всего заданий: {totalTasks}\n" +
                              $"Всего отправок: {totalSubmissions}\n" +
                              $"Средний балл по курсам: {Math.Round(overallAvgScore, 2)}";

            RowCountText.Text = courses.Count.ToString();
        }

        private void GenerateTasksReport()
        {
            ReportTitle.Text = "Отчет по заданиям";
            ReportInfo.Text = _startDate.HasValue ? $"Период создания заданий: {_startDate:dd.MM.yyyy} - {_endDate:dd.MM.yyyy}" : "За все время";

            _currentReportData = new DataTable();
            _currentReportData.Columns.Add("ID", typeof(int));
            _currentReportData.Columns.Add("Название задания", typeof(string));
            _currentReportData.Columns.Add("Курс", typeof(string));
            _currentReportData.Columns.Add("Преподаватель", typeof(string));
            _currentReportData.Columns.Add("Срок выполнения", typeof(DateTime));
            _currentReportData.Columns.Add("Макс. балл", typeof(int));
            _currentReportData.Columns.Add("Отправок", typeof(int));
            _currentReportData.Columns.Add("Проверено", typeof(int));
            _currentReportData.Columns.Add("Средний балл", typeof(double));

            var tasks = _context.Tasks.ToList();

            if (_startDate.HasValue && _endDate.HasValue)
            {
                tasks = tasks
                    .Where(t => t.Deadline >= _startDate && t.Deadline <= _endDate)
                    .ToList();
            }

            int totalSubmissions = 0;
            int totalReviewed = 0;
            double totalAvgScore = 0;
            int tasksWithScore = 0;

            foreach (var task in tasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                var teacher = _context.Users.FirstOrDefault(u => u.Id == task.TeacherUserId);

                var submissionCount = _context.Submissions.Count(s => s.TaskId == task.Id);
                var reviewedCount = _context.Submissions.Count(s => s.TaskId == task.Id &&
                    (s.Status == "Completed" || s.Status == "Returned"));

                var avgScore = _context.Database.SqlQuery<double?>(
                    "SELECT AVG(CAST(Score AS FLOAT)) FROM Submissions " +
                    "WHERE TaskId = {0} AND Score IS NOT NULL",
                    task.Id).FirstOrDefault() ?? 0;

                _currentReportData.Rows.Add(
                    task.Id,
                    task.Title,
                    course?.Title ?? "Неизвестно",
                    teacher?.FullName ?? "Неизвестно",
                    task.Deadline,
                    task.MaxScore,
                    submissionCount,
                    reviewedCount,
                    Math.Round(avgScore, 2)
                );

                totalSubmissions += submissionCount;
                totalReviewed += reviewedCount;
                if (avgScore > 0)
                {
                    totalAvgScore += avgScore;
                    tasksWithScore++;
                }
            }

            ReportDataGrid.ItemsSource = _currentReportData.DefaultView;
            SetupGridColumns();

            double overallAvgScore = tasksWithScore > 0 ? totalAvgScore / tasksWithScore : 0;
            SummaryText.Text = $"Всего заданий: {tasks.Count}\n" +
                              $"Всего отправок: {totalSubmissions}\n" +
                              $"Проверено работ: {totalReviewed}\n" +
                              $"Средний балл по заданиям: {Math.Round(overallAvgScore, 2)}\n" +
                              $"Процент проверки: {(totalSubmissions > 0 ? Math.Round((double)totalReviewed / totalSubmissions * 100, 1) : 0)}%";

            RowCountText.Text = tasks.Count.ToString();
        }

        private void GenerateUsersReport()
        {
            if (_currentUser?.Role != "Administrator")
            {
                MessageBox.Show("У вас нет прав для просмотра отчета по пользователям", "Ошибка доступа");
                ReportTypeCourses.IsChecked = true;
                return;
            }

            ReportTitle.Text = "Отчет по пользователям";
            ReportInfo.Text = _startDate.HasValue ? $"Период активности: {_startDate:dd.MM.yyyy} - {_endDate:dd.MM.yyyy}" : "За все время";

            _currentReportData = new DataTable();
            _currentReportData.Columns.Add("ID", typeof(int));
            _currentReportData.Columns.Add("ФИО", typeof(string));
            _currentReportData.Columns.Add("Логин", typeof(string));
            _currentReportData.Columns.Add("Роль", typeof(string));
            _currentReportData.Columns.Add("Email", typeof(string));
            _currentReportData.Columns.Add("Дата регистрации", typeof(DateTime));
            _currentReportData.Columns.Add("Курсов", typeof(int));
            _currentReportData.Columns.Add("Заданий", typeof(int));
            _currentReportData.Columns.Add("Отправок", typeof(int));
            _currentReportData.Columns.Add("Средний балл", typeof(double));

            var users = _context.Users.ToList();
            int totalUsers = 0;
            int totalStudents = 0;
            int totalTeachers = 0;
            int totalAdmins = 0;

            foreach (var user in users)
            {
                int courseCount = 0;
                int taskCount = 0;
                int submissionCount = 0;
                double avgScore = 0;

                if (user.Role == "Student")
                {
                    courseCount = _context.Database.SqlQuery<int?>(
                        "SELECT COUNT(*) FROM CourseStudents WHERE StudentUserId = {0}",
                        user.Id).FirstOrDefault() ?? 0;

                  
                    if (_startDate.HasValue && _endDate.HasValue)
                    {
                        submissionCount = _context.Submissions
                            .Count(s => s.StudentUserId == user.Id &&
                                   s.SubmittedAt >= _startDate && s.SubmittedAt <= _endDate);
                    }
                    else
                    {
                        submissionCount = _context.Submissions
                            .Count(s => s.StudentUserId == user.Id);
                    }

                   
                    if (_startDate.HasValue && _endDate.HasValue)
                    {
                        avgScore = _context.Database.SqlQuery<double?>(
                            "SELECT AVG(CAST(Score AS FLOAT)) FROM Submissions " +
                            "WHERE StudentUserId = {0} AND Score IS NOT NULL AND SubmittedAt >= {1} AND SubmittedAt <= {2}",
                            user.Id, _startDate, _endDate).FirstOrDefault() ?? 0;
                    }
                    else
                    {
                        avgScore = _context.Database.SqlQuery<double?>(
                            "SELECT AVG(CAST(Score AS FLOAT)) FROM Submissions " +
                            "WHERE StudentUserId = {0} AND Score IS NOT NULL",
                            user.Id).FirstOrDefault() ?? 0;
                    }

                    totalStudents++;
                }
                else if (user.Role == "Teacher")
                {
                    courseCount = _context.Database.SqlQuery<int?>(
                        "SELECT COUNT(*) FROM CourseTeachers WHERE TeacherUserId = {0}",
                        user.Id).FirstOrDefault() ?? 0;

                   
                    if (_startDate.HasValue && _endDate.HasValue)
                    {
                        taskCount = _context.Tasks
                            .Count(t => t.TeacherUserId == user.Id &&
                                   t.Deadline >= _startDate && t.Deadline <= _endDate);
                    }
                    else
                    {
                        taskCount = _context.Tasks
                            .Count(t => t.TeacherUserId == user.Id);
                    }

                    totalTeachers++;
                }
                else if (user.Role == "Administrator")
                {
                    totalAdmins++;
                }

                _currentReportData.Rows.Add(
                    user.Id,
                    user.FullName,
                    user.Login,
                    GetRoleDisplayName(user.Role),
                    user.Email ?? "",
                    user.CreatedAt,
                    courseCount,
                    taskCount,
                    submissionCount,
                    Math.Round(avgScore, 2)
                );

                totalUsers++;
            }

            ReportDataGrid.ItemsSource = _currentReportData.DefaultView;
            SetupGridColumns();

            SummaryText.Text = $"Всего пользователей: {totalUsers}\n" +
                              $"Студентов: {totalStudents}\n" +
                              $"Преподавателей: {totalTeachers}\n" +
                              $"Администраторов: {totalAdmins}\n" +
                              $"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}";

            RowCountText.Text = users.Count.ToString();
        }

        private void GenerateSubmissionsReport()
        {
            ReportTitle.Text = "Отчет по отправкам работ";
            ReportInfo.Text = _startDate.HasValue ? $"Период отправок: {_startDate:dd.MM.yyyy} - {_endDate:dd.MM.yyyy}" : "За все время";

            _currentReportData = new DataTable();
            _currentReportData.Columns.Add("ID", typeof(int));
            _currentReportData.Columns.Add("Задание", typeof(string));
            _currentReportData.Columns.Add("Студент", typeof(string));
            _currentReportData.Columns.Add("Курс", typeof(string));
            _currentReportData.Columns.Add("Дата отправки", typeof(DateTime));
            _currentReportData.Columns.Add("Статус", typeof(string));
            _currentReportData.Columns.Add("Оценка", typeof(decimal));
            _currentReportData.Columns.Add("Макс. балл", typeof(int));
            _currentReportData.Columns.Add("Преподаватель", typeof(string));
            _currentReportData.Columns.Add("Комментарий", typeof(string));

            var submissions = _context.Submissions.ToList();

            if (_startDate.HasValue && _endDate.HasValue)
            {
                submissions = submissions
                    .Where(s => s.SubmittedAt >= _startDate && s.SubmittedAt <= _endDate)
                    .ToList();
            }

            int totalSubmitted = 0;
            int totalCompleted = 0;
            int totalReturned = 0;
            int totalUnderReview = 0;
            decimal totalScore = 0;
            int scoredSubmissions = 0;

            foreach (var submission in submissions)
            {
                var task = _context.Tasks.FirstOrDefault(t => t.Id == submission.TaskId);
                var student = _context.Users.FirstOrDefault(u => u.Id == submission.StudentUserId);
                var course = task != null ? _context.Courses.FirstOrDefault(c => c.Id == task.CourseId) : null;
                var teacher = task != null ? _context.Users.FirstOrDefault(u => u.Id == task.TeacherUserId) : null;

                _currentReportData.Rows.Add(
                    submission.Id,
                    task?.Title ?? "Неизвестно",
                    student?.FullName ?? "Неизвестно",
                    course?.Title ?? "Неизвестно",
                    submission.SubmittedAt,
                    GetStatusDisplayName(submission.Status),
                    submission.Score ?? 0,
                    task?.MaxScore ?? 0,
                    teacher?.FullName ?? "Неизвестно",
                    submission.TeacherComment ?? ""
                );

                totalSubmitted++;
                if (submission.Status == "Completed") totalCompleted++;
                if (submission.Status == "Returned" || submission.Status == "Rejected") totalReturned++;
                if (submission.Status == "Under Review") totalUnderReview++;

                if (submission.Score.HasValue)
                {
                    totalScore += submission.Score.Value;
                    scoredSubmissions++;
                }
            }

            ReportDataGrid.ItemsSource = _currentReportData.DefaultView;
            SetupGridColumns();

            decimal avgScore = scoredSubmissions > 0 ? totalScore / scoredSubmissions : 0;
            SummaryText.Text = $"Всего отправок: {totalSubmitted}\n" +
                              $"Проверено: {totalCompleted}\n" +
                              $"Возвращено: {totalReturned}\n" +
                              $"На проверке: {totalUnderReview}\n" +
                              $"Средняя оценка: {Math.Round(avgScore, 2)}\n" +
                              $"Процент проверки: {(totalSubmitted > 0 ? Math.Round((double)totalCompleted / totalSubmitted * 100, 1) : 0)}%";

            RowCountText.Text = submissions.Count.ToString();
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role)
            {
                case "Student":
                    return "Студент";
                case "Teacher":
                    return "Преподаватель";
                case "Administrator":
                    return "Администратор";
                default:
                    return role;
            }
        }

        private string GetStatusDisplayName(string status)
        {
            switch (status)
            {
                case "Submitted":
                    return "Отправлено";
                case "Under Review":
                    return "На проверке";
                case "Completed":
                    return "Проверено";
                case "Returned":
                    return "Возвращено";
                case "Rejected":
                    return "Отклонено";
                default:
                    return status;
            }
        }

        private void SetupGridColumns()
        {
            ReportDataGrid.Columns.Clear();

            if (_currentReportData == null) return;

            foreach (DataColumn column in _currentReportData.Columns)
            {
                var dataGridColumn = new DataGridTextColumn
                {
                    Header = column.ColumnName,
                    Binding = new System.Windows.Data.Binding(column.ColumnName)
                };

                ReportDataGrid.Columns.Add(dataGridColumn);
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CalculatePeriod();

                if (ReportTypeCourses.IsChecked == true)
                    GenerateCoursesReport();
                else if (ReportTypeTasks.IsChecked == true)
                    GenerateTasksReport();
                else if (ReportTypeUsers.IsChecked == true)
                    GenerateUsersReport();
                else if (ReportTypeSubmissions.IsChecked == true)
                    GenerateSubmissionsReport();

                ExportButton.IsEnabled = true;
                ExportPDFButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчета: {ex.Message}", "Ошибка");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentReportData == null || _currentReportData.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта");
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    FileName = $"{ReportTitle.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        var headers = new List<string>();
                        foreach (DataColumn column in _currentReportData.Columns)
                        {
                            headers.Add($"\"{column.ColumnName}\"");
                        }
                        writer.WriteLine(string.Join(",", headers));

                        foreach (DataRow row in _currentReportData.Rows)
                        {
                            var values = new List<string>();
                            foreach (var item in row.ItemArray)
                            {
                                string value = item?.ToString() ?? "";
                                value = value.Replace("\"", "\"\"");
                                values.Add($"\"{value}\"");
                            }
                            writer.WriteLine(string.Join(",", values));
                        }
                    }

                    MessageBox.Show($"Отчет успешно экспортирован в:\n{saveDialog.FileName}", "Экспорт завершен");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        private void ExportPDFButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Экспорт в PDF временно недоступен. Используйте экспорт в CSV.", "Информация");
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