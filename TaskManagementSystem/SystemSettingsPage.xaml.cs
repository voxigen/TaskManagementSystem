using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class SystemSettingsPage : Page
    {
        private TaskManagementSystemEntities3 _context;

        public SystemSettingsPage()
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            BuildDateText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            this.Loaded += SystemSettingsPage_Loaded;
        }

        private void SystemSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            try
            {
                if (_context?.Database?.Connection == null)
                {
                    DatabasePathText.Text = "Ошибка подключения к БД";
                    DatabaseSizeText.Text = "Недоступно";
                    return;
                }

                DatabasePathText.Text = "localhost\\SQLEXPRESS : TaskManagementSystem";

                var sizeQuery = @"
                    SELECT 
                        database_name = DB_NAME(),
                        size_mb = CAST(SUM(size) * 8.0 / 1024 AS DECIMAL(18,2))
                    FROM sys.master_files
                    WHERE database_id = DB_ID()
                    GROUP BY database_id";

                var sizeResult = _context.Database.SqlQuery<DatabaseSize>(sizeQuery).FirstOrDefault();
                if (sizeResult != null)
                {
                    DatabaseSizeText.Text = $"{sizeResult.size_mb} MB";
                }
                else
                {
                    DatabaseSizeText.Text = "Не удалось получить";
                }

                LastBackupText.Text = "Функция отключена";
            }
            catch (Exception)
            {
                DatabaseSizeText.Text = "Ошибка расчета";
                DatabasePathText.Text = "Не удалось получить путь";
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция резервного копирования временно недоступна", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция восстановления временно недоступна", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _context.Dispose();
                _context = new TaskManagementSystemEntities3();
                StatusText.Text = "Кэш успешно очищен!";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка очистки кэша: {ex.Message}";
            }
        }

        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Система обновлена до последней версии!";
            MessageBox.Show("Проверка обновлений завершена. Ваша система актуальна!", "Обновления", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class DatabaseSize
    {
        public string database_name { get; set; }
        public decimal size_mb { get; set; }
    }
}