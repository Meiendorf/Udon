using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace Client
{
    [DataContract(Namespace = "Wudon")]
    public class TMsg
    {
        [DataMember]
        public string from;
        [DataMember]
        public string to;
        [DataMember]
        public string body;
        [DataMember]
        public string type;
        public TMsg(string from, string to, string body, string _type = "Normal")
        {
            this.from = from;
            this.body = body;
            this.to = to;
            type = _type;
        }
    }

    [ServiceContract]
    public interface IContract
    {
        [OperationContract]
        TMsg SendMessage(TMsg msg);
        [OperationContract]
        TMsg[] ReceiveUnread(string login);
        [OperationContract]
        TMsg SendPMessage(TMsg msg);
        [OperationContract]
        bool NeedPChating(string login);
    }

    [ServiceContract]
    public interface IScreenContract : IContract
    {
        [OperationContract]
        bool SendScreen(ScreenInfo screen);

        [OperationContract]
        bool NeedScreenSharing(string login);
    }

    [ServiceContract]
    public interface IAuthContract : IContract
    {
        [OperationContract]
        string Auth(string login, string password);

        [OperationContract]
        string Regist(string login, string email, string password);

        [OperationContract]
        bool CheckValue(string type, string body);

        [OperationContract]
        void RefreshValue(string type, string body, string value);
    }

    [ServiceContract]
    public interface CustomContract : IContract
    {
        [OperationContract]
        string GetThemeSettings(string login);
        [OperationContract]
        string GetNotificationSettings(string login);

        [OperationContract]
        void RefreshThemeSettings(string login, string settings);
        [OperationContract]
        void RefreshNotificationSettings(string login, string settings);
    }

    [ServiceContract]
    public interface IUdonContract : IScreenContract, IAuthContract, CustomContract
    {

    }

    [DataContract(Namespace = "Wudon")]
    public class ScreenInfo
    {
        [DataMember]
        public string From;
        [DataMember]
        public string To;
        [DataMember]
        public byte[] Image;

        public ScreenInfo(string from, string to, byte[] image)
        {
            From = from;
            To = to;
            Image = image;
        }
    }

}
