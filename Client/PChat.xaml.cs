using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для PChat.xaml
    /// </summary>
    public partial class PChat
    {
        public bool NeedClose { get; set;  } = true;
        public string Login { get; set; }
        public PChat()
        {
            InitializeComponent();
        }
        public void addMess(string mes)
        {
            List_mess.Items.Add(mes);
            MainWindow.UpdateScrollBar(List_mess);
        }
        private void Send_but_Click(object sender, RoutedEventArgs e)
        {
            string Text = Box_mess.Text;
            if (Text != "")
            {
                MainWindow.client.SendPMessage(new TMsg(Login, "Server", Text, "Private"));
                addMess(Login+" : " + Text);
                Box_mess.Clear();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (NeedClose)
                    MainWindow.client.SendPMessage(new TMsg(Login, "Server", "", "PCancel"));
            }
            catch
            {
             
            }
            MainWindow.IsPChating = false;
            MainWindow.StopChating = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_but_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
