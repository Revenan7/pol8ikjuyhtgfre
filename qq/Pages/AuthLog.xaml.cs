using qq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

namespace qq.Pages
{
    /// <summary>
    /// Логика взаимодействия для AuthLog.xaml
    /// </summary>
    public partial class AuthLog : Page
    {
        public AuthLog()
        {
            InitializeComponent();
        }
        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password)).Select(x => x.ToString("X2")));
            }
        }
        public void AddItems()
        {
            Companies qq = new Companies();
            qq.industry = "test";
            qq.name = "test";
            BDconnection.DB.Companies.Add(qq);
            BDconnection.DB.SaveChanges();


        }

        private void signInButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(loginTextBox.Text) || string.IsNullOrEmpty(passwordText.Password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            string _password = GetHash(passwordText.Password);


            var user = BDconnection.DB.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.login == loginTextBox.Text && u.password == _password);

            if (user == null) { MessageBox.Show("Пользователь с такими данными не найден!"); return; }

            else
            {
                //var newWindow = new UserWindow(user);
                var newWindow = new AnalystWindow(user);
                newWindow.Show();
                MainWindow.closeWindow();
            }
           
            
        }

        private void register_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate((new Uri("/Pages/Registration.xaml", UriKind.Relative)));
    }
}
