using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace qq
{
    public partial class MainWindow : Window
    {

        public void qqMainWindow()
        {
            InitializeComponent();
           // AddItems();
            LoadCompanies();
        }

        public void AddItems()
        {
            Companies qq = new Companies();
            qq.industry = "test";
            qq.name = "test";
            BDconnection.DB.Companies.Add(qq);
            //BDconnection.
            BDconnection.DB.SaveChanges();

            
        }
        public MainWindow()
        {
            thisWindow = this;
            InitializeComponent();
        }
        private static Window thisWindow;
        public static void closeWindow() => thisWindow.Close();
        private void LoadCompanies()
        {
            //CompaniesGrid.ItemsSource = BDconnection.DB.Companies.ToList();
        }
    }
}