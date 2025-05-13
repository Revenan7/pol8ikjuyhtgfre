using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace qq.Pages
{
    /// <summary>
    /// Interaction logic for SreMetricsWindow.xaml
    /// </summary>
    public partial class SreMetricsWindow : Window
    {
        private ObservableCollection<SreMetricRecord> _metrics;

        public SreMetricsWindow()
        {
            InitializeComponent();
            LoadSreMetrics();
        }

        private void LoadSreMetrics()
        {
            // тут можно заменить на реальные данные из БД или API
            _metrics = new ObservableCollection<SreMetricRecord>
            {
                new SreMetricRecord { Timestamp = DateTime.Now.AddMinutes(-30), ErrorRate = 1.2, ResponseTime = 350, Availability = 99.5, StockPrice = 112.45 },
                new SreMetricRecord { Timestamp = DateTime.Now.AddMinutes(-20), ErrorRate = 0.8, ResponseTime = 310, Availability = 99.7, StockPrice = 113.10 },
                new SreMetricRecord { Timestamp = DateTime.Now.AddMinutes(-10), ErrorRate = 2.5, ResponseTime = 600, Availability = 98.9, StockPrice = 110.80 },
                new SreMetricRecord { Timestamp = DateTime.Now, ErrorRate = 0.5, ResponseTime = 280, Availability = 99.9, StockPrice = 114.20 }
            };

            MetricsDataGrid.ItemsSource = _metrics; 
        }
    }

    public class SreMetricRecord
    {
        public DateTime Timestamp { get; set; }
        public double ErrorRate { get; set; } // в процентах
        public double ResponseTime { get; set; } // в миллисекундах
        public double Availability { get; set; } // в процентах
        public double StockPrice { get; set; } // цена акции
    }
}
