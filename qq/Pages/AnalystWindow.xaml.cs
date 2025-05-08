using System;
using System.Linq;
using System.Windows;
using qq;
using System.Collections.ObjectModel;

namespace qq.Pages
{
    public partial class AnalystWindow : Window
    {
        private Users _currentUser;
        private ObservableCollection<Incidents> _incidentsList;

        public AnalystWindow(Users currentUser)
        {
            InitializeComponent();
            LoadCompanyData();
            LoadCountryData();
            LoadIncidentTypes();
            LoadIncidents(); // Загрузка инцидентов в таблицу

            if (currentUser != null)
                _currentUser = currentUser;
            DataContext = _currentUser;
        }

        // Загрузка списка компаний в ComboBox
        private void LoadCompanyData()
        {
            var companies = BDconnection.DB.Companies.ToList();
            CompanyComboBox.ItemsSource = companies;
            CompanyComboBox.DisplayMemberPath = "name";
            CompanyComboBox.SelectedValuePath = "id";
        }

        // Загрузка типов инцидентов в ComboBox
        private void LoadIncidentTypes()
        {
            var types = BDconnection.DB.IncidentTypes.ToList();
            IncidentTypeComboBox.ItemsSource = types;
            IncidentTypeComboBox.DisplayMemberPath = "name";
            IncidentTypeComboBox.SelectedValuePath = "id";
        }

        // Загрузка списка стран в ComboBox
        private void LoadCountryData()
        {
            var countries = BDconnection.DB.Countries.ToList();
            CountryComboBox.ItemsSource = countries;
            CountryComboBox.DisplayMemberPath = "name";
            CountryComboBox.SelectedValuePath = "id";
        }

        // Загрузка всех инцидентов в таблицу
        private void LoadIncidents()
        {
            var incidents = BDconnection.DB.Incidents.ToList();
            _incidentsList = new ObservableCollection<Incidents>(incidents);
            IncidentsDataGrid.ItemsSource = _incidentsList;
        }

        private void AddIncidentButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCompany = CompanyComboBox.SelectedItem as Companies;
            var selectedCountry = CountryComboBox.SelectedItem as Countries;
            var selectedIncidentType = IncidentTypeComboBox.SelectedItem as IncidentTypes;

            if (selectedCompany == null || selectedCountry == null || selectedIncidentType == null)
            {
                MessageBox.Show("Выберите компанию, страну и тип инцидента.");
                return;
            }

            var newIncident = new Incidents
            {
                company_id = selectedCompany.id,
                start_time = StartTimePicker.SelectedDate,
                end_time = EndTimePicker.SelectedDate,
                description = DescriptionTextBox.Text,
                Companies = selectedCompany
            };

            BDconnection.DB.Incidents.Add(newIncident);
            BDconnection.DB.SaveChanges(); // сохраняем, чтобы получить id

            // прямой SQL-вставкой связываем тип инцидента
            string sql = "INSERT INTO IncidentToTypes (incident_id, type_id) VALUES (@p0, @p1)";
            BDconnection.DB.Database.ExecuteSqlCommand(sql, newIncident.id, selectedIncidentType.id);

            MessageBox.Show("Инцидент и его тип добавлены успешно!");

            // Обновляем таблицу с инцидентами
            _incidentsList.Add(newIncident);
        }
    }
}
