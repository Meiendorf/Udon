using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для Push.xaml
    /// </summary>
    public partial class Push
    {
        public Push()
        {
            InitializeComponent();
            this.AllowsTransparency = true;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            double workHeight = SystemParameters.WorkArea.Height;
            double workWidth = SystemParameters.WorkArea.Width;
            this.Top = workHeight - this.Height;
            this.Left = workWidth - this.Width;
            Thread th = new Thread(new ParameterizedThreadStart(waite));
            th.Start(5);
        }

        public void waite(object _sec)
        {
            int sec = (int)_sec;
            Thread.Sleep(sec * 1000);
            double d = 1;
            for (int i = 0; i < 100; i++)
            {
                d -= 0.01;
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    this.Opacity = d;
                });
                Thread.Sleep(10);
            }
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                this.Close();
                MainWindow.IsNotificationDisplayed = false;
            });
        }
        private void focus_main()
        {
            App.Current.MainWindow.WindowState = WindowState.Normal;
            App.Current.MainWindow.Activate();
            MainWindow.IsNotificationDisplayed = false;
            this.Close();
        }
        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            focus_main();
        }
    }
}
