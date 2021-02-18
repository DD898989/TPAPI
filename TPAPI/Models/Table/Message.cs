using System;

namespace TPAPI.Models.Table
{
    public enum EType
    {
        Mail = 1,
        SMS = 2,
        Chat = 3,
        Ticket = 4,
    }

    public enum EProvider
    {
        Twilio = 1,
        SendGrid = 2,
        SendinBlue = 3,
        LiveChat = 4,
        Plivo = 5,
        Mailgun = 6,
        Telesign=7,
    }

    public class Messages
    {
        public EProvider provider;
        public EType type;
        public int brandID; //代理商的ID
        public string mainAccountID; //玩家的帳號
        public DateTime datetime;
        public string receiver;
        public string sender;
        public string content;
        public string title;

        static public string sqlTimeFormat()
        {
            return "yyyy-MM-dd HH:mm:ss";
        }
    }


    public class MessagesLoad
    {
        public int brandID;
        public string mainAccountID;
        public DateTime datetime;
        public string receiver;
        public string content;
        public string title;
    }
}