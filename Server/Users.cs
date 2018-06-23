using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.IO;
using System.Xml;
namespace Server
{

    
    public class User
    {
        public int Id { get; set; }
        public bool NeedScreenSharing { get; set; } = false;
        public bool NeedPChating { get; set; } = false;
        public bool IsOnline { get; set; } = true;
        public List<TMsg> Unreaded { get; set; } = new List<TMsg>();
        public string Login { get; set; }
        public string Pass { get; set; }
        public string Email { get; set; }
        public string Accent { get; set; }
        public string Theme { get; set; }
        public bool Msg_Not { get; set; }
        public bool PMsg_Not { get; set; }
        public bool Conn_Not { get; set; }
        public User(string login, string pass, string email, int id, string accent = "Blue", string theme = "BaseLight", bool msg_not = true, bool pmsg_not = true, bool conn_not = true)
        {
            Email = email;
            Login = login;
            Pass = pass;
            Id = id;
            Accent = accent;
            Theme = theme;
            Msg_Not = msg_not;
            PMsg_Not = pmsg_not;
            Conn_Not = conn_not;
        }
        public bool logIn(string login, string pass)
        {
            if (login == Login && Pass == pass)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public class UsersBase
    {
        private string Path { get; set; }
        public delegate void Change();
        public event Change ChangeEvent = null;
        Dictionary<int, User> Users { get; set; }
        protected int Amount { get; set; }
        public UsersBase(string path = "users.xml")
        {
            Path = path;
            ChangeEvent += OnChange;
            Users = new Dictionary<int, User>();
            Amount = 0;
            this.ReadBase();
        }
        public void RefreshPass(string email, string pass)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Email == email)
                    Users[usr.Id].Pass = pass;
            }
            Changed();
        }
        public void _ReadBase()
        {
            FileStream users = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader rusers = new StreamReader(users);
            while (!rusers.EndOfStream)
            {
                string @string = rusers.ReadLine();
                string[] info = @string.Split(':');
                this.addUser(info[1], info[2], info[0], info[3], info[4], Convert.ToBoolean(info[5]), Convert.ToBoolean(info[6]), Convert.ToBoolean(info[7]));
            }
            // MessageBox.Show(String.Format("{0} {1} {2}", Users[0].Login, Users[0].Pass, Users[0].Email));
        }
        public void ReadBase()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(Path);
            var xRoot = xmlDoc.DocumentElement;
            foreach (XmlNode user in xRoot)
            {
                string email = "";
                string login = "";
                string password = "";
                string accent = "Blue";
                string theme = "BaseLight";
                bool msg_n = true;
                bool pmsg_n = true;
                bool conn_n = true;
                foreach (XmlNode child in user)
                {
                    try
                    {
                        switch (child.Name)
                        {
                            case "email":
                                email = child.InnerText;
                                break;
                            case "login":
                                login = child.InnerText;
                                break;
                            case "password":
                                password = child.InnerText;
                                break;
                            case "accent":
                                accent = child.InnerText;
                                break;
                            case "message_notifications":
                                msg_n = Convert.ToBoolean(child.InnerText);
                                break;
                            case "pmessage_notifications":
                                pmsg_n = Convert.ToBoolean(child.InnerText);
                                break;
                            case "connect_notifications":
                                conn_n = Convert.ToBoolean(child.InnerText);
                                break;

                        }
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
                this.addUser(login, password, email, accent, theme, msg_n, pmsg_n, conn_n);
            }
        }
        public void Changed()
        {
            ChangeEvent.Invoke();
        }
        public void _OnChange()
        {
            App.Current.Dispatcher.BeginInvoke((Action)delegate () {
                FileStream users = new FileStream(Path, FileMode.Create, FileAccess.Write);
                StreamWriter wusers = new StreamWriter(users);
                foreach (var user in Users.Values)
                {
                    wusers.WriteLine(String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", user.Email, user.Login, user.Pass, user.Accent, user.Theme, user.Msg_Not.ToString(), user.PMsg_Not.ToString(), user.Conn_Not.ToString()));
                }
                wusers.Close();
                users.Close();
            });
        }
        public void OnChange()
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(Path);
            XmlNode xRoot = xDoc.DocumentElement;
            xRoot.RemoveAll();
            foreach (var user in Users.Values)
            {
                XmlElement[] attr = new XmlElement[8];
                XmlElement xUser = xDoc.CreateElement("user");
                attr[0] = xDoc.CreateElement("email");
                attr[0].AppendChild(xDoc.CreateTextNode(user.Email));
                attr[1] = xDoc.CreateElement("login");
                attr[1].AppendChild(xDoc.CreateTextNode(user.Login));
                attr[2] = xDoc.CreateElement("password");
                attr[2].AppendChild(xDoc.CreateTextNode(user.Pass));
                attr[3] = xDoc.CreateElement("accent");
                attr[3].AppendChild(xDoc.CreateTextNode(user.Accent));
                attr[4] = xDoc.CreateElement("theme");
                attr[4].AppendChild(xDoc.CreateTextNode(user.Theme));
                attr[5] = xDoc.CreateElement("message_notifications");
                attr[5].AppendChild(xDoc.CreateTextNode(user.Msg_Not.ToString()));
                attr[6] = xDoc.CreateElement("pmessage_notifications");
                attr[6].AppendChild(xDoc.CreateTextNode(user.PMsg_Not.ToString()));
                attr[7] = xDoc.CreateElement("connect_notifications");
                attr[7].AppendChild(xDoc.CreateTextNode(user.Conn_Not.ToString()));
                for (int i = 0; i < attr.Length; i++)
                {
                    xUser.AppendChild(attr[i]);
                }
                xRoot.AppendChild(xUser);
            }
            xDoc.Save(Path);
        }
        public void addUser(string login, string pass, string email, string accent = "Blue", string theme = "BaseLight", bool msg_not = true, bool pmsg_not = true, bool conn_not = true)
        {
            Users.Add(Amount, new User(login, pass, email, Amount, accent, theme, msg_not, pmsg_not, conn_not));
            Amount++;
            Changed();
        }
        public void addUser(User user)
        {
            Users.Add(Amount, user);
            Amount++;
            Changed();
        }
        public User search(string login)
        {
            foreach (var user in Users.Values)
            {
                if (user.Login == login)
                {
                    return user;
                }
            }
            return null;
        }
        public User search_email(string email)
        {
            foreach (var user in Users.Values)
            {
                if (user.Email == email)
                {
                    return user;
                }
            }
            return null;
        }
        public bool EIsInBase(string email)
        {
            User user = search_email(email);
            if (user == null)
                return false;
            return true;
        }
        public bool IsInBase(string login, string pass)
        {
            User user = search(login);
            if (user == null)
                return false;
            return user.logIn(login, pass);
        }
        public bool IsInBase(string login)
        {
            User user = search(login);
            if (user == null)
                return false;
            return true;
        }
        public void RefreshNotifications(string login, string _msg_not, string _pmsg_not, string _conn_not)
        {
            bool msg_not;
            bool pmsg_not;
            bool conn_not;
            msg_not = Convert.ToBoolean(_msg_not);
            pmsg_not = Convert.ToBoolean(_pmsg_not);
            conn_not = Convert.ToBoolean(_conn_not);

            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                {
                    usr.Msg_Not = msg_not;
                    usr.PMsg_Not = pmsg_not;
                    usr.Conn_Not = conn_not;
                }
            }
            Changed();
        }
        public void RefreshTheme(string login, string accent, string theme)
        {
            foreach (var usr in Users.Values)
            {
                if (usr.Login == login)
                {
                    usr.Accent = accent;
                    usr.Theme = theme;
                }
            }
            Changed();
        }
        public void DeleteUser(string login)
        {
            User user = search(login);
            if (user == null)
            {
                return;
            }
            else
            {
                Users.Remove(user.Id);
                Amount--;
            }
            Changed();
        }
    }
}
