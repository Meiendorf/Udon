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
using MahApps.Metro.Controls.Dialogs;
using System.Threading;
using System.Drawing;
using System.IO;
using MahApps.Metro;

namespace Client
{
    public partial class MainWindow
    {
        #region Fields
        public static IUdonContract client;
        public PChat Chat { get; set; }
        public Push push { get; set; }
        public string Login { get; set; }
        public int RefreshSpeed { get; set; } = 250;
        private Thread ReceivingMessagesThread { get; set; }
        private Thread NeedPChatThread { get; set; }
        private Thread NeedScreenShareThread { get; set; }
        private bool IsScreenSharing { get; set; }
        public static bool IsPChating { get; set; }
        public static bool StopChating { get; set; } = false;
        private bool NeedCheckScreenShare { get; set; }
        private bool NeedCheckPChating { get; set; }
        private bool IsReceivingMessages { get; set; } = false;
        public string Accent { get; set; } = "Blue";
        public string Theme { get; set; } = "BaseDark";
        public bool AllowMessageNotifications { get; private set; }
        public bool AllowPMessageNotifications { get; private set; }
        public bool AllowConnectNotifications { get; private set; }
        public static bool IsNotificationDisplayed { get; set; }
        public bool IsLoged;
        #endregion
        #region Initializing
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new ChannelFactory<IUdonContract>("Udon").CreateChannel();
            }
            catch
            {
                this.ShowMessageAsync("Ошибка", "Невозможно подключиться к серверу! Перезапустите приложение!");
            }
            SmtpLog.client_create("natsuki-bot@rambler.ru", "kiritorito1110111");
            Authorizathion();
            StartReceivingMessages();
            StartCheckingPChating();
            StartScreenShareChecking();
            GetOnline();
        }
        #endregion
        #region Messaging
        public void StartScreenShareChecking()
        {
            if (!NeedCheckScreenShare)
            {
                NeedScreenShareThread = new Thread(new ThreadStart(CheckScreenSharing));
                NeedScreenShareThread.IsBackground = true;
                NeedCheckScreenShare = true;
                NeedScreenShareThread.Start();
            }
        }
        public void StopScreenShareChecking()
        {
            NeedCheckScreenShare = false;
        }
        public void StartReceivingMessages()
        {
            if (!IsReceivingMessages)
            {
                ReceivingMessagesThread = new Thread(new ThreadStart(CheckNewMessages));
                ReceivingMessagesThread.IsBackground = true;
                ReceivingMessagesThread.Start();
                IsReceivingMessages = true;
            }
        }
        public void StopReceivingMesssages()
        {
            if(IsReceivingMessages)
            {
                ReceivingMessagesThread.Abort();
                IsReceivingMessages = false;
            }
        }
        public void CheckScreenSharing()
        {
            while(NeedCheckScreenShare)
            {
                try
                {
                    var need = client.NeedScreenSharing(Login);
                    bool SharingActive = true;
                    if (need)
                    {
                        while (SharingActive)
                        {
                            SharingActive = client.SendScreen(new ScreenInfo(Login, "Server", GetScreenshot()));
                            Thread.Sleep(100);
                        }
                    }
                    Thread.Sleep(RefreshSpeed);
                }
                catch
                {

                }
            }
        }
        public void StartCheckingPChating()
        {
            NeedPChatThread = new Thread(new ThreadStart(CheckPChating));
            NeedPChatThread.IsBackground = true;
            NeedCheckPChating = true;
            NeedPChatThread.Start();
        }
        public void CheckPChating()
        {
            while(NeedCheckPChating)
            {
                try
                {
                    var need = client.NeedPChating(Login);
                    if (need)
                    {
                        Dispatcher.BeginInvoke((Action)delegate ()
                        {
                            IsPChating = true;
                            Chat = new PChat();
                            Chat.Login = Login;
                            Chat.Show();
                        });
                        bool StopPChating = true;
                        while (StopPChating)
                        {
                            StopPChating = client.NeedPChating(Login);
                            if (!StopPChating)
                            {
                                Dispatcher.BeginInvoke((Action)delegate ()
                                {
                                    Chat.NeedClose = false;
                                    Chat.Close();
                                });
                            }
                            if (StopChating)
                            {
                                StopChating = false;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    try
                    {
                        client.SendMessage(new TMsg(Login, "Server", "Offline"));
                    }
                    catch
                    {
                        Dispatcher.Invoke(delegate ()
                        {
                            App.Current.Shutdown();
                        });
                    }
                }
            }
        }
        private void CheckNewMessages()
        {
            try
            {
                while (true)
                {
                    TMsg[] messages = client.ReceiveUnread(Login);
                    if (messages != null)
                    {

                        foreach (var msg in messages)
                        {
                            if (msg.from == "ServerCommand")
                            {
                                ServerCommandsCenter(msg.body);
                                continue;
                            }
                            if (msg.type == "Private")
                            {
                                Dispatcher.BeginInvoke((Action)delegate ()
                                {
                                    Show_Push("Новое приватное сообщение!", "pmsg");
                                    Chat.addMess(msg.from + " : " + msg.body);
                                });
                                continue;
                            }
                            Show_Push("Новое сообщение от " + msg.body, "msg");
                            addMes(msg.from + " : " + msg.body);
                        }
                    }
                    System.Threading.Thread.Sleep(RefreshSpeed);
                }
            }
            catch(EndpointNotFoundException e)
            {
                Dispatcher.Invoke(delegate ()
                {
                    App.Current.Shutdown();
                });
            }
        }
        public void Request_PChat()
        {
            if(!IsPChating)
            {
                client.SendPMessage(new TMsg(Login, "Server", "PRequest", "Private"));
                Show_Push("Запрос отправлен!", "msg");
            }
            else
            {
                this.ShowMessageAsync("Уведомление", "Вы уже находитесь в приватном чате.");
            }
        }
        public void ServerCommandsCenter(string command)
        {
            string[] commands = command.Split('|');
            switch (commands[0])
            {
                case "Online":
                    OnlineManipulator(commands[1]);
                    Show_Push(commands[1] + " вошел в сеть.", "connect");
                    addMes(commands[1] + " вошел в сеть.");
                    break;
                case "Offline":
                    OnlineManipulator(commands[1], false);
                    addMes(commands[1] + " вышел из сети.");
                    break;
                case "PRequestCancel":
                    Show_Push("Сервер отклонил запрос на приватный чат.", "msg");
                    break;
            }
        }
        #endregion
        #region Online
        public void GetOnline()
        {
            var online = client.SendMessage(new TMsg(Login, "Server", "GetAllOnline"));
            string[] users = online.body.Split('|');
            foreach(var usr in users)
            {
                if(usr.Trim()!=""&&usr!=null)
                OnlineManipulator(usr);
            }
        }
        public void OnlineManipulator(string login, bool online = true)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (online)
                    Users_box.Items.Add(login);
                else
                    Users_box.Items.Remove(login);
            });
        }
        #endregion
        #region Auth
        private async Task ShowLoginDialog()
        {
            string login;
            string pass;
            while (!IsLoged)
            {
                LoginDialogData result = await this.ShowLoginAsync("Аутентификация", "Введите ваши данные. Нажмите \"Esc\", чтобы вернуться назад.", new LoginDialogSettings { NegativeButtonText = "Отмена", ColorScheme = this.MetroDialogOptions.ColorScheme, EnablePasswordPreview = true });
                if (result == null)
                {
                    break;
                }
                else
                {
                    login = result.Username;
                    pass = result.Password;
                    if (login.Trim(' ') == "" || pass.Trim(' ') == "")
                    {
                        await this.ShowMessageAsync("Ошибка!", "Заполните все поля!");
                        continue;
                    }
                    string resp = client.Auth(login, pass);
                    if (resp == "true")
                    {
                        await this.ShowMessageAsync("Уведомление", "Успех!");
                        Login = login;
                        GetThemeSettings();
                        GetNotificationSettings();
                        IsLoged = true;
                    }
                    else if (resp == "online")
                    {
                        await this.ShowMessageAsync("Уведомление", "Пользователь с такими данными уже в сети!");
                        continue;
                    }
                    else
                    {
                        await this.ShowMessageAsync("Уведомление", "Введён неправильный логин или пароль!");
                    }
                }
            }
        }
        private async void Authorizathion()
        {
            while (!IsLoged)
            {
                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "Войти",
                    NegativeButtonText = "Регистрация",
                    FirstAuxiliaryButtonText = "Забыли пароль?",
                    SecondAuxiliaryButtonText = "Выход",
                    ColorScheme = MetroDialogOptions.ColorScheme
                };
                var res = await this.ShowMessageAsync("Уведомление", "Для продолжения необходимо авторизироваться.", MessageDialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    await ShowLoginDialog();
                }
                else if (res == MessageDialogResult.Negative)
                {
                    await ShowRegistDialog();
                    //await this.ShowMessageAsync("Успех", "Вы нажали кнопку регистрации");
                }
                else if (res == MessageDialogResult.FirstAuxiliary)
                {
                    //await this.ShowMessageAsync("Успех", "Вы нажали кнопку пароля");
                    await ShowLostDialog();
                }
                else if (res == MessageDialogResult.SecondAuxiliary)
                {
                    await this.ShowMessageAsync("Уведомление", "Завершение работы приложения!");
                    App.Current.Shutdown();
                }
            }

        }
        private async Task ShowRegistDialog()
        {
            bool registred = false;
            while (!registred)
            {
                bool need_break = false;
                string email = "";
                string login;
                string password;
                while (true)
                {
                    email = await this.ShowInputAsync("Регистрация", "Введите e-mail.");
                    if (email == null)
                    {
                        need_break = true;
                        break;
                    }
                    if (client.CheckValue("email", email))
                    {
                        break;
                    }
                    else
                    {
                        await this.ShowMessageAsync("Регистрация", "Данный e-mail адрес занят.");
                        continue;
                    }
                }
                if (need_break)
                    break;
                SmtpLog.send_code(email);
                while (true)
                {
                    login = await this.ShowInputAsync("Регистрация", "Введите логин.");
                    if (login == null)
                    {
                        need_break = true;
                        break;
                    }
                    if (client.CheckValue("login", login))
                    {
                        break;
                    }
                    else
                    {
                        await this.ShowMessageAsync("Регистрация", "Данный логин занят.");
                        continue;
                    }
                }
                if (need_break)
                    break;
                while (true)
                {
                    password = await this.ShowInputAsync("Регистрация", "Введите пароль.");
                    if (password == null)
                    {
                        need_break = true;
                        break;
                    }
                    if (password.Length < 6)
                    {
                        await this.ShowMessageAsync("Регистрация", "Пароль должен содержать не меньше 6 символов.");
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (need_break)
                    break;
                while (true)
                {
                    string code = await this.ShowInputAsync("Регистрация", "На ваш почтовый адрес отправлен код. Введите его для завершения регистрации.");
                    if (code == null)
                        break;
                    if (code == SmtpLog.code)
                    {
                        await this.ShowMessageAsync("Регистрация", "Вы успешно зарегистрировались! Войдите, используя введённые данные.");
                        registred = true;
                        client.Regist(login, email, password);
                        break;
                    }
                    else
                    {
                        await this.ShowMessageAsync("Регистрация", "Вы ввели неправельный код, попробуйте еще раз.");
                    }
                }
            }
        }
        private async Task ShowLostDialog()
        {
            string email;
            while (true)
            {
                email = await this.ShowInputAsync("Смена пароля", "Введите e-mail, привязанный к вашему аккаунту.");
                if (email == null)
                    break;
                if (!client.CheckValue("email", email))
                {
                    SmtpLog.send_code(email, "Your code for changing password is {0}. If you don't require changing, ignore this message.");
                    while (true)
                    {
                        string code = await this.ShowInputAsync("Смена пароля", "На ваш почтовый адрес отправлен код. Введите его для завершения регистрации.");
                        if (code == null)
                            break;
                        if (code == SmtpLog.code)
                        {
                            break;
                        }
                        else
                        {
                            await this.ShowMessageAsync("Смена пароля", "Вы ввели неправельный код, попробуйте еще раз.");
                        }
                    }
                    string password;
                    bool valide_pass = false;
                    while (true)
                    {
                        password = await this.ShowInputAsync("Смена пароля", "Введите пароль.");
                        if (password == null)
                        {
                            break;
                        }
                        if (password.Length < 6)
                        {
                            await this.ShowMessageAsync("Смена пароля", "Пароль должен содержать не меньше 6 символов.");
                            continue;
                        }
                        else
                        {
                            valide_pass = true;
                            break;
                        }
                    }
                    if (valide_pass)
                    {
                        client.RefreshValue("password", email, password);
                        await this.ShowMessageAsync("Смена пароля", "Вы успешно сменили пароль.");
                        break;
                    }
                }
            }
        }
        #endregion
        #region UI_manipulations
        public void addMes(string mes)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                Chat_box.Items.Add(mes);
                UpdateScrollBar(Chat_box);
            });
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
        public byte[] GetScreenshot()
        {
            Graphics graph = null;
            var bmp = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            graph = Graphics.FromImage(bmp);
            graph.CopyFromScreen(0, 0, 0, 0, bmp.Size);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);


            return ms.ToArray();

        }
        #endregion
        #region UIEvents
        private void Send_but_Click(object sender, RoutedEventArgs e)
        {
            if(Msg_box.Text!="")
            {
                addMes(Login + " : " + Msg_box.Text);
                client.SendMessage(new TMsg(Login, "All", Msg_box.Text));
                UpdateScrollBar(Chat_box);
                Msg_box.Clear();
            }
        }
        private void RequestChat_Click(object sender, RoutedEventArgs e)
        {
            Request_PChat();
        }
        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
            {
                Send_but_Click(sender, new RoutedEventArgs());
            }
        }     
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                client.SendMessage(new TMsg(Login, "Server", "Offline"));
            }
            catch
            {

            }
        }
        #endregion
        #region Notification_Settings
        public void RefreshNotifications()
        {
            client.RefreshNotificationSettings(Login, AllowMessageNotifications.ToString() + "|" + AllowPMessageNotifications.ToString() + "|" + AllowConnectNotifications.ToString());
        }
        public void Show_Push(string text, string type)
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
                push.Close();
                _push(text);
            }
        }
        public void _push(string text)
        {
            push = new Push();
            push.Inf.Text = text;
            push.Focus();
            push.Show();
            IsNotificationDisplayed = true;
        }
        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            string head = (string)(sender as MahApps.Metro.Controls.ToggleSwitch).Header;
            switch (head)
            {
                case "Обычные сообщения":
                    AllowMessageNotifications = true;
                    break;
                case "Приватные сообщения":
                    AllowPMessageNotifications = true;
                    break;
                case "Подключения":
                    AllowConnectNotifications = true;
                    break;
            }
            RefreshNotifications();
        }
        public void GetNotificationSettings()
        {
            var settings = client.GetNotificationSettings(Login).Split('|');
            AllowMessageNotifications = Convert.ToBoolean(settings[0]);
            AllowPMessageNotifications = Convert.ToBoolean(settings[1]);
            AllowConnectNotifications = Convert.ToBoolean(settings[2]);
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                Msg_toggle.IsChecked = AllowMessageNotifications;
                PMsg_toggle.IsChecked = AllowPMessageNotifications;
                Connect_toggle.IsChecked = AllowConnectNotifications;
            });
        }
        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            string head = (string)(sender as MahApps.Metro.Controls.ToggleSwitch).Header;
            switch (head)
            {
                case "Обычные сообщения":
                    AllowMessageNotifications = false;
                    break;
                case "Приватные сообщения":
                    AllowPMessageNotifications = false;
                    break;
                case "Подключения":
                    AllowConnectNotifications = false;
                    break;

            }
            RefreshNotifications();
        }
        #endregion
        #region Theme_Settings
        public void GetThemeSettings()
        {
            var settings = client.GetThemeSettings(Login).Split('|');
            if (settings != null)
            {
                Theme = settings[0];
                Accent = settings[1];
                Change_Style();
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
        public void Change_Style()
        {
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(Accent), ThemeManager.GetAppTheme(Theme));
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
        public void change_theme(string theme)
        {
            Theme = theme;
            client.RefreshThemeSettings(Login, String.Format("{0}|{1}", Accent, Theme));
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(Accent), ThemeManager.GetAppTheme(Theme));
        }
        public void change_accent(string accent)
        {
            Accent = accent;
            client.RefreshThemeSettings(Login, String.Format("{0}|{1}", Accent, Theme));
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(Accent), ThemeManager.GetAppTheme(Theme));
        }
        #endregion

        
    }
}
