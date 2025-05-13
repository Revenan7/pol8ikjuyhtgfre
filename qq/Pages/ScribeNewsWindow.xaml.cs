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
    public partial class ScribeNewsWindow : Window
    {
        private ObservableCollection<Incidents> _incidentsList;
        private Incidents _selectedIncident;

        public ScribeNewsWindow()
        {
            InitializeComponent();
            LoadIncidents();
            LoadNewsGrouped();
        }

        private void LoadIncidents()
        {
            var incidents = BDconnection.DB.Incidents.ToList();
            _incidentsList = new ObservableCollection<Incidents>(incidents);
            IncidentsDataGrid.ItemsSource = _incidentsList;
        }



        private void LoadNewsGrouped()
        {
            var allNews = BDconnection.DB.News.ToList();

            var groupedNews = (CollectionViewSource)FindResource("GroupedNews");
            groupedNews.Source = allNews;

            if (groupedNews.View != null)
            {
                groupedNews.GroupDescriptions.Clear();
                groupedNews.GroupDescriptions.Add(new PropertyGroupDescription("incident_id"));
            }
        }


        private void IncidentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedIncident = IncidentsDataGrid.SelectedItem as Incidents;
        }

        private void AddNewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIncident == null)
            {
                MessageBox.Show("Выберите инцидент из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string description = DescriptionTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Описание не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            MessageBox.Show("Новость успешно добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DescriptionTextBox.Clear();

            LoadNewsGrouped(); // обновляем новости
        }
    }

    public partial class IncidentIdToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int incidentId)
            {
                var incident = BDconnection.DB.Incidents.FirstOrDefault(i => i.id == incidentId);
                return incident != null ? $"Инцидент: {incident.description}" : $"Инцидент ID: {incidentId}";
            }

            return "Неизвестный инцидент";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
