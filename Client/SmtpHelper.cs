using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    public static class SmtpLog
    {
        public static SmtpClient client { get; set; }
        private class SmtpInfo
        {
            public string Email { get; set; }
            public string Body { get; set; }
            public SmtpInfo(string email, string body)
            {
                Email = email;
                Body = body;
            }
        }
        public static bool IsCreated { get; set; } = false;
        public static string code { get; set; }
        public static void _send_code(object data)
        {
            try
            {
                string body = (data as SmtpInfo).Body;
                string email = (data as SmtpInfo).Email;
                Random random = new Random();
                string coode = "";
                for (int i = 0; i < 8; i++)
                {
                    int num = random.Next(0, 9);
                    coode += Convert.ToString(num);
                }
                code = coode;
                MailMessage msg = new MailMessage();
                msg.To.Add(email);
                msg.From = new MailAddress("natsuki-bot@rambler.ru");
                msg.Subject = "Confirmation code.";
                msg.Body = String.Format(body, coode);
                client.Send(msg);
            }
            catch
            {
                code = "-1";
            }
        }
        public static string send_code(string email, string body = "Your code is {0}. Thank's for registration!")
        {
            try
            {
                Thread send = new Thread(new ParameterizedThreadStart(_send_code));
                send.Start(new SmtpInfo(email, body));
                return code;
            }
            catch (Exception e)
            {
                MessageBox.Show("SMTP ошибка.", "Ошибка!");
                return null;
            }

        }
        public static void client_create(string login, string pass)
        {
            try
            {
                client = new SmtpClient("smtp.rambler.ru", 587);
                client.EnableSsl = true;
                client.Timeout = 100000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(login, pass);
                IsCreated = true;
            }
            catch (Exception e)
            {
                IsCreated = false;
            }

        }
    }
}
