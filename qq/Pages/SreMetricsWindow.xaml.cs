using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace qq.Pages
{
    public partial class SreMetricsWindow : Window
    {
        private ObservableCollection<SreMetricView> _metrics;

        public SreMetricsWindow()
        {
            InitializeComponent();
            InitFilters();
            LoadMetrics();
        }

        #region Справочники
        private void InitFilters()
        {
            CompanyFilterCombo.ItemsSource = BDconnection.DB.Companies.ToList();
            CompanyFilterCombo.DisplayMemberPath = "name";
            CompanyFilterCombo.SelectedValuePath = "id";
            CompanyFilterCombo.SelectedIndex = 0;
        }
        #endregion

        #region Загрузка данных
        private void LoadMetrics()
        {
            int? companyId = CompanyFilterCombo.SelectedValue as int?;
            DateTime? fromDate = DateFromPicker.SelectedDate;
            DateTime? toDate = DateToPicker.SelectedDate;

            var query = from m in BDconnection.DB.SREMetrics
                        join a in BDconnection.DB.AvailabilityMetrics on m.id equals a.metric_id
                        join r in BDconnection.DB.ResponseTimeMetrics on m.id equals r.metric_id
                        join e in BDconnection.DB.ErrorRateMetrics on m.id equals e.metric_id
                        join s in BDconnection.DB.StocksData on m.id equals s.id into sd
                        from s in sd.DefaultIfEmpty()
                        select new SreMetricView
                        {
                            Id = m.id,
                            CompanyId = m.company_id,
                            Timestamp = m.timestamp.Value,              // DateTimeOffset
                            Availability = (double)a.availability,
                            ResponseTime = (double)r.avg_response_time,
                            ErrorRate = (double)e.error_rate,
                            StockPrice = s != null ? (double?)s.price : null
                        };

            if (companyId.HasValue)
                query = query.Where(q => q.CompanyId == companyId.Value);

            if (fromDate.HasValue)
            {
                var fromOffset = new DateTimeOffset(fromDate.Value,
                                 TimeZoneInfo.Local.GetUtcOffset(fromDate.Value));
                query = query.Where(q => q.Timestamp >= fromOffset);
            }

            if (toDate.HasValue)
            {
                var toOffset = new DateTimeOffset(toDate.Value,
                               TimeZoneInfo.Local.GetUtcOffset(toDate.Value));
                query = query.Where(q => q.Timestamp <= toOffset);
            }

            _metrics = new ObservableCollection<SreMetricView>(
                           query.OrderByDescending(q => q.Timestamp));
            MetricsDataGrid.ItemsSource = _metrics;
        }
        #endregion

        #region CRUD через DataGrid
        private void MetricsDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new SreMetricView
            {
                Timestamp = DateTimeOffset.Now
            };
        }

        private void MetricsDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var vm = e.Row.Item as SreMetricView;
                if (vm == null) return;

                try
                {
                    if (vm.Id == 0)                // ---------- CREATE ----------
                    {
                        var m = new SREMetrics
                        {
                            company_id = vm.CompanyId ??
                                         (int)CompanyFilterCombo.SelectedValue,
                            timestamp = vm.Timestamp
                        };
                        BDconnection.DB.SREMetrics.Add(m);
                        BDconnection.DB.SaveChanges();   // m.id

                        BDconnection.DB.AvailabilityMetrics.Add(new AvailabilityMetrics
                        {
                            metric_id = m.id,
                            availability = (decimal)vm.Availability
                        });
                        BDconnection.DB.ResponseTimeMetrics.Add(new ResponseTimeMetrics
                        {
                            metric_id = m.id,
                            avg_response_time = (decimal)vm.ResponseTime
                        });
                        BDconnection.DB.ErrorRateMetrics.Add(new ErrorRateMetrics
                        {
                            metric_id = m.id,
                            error_rate = (decimal)vm.ErrorRate
                        });

                        if (vm.StockPrice.HasValue)
                        {
                            BDconnection.DB.StocksData.Add(new StocksData
                            {
                                company_id = m.company_id,
                                timestamp = m.timestamp,
                                price = (decimal)vm.StockPrice.Value,
                                volume = 0
                            });
                        }

                        BDconnection.DB.SaveChanges();

                        vm.Id = m.id;
                        vm.CompanyId = m.company_id;
                    }
                    else                             // ---------- UPDATE ----------
                    {
                        var m = BDconnection.DB.SREMetrics.Find(vm.Id);
                        if (m == null) return;

                        m.timestamp = vm.Timestamp;

                        var a = BDconnection.DB.AvailabilityMetrics.Find(vm.Id);
                        var r = BDconnection.DB.ResponseTimeMetrics.Find(vm.Id);
                        var eMetric = BDconnection.DB.ErrorRateMetrics.Find(vm.Id);

                        if (a != null) a.availability = (decimal)vm.Availability;
                        if (r != null) r.avg_response_time = (decimal)vm.ResponseTime;
                        if (eMetric != null) eMetric.error_rate = (decimal)vm.ErrorRate;

                        var stock = BDconnection.DB.StocksData
                                           .FirstOrDefault(s => s.company_id == m.company_id &&
                                                                s.timestamp == m.timestamp);
                        if (vm.StockPrice.HasValue)
                        {
                            if (stock == null)
                            {
                                BDconnection.DB.StocksData.Add(new StocksData
                                {
                                    company_id = m.company_id,
                                    timestamp = m.timestamp,
                                    price = (decimal)vm.StockPrice.Value,
                                    volume = 0
                                });
                            }
                            else stock.price = (decimal)vm.StockPrice.Value;
                        }
                        else if (stock != null)
                        {
                            BDconnection.DB.StocksData.Remove(stock);
                        }

                        BDconnection.DB.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Сохранить не удалось: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        private void MetricsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;

            var vm = MetricsDataGrid.SelectedItem as SreMetricView;
            if (vm == null) return;

            var res = MessageBox.Show($"Удалить запись от {vm.Timestamp:g}?",
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
                var m = BDconnection.DB.SREMetrics.Find(vm.Id);
                if (m != null)
                {
                    var a = BDconnection.DB.AvailabilityMetrics.Find(vm.Id);
                    var r = BDconnection.DB.ResponseTimeMetrics.Find(vm.Id);
                    var eMetric = BDconnection.DB.ErrorRateMetrics.Find(vm.Id);

                    if (a != null) BDconnection.DB.AvailabilityMetrics.Remove(a);
                    if (r != null) BDconnection.DB.ResponseTimeMetrics.Remove(r);
                    if (eMetric != null) BDconnection.DB.ErrorRateMetrics.Remove(eMetric);

                    var stock = BDconnection.DB.StocksData
                                       .FirstOrDefault(s => s.company_id == m.company_id &&
                                                            s.timestamp == m.timestamp);
                    if (stock != null) BDconnection.DB.StocksData.Remove(stock);

                    BDconnection.DB.SREMetrics.Remove(m);
                    BDconnection.DB.SaveChanges();
                }

                _metrics.Remove(vm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region UI-кнопки
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadMetrics();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        #endregion
    }

    /// <summary>
    /// Агрегированная строка для DataGrid
    /// </summary>
    public class SreMetricView
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public DateTimeOffset Timestamp { get; set; }   // ← DateTimeOffset
        public double ErrorRate { get; set; }
        public double ResponseTime { get; set; }
        public double Availability { get; set; }
        public double? StockPrice { get; set; }
    }
}
