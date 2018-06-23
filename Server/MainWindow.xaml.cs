using System;
using System.Collections.Generic;
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
using System.ServiceModel;
using System.IO;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public class Service : IUdonContract
    {
        #region CustomSettings
        public void RefreshNotificationSettings(string login, string settings)
        {
            var sett = settings.Split('|');
            MainWindow.Users.RefreshNotifications(login, sett[0], sett[1], sett[2]);
        }
        public void RefreshThemeSettings(string login,string settings)
        {
            var sett = settings.Split('|');
            MainWindow.Users.RefreshTheme(login, sett[0], sett[1]);
        }
        public string GetThemeSettings(string login)
        {
            var user = MainWindow.Users.search(login);
            if (user != null)
            {
                return user.Theme + "|" + user.Accent;
            }
            else
            {
                return null;
            }
        }
        public string GetNotificationSettings(string login)
        {
            var user = MainWindow.Users.search(login);
            if (user != null)
                return user.Msg_Not + "|" + user.PMsg_Not + "|" + user.Conn_Not;
            else
                return null;
        }
        #endregion
        #region ScreenSharing
        public bool NeedScreenSharing(string login)
                {
                    return OnlineUsers.NeedScreenSharing(login);
                }
        public bool SendScreen(ScreenInfo screen)
                {
                    if (screen.To == "Server")
                        MainWindow.SetImageBox(screen.Image);
                    return OnlineUsers.NeedScreenSharing(screen.From);
                }
        #endregion
        #region PChat
        public bool NeedPChating(string login)
        {
            return OnlineUsers.IsPChating(login);
        }
        public TMsg SendPMessage(TMsg msg)
        {
            if (msg.type == "PCancel")
            {
                if (OnlineUsers.IsPChating(msg.from))
                {
                    PChatHelper.closeChat(msg.from);
                    MainWindowWorker.show_push(msg.from + " вышел из приватного чата.", "pmsg");
                }
            }
            if(msg.type == "PRequest")
            {
                MainWindowWorker.Request_PChat(msg.from);
            }
            else
            {
                PChatHelper.ReceivePMessage(msg);
            }
            return new TMsg("Server", msg.from, "Succes");
        }
        #endregion
        #region Auth
        public string Auth(string login, string password)
        {
            if (MainWindow.Users.IsInBase(login, password))
            {
                if (OnlineUsers.IsOnline(login))
                {
                    return "online";
                }
                MainWindowWorker.usr_online(login);
                MainWindowWorker.add_mes(login + " вошел в сеть.");
                MainWindowWorker.show_push(login + " вошел в сеть.", "connect");
                OnlineUsers.get_online(login, MainWindow.Users.search(login));
                OnlineUsers.NewMsg(new TMsg("ServerCommand", "All", "Online|" + login));
                return "true";
            }
            return "false";
        }
        public string Regist(string login, string email, string password)
        {
            MainWindow.Users.addUser(login, password, email);
            return "true";
        }
        #endregion
        #region MainMethods
        public TMsg SendMessage(TMsg msg)
        {
            if (msg.to != "Server")
            {
                OnlineUsers.NewMsg(msg);
                MainWindowWorker.show_push("Новое сообщение от " + msg.from, "msg");
                MainWindowWorker.add_mes(msg.from + " : " + msg.body);
            }
            else
            {
                if (msg.body == "Offline")
                {
                    OnlineUsers.Get_Offline(msg.from);
                }
                else if (msg.body == "GetAllOnline")
                {
                    string body = "";
                    foreach (var usr in OnlineUsers.Users.Values)
                    {
                        if (usr.Login != msg.from)
                            body += usr.Login + "|";
                    }
                    body.TrimEnd('|');
                    return new TMsg("Server", msg.from, body);
                }
            }
            return new TMsg("Server", msg.from, "Success");
        }
        public TMsg[] ReceiveUnread(string login)
        {
            OnlineUsers.Set_Online_State(login, true);
            return OnlineUsers.IsSomethingNew(login);
        }
        public bool CheckValue(string type, string body)
        {
            switch (type)
            {
                case "login":
                    if (MainWindow.Users.search(body) != null)
                        return false;
                    else
                        return true;
                case "email":
                    if (MainWindow.Users.search_email(body) != null)
                        return false;
                    else
                        return true;
            }
            return false;
        }
        public void RefreshValue(string type, string body, string value)
        {
            if (type == "password")
            {
                MainWindow.Users.RefreshPass(body, value);
            }
        }
        #endregion
    } 
    public static class OnlineUsers
    {
        public static Dictionary<string, User> Users { get; set; } = new Dictionary<string, User>();

        public static bool NeedScreenSharing(string login)
        {
            foreach(var usr in Users.Values)
            {
                if (usr.Login == login)
                    return usr.NeedScreenSharing;
            }
            return false;
        }
        public static void NeedPChating(string login, bool state)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                {
                    usr.NeedPChating = state;
                    break;
                }
            }
        }
        public static void ScreenShare(string login, bool state)
        {
            foreach(var usr in Users.Values)
            {
                if(usr.Login==login)
                {
                    usr.NeedScreenSharing = state;
                    break;
                }
            }
        }
        public static void NewMsg(TMsg msg)
        {
            if (msg.to == "All")
            {
                foreach (var usr in Users.Values)
                {
                    if (usr.Login == msg.from)
                        continue;
                    usr.Unreaded.Add(msg);
                }
            }
            else
            {
                foreach (var usr in Users.Values)
                {
                    if (usr.Login == msg.to)
                    {
                        usr.Unreaded.Add(msg);
                        break;
                    }
                }
            }
        }
        public static TMsg[] IsSomethingNew(string login)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                {
                    TMsg[] messages = usr.Unreaded.ToArray();
                    usr.Unreaded.Clear();
                    return messages;
                }

            }
            return null;
        }
        public static bool IsPChating(string login)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                {
                    if (usr.NeedPChating)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
        public static bool IsOnline(string login)
        {
            foreach(var usr in Users.Values)
            {
                if(usr.Login==login)
                {
                    return true;
                }
            }
            return false;
        }
        public static void get_online(string login, User usr)
        {
            Users.Add(login, usr);
        }
        public static void Get_Offline(string login)
        {
            MainWindowWorker.add_mes(login + " вышел из сети.");
            MainWindowWorker.usr_offline(login);
            get_offline(login);
            NewMsg(new TMsg("ServerCommand", "All", "Offline|" + login));
        }
        public static void Get_All_Offline()
        {
            foreach(var usr in Users.Values)
            {
                usr.IsOnline = false;
            }
        }
        public static void Set_Online_State(string login, bool state)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                    usr.IsOnline = state;
            }
        }
        public static void Kick_If_Offline()
        {
            List<string> off_users = new List<string>();
            foreach(var usr in Users.Values)
            {
                if (usr.IsOnline == false)
                    off_users.Add(usr.Login);
            }
            foreach(var usr in off_users)
            {
                OnlineUsers.Get_Offline(usr);
            }
        }
        public static void get_offline(string login)
        {
            Users.Remove(login);
        }
    }
    public static class PChatHelper
    {
        private static Dictionary<string, PChat> chats = new Dictionary<string, PChat>();
        public static void addChat(string login)
        {
            chats.Add(login, new PChat(login));
            OnlineUsers.NeedPChating(login, true);
            chats[login].Show();
        }
        public static void SendPMessage(string login, string body)
        {
            OnlineUsers.NewMsg(new TMsg("Server", login, body, "Private"));
        }
        public static void ReceivePMessage(TMsg pmsg)
        {
            chats[pmsg.from].addMess(pmsg.from + " : " + pmsg.body);
        }
        public static void closeChat(string login)
        {
            chats[login].Close();
        }
        public static void removeChat(string login)
        {
            chats.Remove(login);
            OnlineUsers.NeedPChating(login, false);
        }
    }
    public partial class MainWindow
    {
        private static ScreenSharing screen { get; set; }
        private Thread CheckingOfflineThread { get; set; }
        public static bool IsCheckingOnline { get; set; } = false;
        public static bool IsScreenReceiving { get; set; } = false;
        public static UsersBase Users = new UsersBase();
        public static bool IsNotificationDisplayed { get; set; }
        public static bool AllowMessageNotifications { get; set; }
        public static bool AllowPMessageNotifications { get; set; }
        public static bool AllowConnectNotifications { get; set; }
        public Push Push { get; set; }
        ServiceHost host = new ServiceHost(typeof(Service));
        public MainWindow()
        {
            InitializeComponent();
            host.Open();
            Chat_box.Items.Add("Сервер запущен.");
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartCheckingOnline();
            set_toogle();
        }
        private void Send_but_Click(object sender, RoutedEventArgs e)
        {
            if(Msg_box.Text!="")
            {
                OnlineUsers.NewMsg(new TMsg("Server", "All", Msg_box.Text));
                Chat_box.Items.Add("Server : " + Msg_box.Text);
                Msg_box.Clear();
            }
        }
        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
                Send_but_Click(sender, new RoutedEventArgs());
        }
        
        public void StartCheckingOnline()
        {
            if (!IsCheckingOnline)
            {
                IsCheckingOnline = true;
                CheckingOfflineThread = new Thread(new ThreadStart(CheckIfOnline));
                CheckingOfflineThread.IsBackground = true;
                CheckingOfflineThread.Start();
            }
        }
        private void CheckIfOnline()
        {
            while (IsCheckingOnline)
            {
                OnlineUsers.Get_All_Offline();
                Thread.Sleep(4000);
                OnlineUsers.Kick_If_Offline();
            }
        }
        public static void UpdateScrollBar(ListBox listBox)
        {
            if (listBox != null)
            {
                var border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }
        public static void SetImageBox(byte[] array)
        {
            if (IsScreenReceiving)
                screen.Image_box.Source = ConvertToBitmapImage(array);
        }
        public static BitmapImage ConvertToBitmapImage(byte[] array)
        {
            BitmapImage im = new BitmapImage();
            im.BeginInit();
            im.StreamSource = new MemoryStream(array);
            im.EndInit();
            return im;
        }
        private void ScreenShare_click(object sender, RoutedEventArgs e)
        {
            if(Users_box.SelectedItem!=null)
            {
                if (!IsScreenReceiving)
                {
                    string login = Users_box.SelectedItem.ToString();
                    OnlineUsers.ScreenShare(login, true);
                    IsScreenReceiving = true;
                    screen = new ScreenSharing();
                    screen.Owner = this;
                    screen.Login = login;
                    screen.Title = "Screen - " + login ;
                    screen.Show();
                }
                else
                {
                    this.ShowMessageAsync("Уведомление", "Передача экрана уже запущена!");
                }
            }
        }

        #region ThemeSettings
        public void change_theme(string theme)
        {
            Properties.Settings.Default.Theme = theme;
            Properties.Settings.Default.Save();
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(Properties.Settings.Default.Accent), ThemeManager.GetAppTheme(Properties.Settings.Default.Theme));
        }
        public void change_accent(string accent)
        {
            Properties.Settings.Default.Accent = accent;
            Properties.Settings.Default.Save();
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(accent), ThemeManager.GetAppTheme(Properties.Settings.Default.Theme));
        }
        public void Change_Accent(string accent)
        {
            switch (accent)
            {
                case "Красный":
                    change_accent("Red");
                    break;
                case "Зелёный":
                    change_accent("Green");
                    break;
                case "Синий":
                    change_accent("Blue");
                    break;
                case "Фиолетовый":
                    change_accent("Purple");
                    break;
                case "Оранжевый":
                    change_accent("Orange");
                    break;
                case "Лаймовый":
                    change_accent("Lime");
                    break;
                case "Изумрудный":
                    change_accent("Emerald");
                    break;
                case "Циан":
                    change_accent("Cyan");
                    break;
                case "Кобальт":
                    change_accent("Cobalt");
                    break;
                case "Индиго":
                    change_accent("Indigo");
                    break;
                case "Лиловый":
                    change_accent("Violet");
                    break;
                case "Розовый":
                    change_accent("Pink");
                    break;
                case "Пурпурный":
                    change_accent("Magenta");
                    break;
                case "Малиновый":
                    change_accent("Crimson");
                    break;
                case "Янтарный":
                    change_accent("Amber");
                    break;
                case "Жёлтый":
                    change_accent("Yellow");
                    break;
                case "Коричневый":
                    change_accent("Brown");
                    break;
                case "Оливковый":
                    change_accent("Olive");
                    break;
                case "Сталь":
                    change_accent("Steel");
                    break;
                case "Серый":
                    change_accent("Taupe");
                    break;
                case "Охра":
                    change_accent("Sienna");
                    break;
            }
        }
        public void Change_Theme(string theme)
        {
            switch (theme)
            {
                case "Тёмная":
                    change_theme("BaseDark");
                    break;
                case "Светлая":
                    change_theme("BaseLight");
                    break;
            }
        }
        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            string theme = (sender as MenuItem).Header as string;
            Change_Theme(theme);
        }
        private void Accent_Click(object sender, RoutedEventArgs e)
        {
            string accent = (sender as MenuItem).Header as string;
            Change_Accent(accent);
        }
        #endregion
        #region NotificationSettings
        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            string head = (string)(sender as MahApps.Metro.Controls.ToggleSwitch).Header;
            switch (head)
            {
                case "Обычные сообщения":
                    Properties.Settings.Default.AllowMessageNotifications = true;
                    AllowMessageNotifications = true;
                    break;
                case "Приватные сообщения":
                    Properties.Settings.Default.AllowPMessageNotifications = true;
                    AllowPMessageNotifications = true;
                    break;
                case "Подключения":
                    Properties.Settings.Default.AllowConnectNotifications = true;
                    AllowConnectNotifications = true;
                    break;
            }
            Properties.Settings.Default.Save();
        }
        public void set_toogle()
        {
            AllowConnectNotifications = Properties.Settings.Default.AllowConnectNotifications;
            AllowMessageNotifications = Properties.Settings.Default.AllowMessageNotifications;
            AllowPMessageNotifications = Properties.Settings.Default.AllowPMessageNotifications;
            if (AllowMessageNotifications)
                Msg_toggle.IsChecked = true;
            if (AllowPMessageNotifications)
                PMsg_toggle.IsChecked = true;
            if (AllowConnectNotifications)
                Connect_toggle.IsChecked = true;
        }
        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            string head = (string)(sender as MahApps.Metro.Controls.ToggleSwitch).Header;
            switch (head)
            {
                case "Обычные сообщения":
                    Properties.Settings.Default.AllowMessageNotifications = false;
                    AllowMessageNotifications = false;
                    break;
                case "Приватные сообщения":
                    Properties.Settings.Default.AllowPMessageNotifications = false;
                    AllowPMessageNotifications = false;
                    break;
                case "Подключения":
                    Properties.Settings.Default.AllowConnectNotifications = false;
                    AllowConnectNotifications = false;
                    break;

            }
            Properties.Settings.Default.Save();
        }
        public void show_push(string text, string type)
        {
            Dispatcher.BeginInvoke((Action)delegate () {
                if (type == "msg" && AllowMessageNotifications)
                {
                    push_(text);
                }
                else if (type == "connect" && AllowConnectNotifications)
                {
                    push_(text);
                }
                else if (type == "pmsg" && AllowPMessageNotifications)
                {
                    push_(text);
                }
            });
        }
        public void push_(string text)
        {
            if (!IsNotificationDisplayed)
            {
                _push(text);
            }
            else
            {
                Push.Close();
                _push(text);
            }
        }
        public void _push(string text)
        {
            Push = new Push();
            Push.Inf.Text = text;
            Push.Focus();
            Push.Show();
            IsNotificationDisplayed = true;
        }
        #endregion
        #region PChat
        public async void Request_Pchat(string login)
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Принять",
                NegativeButtonText = "Отклонить",
                ColorScheme = MetroDialogOptions.ColorScheme,
            };

            var answer = await this.ShowMessageAsync("Уведомление", login + " приглашает вас в приватный чат", MessageDialogStyle.AffirmativeAndNegative, settings);
            if(answer==MessageDialogResult.Affirmative)
            {
                StartPChat(login);
            }
            else
            {
                OnlineUsers.NewMsg(new TMsg("ServerCommand", login, "PRequestCancel", "Command"));
            }
        }
        private void PChat_click(object sender, RoutedEventArgs e)
        {
            if(Users_box.SelectedItem!=null)
            {
                StartPChat(Users_box.SelectedItem.ToString());
            }
        }
        public void StartPChat(string login)
        {
            if(OnlineUsers.IsPChating(login))
            {
                this.ShowMessageAsync("Уведомление", "Чат с данным пользователем уже открыт!");
                return;
            }
            PChatHelper.addChat(login);
        }
        #endregion
    }

    public static class MainWindowWorker
    {
        public static MainWindow wind = App.Current.MainWindow as MainWindow;
        public static void usr_online(string login)
        {
            wind.Dispatcher.BeginInvoke((Action)delegate ()
            {
                wind.Users_box.Items.Add(login);
            });
        }
        public static void usr_offline(string login)
        {
            wind.Dispatcher.BeginInvoke((Action)delegate ()
            {
                wind.Users_box.Items.Remove(login);
            });
        }
        public static void show_push(string mes, string type)
        {
            wind.Dispatcher.BeginInvoke((Action)delegate ()
            {
                wind.show_push(mes, type);
            });
        }
        public static void add_mes(string msg)
        {
            wind.Dispatcher.BeginInvoke((Action)delegate ()
            {
                wind.Chat_box.Items.Add(msg);
                MainWindow.UpdateScrollBar(wind.Chat_box);
            });
        }
        public static void Request_PChat(string login)
        {
            wind.Dispatcher.BeginInvoke((Action)delegate ()
            {
                wind.Request_Pchat(login);
            });
        }
    }

}
