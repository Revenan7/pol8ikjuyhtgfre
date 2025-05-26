using System;
using System.Linq;                         // ← для Contains
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using qq;

namespace qq.Pages
{
    public partial class AnalystWindow : Window
    {
        private readonly Users _currentUser;
        private ObservableCollection<Incidents> _incidentsList;

        // список разрешённых колонок
        private static readonly string[] _allowedColumns =
        {
            "id",
            "company_id",
            "start_time",
            "end_time",
            "description",
            "Companies"
        };

        public AnalystWindow(Users currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;
            DataContext = _currentUser;

            LoadCompanyData();
            LoadCountryData();
            LoadIncidentTypes();
            LoadIncidents();
        }

        #region Справочники
        private void LoadCompanyData()
        {
            CompanyComboBox.ItemsSource = BDconnection.DB.Companies.ToList();
            CompanyComboBox.DisplayMemberPath = "name";
            CompanyComboBox.SelectedValuePath = "id";
        }

        private void LoadIncidentTypes()
        {
            IncidentTypeComboBox.ItemsSource = BDconnection.DB.IncidentTypes.ToList();
            IncidentTypeComboBox.DisplayMemberPath = "name";
            IncidentTypeComboBox.SelectedValuePath = "id";
        }

        private void LoadCountryData()
        {
            CountryComboBox.ItemsSource = BDconnection.DB.Countries.ToList();
            CountryComboBox.DisplayMemberPath = "name";
            CountryComboBox.SelectedValuePath = "id";
        }
        #endregion

        #region Таблица инцидентов
        private void LoadIncidents()
        {
            _incidentsList = new ObservableCollection<Incidents>(
                                BDconnection.DB.Incidents.ToList());
            IncidentsDataGrid.ItemsSource = _incidentsList;
        }

        // фильтрация авто-создаваемых колонок
        private void IncidentsDataGrid_AutoGeneratingColumn(object sender,
                                      DataGridAutoGeneratingColumnEventArgs e)
        {
            if (!_allowedColumns.Contains(e.PropertyName))
            {
                e.Cancel = true;     // скрываем лишнюю колонку
                return;
            }

            // при желании подправим шапки
            switch (e.PropertyName)
            {
                case "company_id": e.Column.Header = "CompanyId"; break;
                case "start_time": e.Column.Header = "Start"; break;
                case "end_time": e.Column.Header = "End"; break;
                case "Companies": e.Column.Header = "Company"; break;
            }
        }

        private void IncidentsDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new Incidents { start_time = DateTimeOffset.Now };
        }

        private void IncidentsDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var inc = e.Row.Item as Incidents;
                if (inc == null) return;

                try
                {
                    if (inc.id == 0)
                        BDconnection.DB.Incidents.Add(inc);

                    BDconnection.DB.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        private void IncidentsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;

            var inc = IncidentsDataGrid.SelectedItem as Incidents;
            if (inc == null) return;

            var res = MessageBox.Show($"Удалить инцидент \"{inc.description}\"?",
                                      "Подтверждение",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes)
            {
                e.Handled = true;
                return;
            }

            try
            {
                BDconnection.DB.Incidents.Remove(inc);
                BDconnection.DB.SaveChanges();
                _incidentsList.Remove(inc);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Кнопка «Добавить инцидент»
        private void AddIncidentButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCompany = CompanyComboBox.SelectedItem as Companies;
            var selectedCountry = CountryComboBox.SelectedItem as Countries;
            var selectedType = IncidentTypeComboBox.SelectedItem as IncidentTypes;

            if (selectedCompany == null || selectedCountry == null || selectedType == null)
            {
                MessageBox.Show("Выберите компанию, страну и тип инцидента.");
                return;
            }

            if (!StartTimePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Заполните дату начала.");
                return;
            }

            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var startDao = new DateTimeOffset(StartTimePicker.SelectedDate.Value, offset);

            DateTimeOffset? endDao = null;
            if (EndTimePicker.SelectedDate.HasValue)
                endDao = new DateTimeOffset(EndTimePicker.SelectedDate.Value, offset);

            var newIncident = new Incidents
            {
                company_id = selectedCompany.id,
                start_time = startDao,
                end_time = endDao,
                description = DescriptionTextBox.Text,
                Companies = selectedCompany
            };

            try
            {
                BDconnection.DB.Incidents.Add(newIncident);
                BDconnection.DB.SaveChanges();   // id получен

                var sql = "INSERT INTO IncidentToTypes (incident_id, type_id) VALUES (@p0, @p1)";
                BDconnection.DB.Database
                              .ExecuteSqlCommand(sql,
                                                 new object[] { newIncident.id, selectedType.id });

                _incidentsList.Add(newIncident);
                MessageBox.Show("Инцидент добавлен успешно!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
