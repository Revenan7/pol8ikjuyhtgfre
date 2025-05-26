using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace qq.Pages
{
    /// <summary>Окно для отображения и управления новостями, связанными с инцидентами</summary>
    public partial class ScribeNewsWindow : Window
    {
        private ObservableCollection<Incidents> _incidentsList;
        private Incidents _selectedIncident;

        private ObservableCollection<News> _incidentNews;   // новости выбранного инцидента

        public ScribeNewsWindow()
        {
            InitializeComponent();
            LoadIncidents();
            LoadNewsGrouped();
        }

        #region Загрузка данных
        private void LoadIncidents()
        {
            _incidentsList = new ObservableCollection<Incidents>(BDconnection.DB.Incidents.ToList());
            IncidentsDataGrid.ItemsSource = _incidentsList;
        }

        private void LoadNewsGrouped()
        {
            var groupedNews = (CollectionViewSource)FindResource("GroupedNews");
            groupedNews.Source = BDconnection.DB.News.ToList();

            if (groupedNews.View != null)
            {
                groupedNews.GroupDescriptions.Clear();
                groupedNews.GroupDescriptions.Add(new PropertyGroupDescription(nameof(News.incident_id)));
            }
        }

        private void LoadIncidentNews()
        {
            if (_selectedIncident == null)
            {
                NewsDataGrid.ItemsSource = null;
                return;
            }

            _incidentNews = new ObservableCollection<News>(
                BDconnection.DB.News
                               .Where(n => n.incident_id == _selectedIncident.id)
                               .OrderByDescending(n => n.published_at)
                               .ToList());

            NewsDataGrid.ItemsSource = _incidentNews;
        }
        #endregion

        #region Обработчики UI
        private void IncidentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedIncident = IncidentsDataGrid.SelectedItem as Incidents;
            LoadIncidentNews();
        }

        /// <summary>Быстрое добавление новости к выбранному инциденту</summary>
        private void AddNewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIncident == null)
            {
                MessageBox.Show("Выберите инцидент из списка.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string description = DescriptionTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Описание не может быть пустым.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var news = new News
            {
                incident_id = _selectedIncident.id,
                published_at = DateTimeOffset.Now,
                title = description,
                source = "Внутренний отчёт",
                popularity = 0,
                sentiment_score = 0
            };

            BDconnection.DB.News.Add(news);
            BDconnection.DB.SaveChanges();

            _incidentNews.Insert(0, news);          // сразу отобразить
            LoadNewsGrouped();                      // обновить общий список
            DescriptionTextBox.Clear();
        }
        #endregion

        #region CRUD-события DataGrid
        /// <summary>Создание объекта при добавлении новой строки</summary>
        private void NewsDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            if (_selectedIncident == null)
            {
                MessageBox.Show("Сначала выберите инцидент.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            e.NewItem = new News
            {
                incident_id = _selectedIncident.id,
                published_at = DateTimeOffset.Now,
                source = "—",
                popularity = 0,
                sentiment_score = 0
            };
        }

        /// <summary>Сохранить изменения после редактирования или добавить новую запись</summary>
        private void NewsDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // если пользователь отменил редактирование – ничего не делаем
            if (e.EditAction != DataGridEditAction.Commit) return;

            // даём DataGrid закончить commit, а потом сохраняем в БД
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var edited = e.Row.Item as News;
                if (edited == null) return;

                try
                {
                    // новая строка? – добавляем в контекст
                    if (edited.id == 0)
                        BDconnection.DB.News.Add(edited);

                    BDconnection.DB.SaveChanges();
                    LoadNewsGrouped();   // рефреш нижнего списка
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        /// <summary>Удаление через клавишу Delete или кнопку Delete на DataGrid</summary>
        private void NewsDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Delete) return;

            var selected = NewsDataGrid.SelectedItem as News;
            if (selected == null) return;

            var res = MessageBox.Show($"Удалить новость «{selected.title}»?", "Подтверждение",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes)
            {
                e.Handled = true;
                return;
            }

            // убрать из контекста и коллекции
            BDconnection.DB.News.Remove(selected);
            BDconnection.DB.SaveChanges();
            _incidentNews.Remove(selected);
            LoadNewsGrouped();
        }
        #endregion
    }

    /// <summary>Конвертер ID инцидента в его описание (для ListView GroupHeader или иных мест)</summary>
    public class IncidentIdToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int incidentId)
            {
                var incident = BDconnection.DB.Incidents.FirstOrDefault(i => i.id == incidentId);
                return incident != null
                    ? $"Инцидент: {incident.description}"
                    : $"Инцидент ID: {incidentId}";
            }
            return "Неизвестный инцидент";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
