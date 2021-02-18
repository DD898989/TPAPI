using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using TPAPI.Models;
using TPAPI.Models.Table;
using TPAPI.Controllers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;


namespace TPAPI.Provider
{
    public partial/*沒有model*/ class LiveChatController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static NotifyTokenAgent _modelTokenAgent = null;
        public static NotifyTokenCustomer _modelTokenCustomer = null;

        public static string LiveChat_AccountID = ConfigurationManager.AppSettings["LiveChat_AccountID"];
        public static string LiveChat_PAT = ConfigurationManager.AppSettings["LiveChat_PAT"];
        public static string LiveChat_ClientID = ConfigurationManager.AppSettings["LiveChat_ClientID"];
        public static string LiveChat_ClientSecret = ConfigurationManager.AppSettings["LiveChat_ClientSecret"];
        public static string LiveChat_Account = ConfigurationManager.AppSettings["LiveChat_Account"];
        public static string LiveChat_Password = ConfigurationManager.AppSettings["LiveChat_Password"];
        public static string LiveChat_EntityID = ConfigurationManager.AppSettings["LiveChat_EntityID"];

        public static void Run()
        {
            //檢查相關設定
            {
                var client = new RestClient("https://api.livechatinc.com/webhooks");
                var request = new RestRequest(Method.GET);

                request.AddHeader("Content-Type", "multipart/form-data; boundary=<calculated when request is sent>");
                request.AddHeader("X-API-Version", "2");
                string username = ConfigurationManager.AppSettings["LiveChat_EntityID"];
                string password = ConfigurationManager.AppSettings["LiveChat_PAT"];
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                request.AddHeader("Authorization", "Basic " + svcCredentials);

                var response = client.Execute(request);

                logger.Info("live chat web hook info:" + response.Content);

                JToken json = JObject.Parse(response.Content);
                JToken jsonChatStart = null;
                JToken jsonChatEnd = null;
                JToken jsonChatTicket = null;


                //因為多個環境可能共用一個LiveChat帳號 所以需要透過url來找
                foreach (var item in json["events"])
                {
                    if (item["url"].ToString() == ConfigurationManager.AppSettings["ChatStartsNotify"])
                    {
                        jsonChatStart = item;
                    }

                    if (item["url"].ToString() == ConfigurationManager.AppSettings["ChatEndsNotify"])
                    {
                        jsonChatEnd = item;
                    }

                    if (item["url"].ToString() == ConfigurationManager.AppSettings["ChatTicketNotify"])
                    {
                        jsonChatTicket = item;
                    }
                }

                if (jsonChatStart == null) { logger.Error("jsonChatStart == null"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd == null) { logger.Error("jsonChatEnd == null"); throw new Exception("live chat web hook error:"); }
                if (jsonChatTicket == null) { logger.Error("jsonChatTicket == null"); throw new Exception("live chat web hook error:"); }

                if (jsonChatStart["url"].ToString() != ConfigurationManager.AppSettings["ChatStartsNotify"]) { logger.Error("jsonChatStart url"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["url"].ToString() != ConfigurationManager.AppSettings["ChatEndsNotify"]) { logger.Error("jsonChatEnd url"); throw new Exception("live chat web hook error:"); }
                if (jsonChatTicket["url"].ToString() != ConfigurationManager.AppSettings["ChatTicketNotify"]) { logger.Error("jsonChatTicket url"); throw new Exception("live chat web hook error:"); }

                if (jsonChatStart["event_type"].ToString() != "chat_started") { logger.Error("jsonChatStart event_type"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["event_type"].ToString() != "chat_ended") { logger.Error("jsonChatEnd event_type"); throw new Exception("live chat web hook error:"); }
                if (jsonChatTicket["event_type"].ToString() != "ticket_created") { logger.Error("jsonChatTicket event_type"); throw new Exception("live chat web hook error:"); }

                if (jsonChatStart["verified"].ToObject<bool>() != true) { logger.Error("jsonChatStart verified"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["verified"].ToObject<bool>() != true) { logger.Error("jsonChatEnd verified"); throw new Exception("live chat web hook error:"); }
                if (jsonChatTicket["verified"].ToObject<bool>() != true) { logger.Error("jsonChatTicket verified"); throw new Exception("live chat web hook error:"); }

                if (jsonChatStart["data_types"].ToList<object>().Count != 3) { logger.Error("jsonChatStart data_types count"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["data_types"].ToList<object>().Count != 3) { logger.Error("jsonChatEnd data_types count"); throw new Exception("live chat web hook error:"); }
                if (jsonChatTicket["data_types"].ToList<object>().Count != 1) { logger.Error("jsonChatTicket data_types count"); throw new Exception("live chat web hook error:"); }

                if (jsonChatTicket["data_types"][0].ToString() != "ticket") { logger.Error("jsonChatTicket data_types 0"); throw new Exception("live chat web hook error:"); }

                if (jsonChatStart["data_types"][0].ToString() != "chat") { logger.Error("jsonChatStart data_types 0"); throw new Exception("live chat web hook error:"); }
                if (jsonChatStart["data_types"][1].ToString() != "visitor") { logger.Error("jsonChatStart data_types 1"); throw new Exception("live chat web hook error:"); }
                if (jsonChatStart["data_types"][2].ToString() != "pre_chat_survey") { logger.Error("jsonChatStart data_types 2"); throw new Exception("live chat web hook error:"); }

                if (jsonChatEnd["data_types"][0].ToString() != "chat") { logger.Error("jsonChatEnd data_types 0"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["data_types"][1].ToString() != "visitor") { logger.Error("jsonChatEnd data_types 1"); throw new Exception("live chat web hook error:"); }
                if (jsonChatEnd["data_types"][2].ToString() != "pre_chat_survey") { logger.Error("jsonChatEnd data_types 2"); throw new Exception("live chat web hook error:"); }
            }

            //實際執行
            {
                if (_modelTokenAgent != null)
                {
                    throw new Exception("if (_modelTokenAgent != null)");
                }

                var url = "";
                url += "https://accounts.livechat.com/?";
                url += "response_type=code";
                url += "&";
                url += ("client_id=" + LiveChat_ClientID);
                url += "&";
                url += "redirect_uri=";
                url += ConfigurationManager.AppSettings["ChatTokenNotify"];

                logger.Info("try open url by chrome driver:" + url);


                new Thread(() =>
                {
                    IWebDriver driver = new ChromeDriver();
                    try
                    {
                        driver.Navigate().GoToUrl(url);//開啟網頁
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3000);//隱式等待 - 直到畫面跑出資料才往下執行

                        logger.Info("live chat title:" + driver.Title);

                        //輸入帳號
                        IWebElement inputAccount = driver.FindElement(By.Name("email"));
                        //清除按鈕
                        inputAccount.Clear();
                        Thread.Sleep(500);
                        inputAccount.SendKeys(LiveChat_Account);
                        Thread.Sleep(500);

                        IWebElement inputPassword = driver.FindElement(By.Name("password"));
                        inputPassword.Clear();
                        Thread.Sleep(500);
                        inputPassword.SendKeys(LiveChat_Password);
                        Thread.Sleep(500);

                        //點擊執行
                        IWebElement submitButton = driver.FindElement(By.XPath("/html/body/div[2]/div/div[2]/div[2]/div/div/form/div[3]/button/span"));
                        Thread.Sleep(500);
                        submitButton.Click();
                        Thread.Sleep(2000);

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                    finally
                    {
                        driver.Quit();
                    }

                }).Start();

            }
        }

        static System.Timers.Timer UpdateAgentToken_0 = new System.Timers.Timer();
        static void UpdateAgentToken_1(object source, System.Timers.ElapsedEventArgs e)
        {
            UpdateAgentToken_0.Stop();
            try
            {
                if (!(_modelTokenAgent?.expires_in > 0))
                {
                    logger.Error("if (!(_modelTokenAgent?.expires_in > 0))");
                    UpdateAgentToken_0.Interval = 60 * 1000; //if error, log every 60 sec
                }
                else
                {
                    logger.Info("now update agent token");

                    UpdateAgentToken_2(false);

                    logger.Info("wait update agent token " + _modelTokenAgent.expires_in + " seconds");

                    UpdateAgentToken_0.Interval = (_modelTokenAgent.expires_in - 600) * 1000;//600=在token過期前十分鐘做更新
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                UpdateAgentToken_0.Interval = 60 * 1000; //if error, log every 60 sec
            }
            finally
            {
                UpdateAgentToken_0.Start();
            }
        }
        static void UpdateAgentToken_2(bool isFirst, string firstCode = "")
        {
            try
            {
                var client = new RestClient("https://accounts.livechat.com/token");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                string username = LiveChat_AccountID;
                string password = LiveChat_PAT;
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                request.AddHeader("Authorization", "Basic " + svcCredentials);
                if (isFirst)
                {
                    request.AddParameter("grant_type", "authorization_code");
                    request.AddParameter("redirect_uri", ConfigurationManager.AppSettings["ChatTokenNotify"]);
                    request.AddParameter("code", firstCode);
                }
                else
                {
                    if (!(_modelTokenAgent?.refresh_token?.Length > 0))
                    {
                        logger.Error(@"if (!(_modelTokenAgent?.refresh_token?.Length > 0))");
                        return;
                    }
                    else
                    {
                        request.AddParameter("grant_type", "refresh_token");
                        request.AddParameter("refresh_token", _modelTokenAgent.refresh_token);
                    }
                }
                request.AddParameter("client_id", LiveChat_ClientID);
                request.AddParameter("client_secret", LiveChat_ClientSecret);
                var jsonContent = client.Execute(request).Content;

                _modelTokenAgent = JsonConvert.DeserializeObject<NotifyTokenAgent>(jsonContent);

                if (!(_modelTokenAgent?.access_token?.Length > 0))
                {
                    logger.Error(@"if (!(_modelTokenAgent?.access_token?.Length > 0))", jsonContent);
                    return;
                }

                logger.Info("新的access_token:" + _modelTokenAgent.access_token);

                if (isFirst)
                {
                    UpdateAgentToken_0.Elapsed += UpdateAgentToken_1;
                    UpdateAgentToken_0.Interval = (_modelTokenAgent.expires_in - 600) * 1000;//600=在token過期前十分鐘做更新;
                    UpdateAgentToken_0.Start();
                    UpdateCustomerToken_2(true);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }


        static System.Timers.Timer UpdateCustomerToken_0 = new System.Timers.Timer();
        static void UpdateCustomerToken_1(object source, System.Timers.ElapsedEventArgs e)
        {
            UpdateCustomerToken_0.Stop();
            try
            {
                if (!(_modelTokenCustomer?.expires_in > 0))
                {
                    UpdateCustomerToken_0.Interval = 60 * 1000; //if error, log every 60 sec
                }
                else
                {
                    logger.Info("now update customer token");

                    UpdateCustomerToken_2(false);

                    logger.Info("wait update customer token " + _modelTokenCustomer.expires_in + " seconds");

                    UpdateCustomerToken_0.Interval = (_modelTokenCustomer.expires_in - 600) * 1000;//600=在token過期前十分鐘做更新
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                UpdateCustomerToken_0.Interval = 60 * 1000; //if error, log every 60 sec
            }
            finally
            {
                UpdateCustomerToken_0.Start();
            }
        }
        static void UpdateCustomerToken_2(bool isFirst)
        {
            var client = new RestClient("https://accounts.livechat.com/customer/");
            var request = new RestRequest(Method.POST);

            if (!(_modelTokenAgent?.access_token?.Length > 0))
            {
                logger.Error(@"if (!(_modelTokenAgent?.access_token?.Length > 0))");
                return;
            }

            request.AddHeader("Authorization", "Bearer " + _modelTokenAgent.access_token);


            Dictionary<string, string> req = new Dictionary<string, string>()
                {
                    {"client_id", LiveChat_ClientID },
                    {"response_type", "token" },
                    {"redirect_uri", ConfigurationManager.AppSettings["ChatTokenNotify"] },
                };
            var str = JsonConvert.SerializeObject(req);
            request.AddParameter("text/plain", str, ParameterType.RequestBody);


            var jsonContent = client.Execute(request).Content;
            _modelTokenCustomer = JsonConvert.DeserializeObject<NotifyTokenCustomer>(jsonContent);
            if (!(_modelTokenCustomer?.access_token?.Length > 0))
            {
                logger.Error(@"if (!(_modelTokenCustomer?.access_token?.Length > 0))", jsonContent);
                return;
            }

            logger.Info("新的customer_token:" + _modelTokenCustomer.access_token);

            if (isFirst)
            {
                UpdateCustomerToken_0.Elapsed += UpdateCustomerToken_1;
                UpdateCustomerToken_0.Interval = (_modelTokenCustomer.expires_in - 600) * 1000;//600=在token過期前十分鐘做更新;
                UpdateCustomerToken_0.Start();
            }
        }

        [HttpPost]
        public HttpResponseMessage ChatStartsNotify()
        {
            try
            {
                Chat_1 notify = null;
                string name = "";
                string thread_id = "";

                {
                    var content = Request.Content.ReadAsStringAsync().Result;

                    logger.Info(JsonConvert.DeserializeObject(content));//用jsoncovert才能轉成unicode

                    notify = JsonConvert.DeserializeObject<Chat_1>(content);

                    if (!(notify?.chat?.id?.Length > 0))
                    {
                        logger.Error(@"if (!(notify?.chat?.id?.Length > 0))", JsonConvert.DeserializeObject(content));
                        goto ReturnOK;
                    }


                    thread_id = notify.chat.id;


                    if (!(notify?.visitor?.name?.Length > 0))
                    {
                        logger.Error("if (!(notify?.visitor?.name?.Length > 0))", JsonConvert.DeserializeObject(content));
                        goto ReturnOK;
                    }

                    name = notify.visitor.name;
                }

                var chat_id = "";

                {
                    var client = new RestClient("https://api.livechatinc.com/v3.2/agent/action/list_chats");
                    var request = new RestRequest(Method.POST);
                    string username = LiveChat_AccountID;
                    string password = LiveChat_PAT;
                    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));

                    request.AddHeader("Authorization", "Basic " + svcCredentials);
                    request.AddParameter("application/json", "{\n\t\"filters\": {\n\t\t\"group_ids\": [0, 1]\n\t}\n}", ParameterType.RequestBody);

                    var content = client.Execute(request).Content;

                    ListChat model = JsonConvert.DeserializeObject<ListChat>(content);

                    foreach (var chat in model.chats_summary)
                    {
                        if (chat.last_thread_summary.id == thread_id)
                        {
                            chat_id = chat.id;
                            break;
                        }
                    }


                    if (string.IsNullOrEmpty(chat_id))
                    {
                        logger.Error("if (string.IsNullOrEmpty(chat_id))", JsonConvert.DeserializeObject(content));
                        goto ReturnOK;
                    }
                }

                {
                    SendModel model = new SendModel
                    {
                        BrandID = -1,
                        MainAccountID = "",
                        Receiver = chat_id,
                        Title = "",
                        Content = "",
                    };

                    var dt = (new DateTime(1970, 1, 1, 0, 0, 0)).AddHours(8).AddSeconds((double)notify.chat.started_timestamp);
                    var db = MsgController.Save(model, EProvider.LiveChat, EType.Chat, "", dt);
                    if (db.code != Code.成功)
                    {
                        logger.Error(db.errorData);
                        goto ReturnOK;
                    }
                }



                var CannedMsg = "";

                {
                    foreach (var v in notify.pre_chat_survey)
                    {
                        switch (v.type)
                        {
                            //case "name":
                            //case "email":
                            //case "checkbox":
                            //case "radio":
                            case "question":
                                if (v.label == "單一問題:" && !string.IsNullOrEmpty(v.answer))
                                {
                                    var 回答 = v.answer;
                                }
                                break;
                            case "select":
                                if (v.label == "多重選項:")
                                {
                                    var chosens = v.answers.Where<Answer>(c => c.chosen).ToList();
                                    var two_three = chosens.Where<Answer>(c => c.label == "第二個選項的答案" || c.label == "第三個選項的答案").ToList();
                                }
                                break;

                            default:
                                logger.Error("unknown type:" + v.type);
                                break;
                        }
                    }



                    CannedMsg = "自動回傳訊息"; //TODO: 根據 pre_chat_survey 來決定自動回傳訊息的內容
                }

                {
                    logger.Info(CannedMsg);

                    var client = new RestClient("https://api.livechatinc.com/v3.2/agent/action/send_event");
                    var request = new RestRequest(Method.POST);

                    if (!(_modelTokenAgent?.access_token?.Length > 0))
                    {
                        logger.Error(@"if (!(_modelTokenAgent?.access_token?.Length > 0))");
                        goto ReturnOK;
                    }

                    request.AddHeader("Authorization", "Bearer " + _modelTokenAgent.access_token);
                    request.AddParameter("application/json", "{ \"chat_id\": \"" + chat_id + "\", \"event\": { \"type\": \"message\", \"text\": \"" + CannedMsg + "\", \"recipients\": \"all\" } }", ParameterType.RequestBody);

                    var jsonContent = client.Execute(request).Content;
                    if (!jsonContent.Contains("event_id"))
                    {
                        logger.Error("if (!jsonContent.Contains(\"event_id\"))", jsonContent);
                        goto ReturnOK;
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                goto ReturnOK;
            }

        ReturnOK:
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        public HttpResponseMessage ChatEndsNotify()
        {
            var content = Request.Content.ReadAsStringAsync().Result;

            logger.Info(JsonConvert.DeserializeObject(content));//用jsoncovert才能轉成unicode

            Chat_1 notify = JsonConvert.DeserializeObject<Chat_1>(content);

            foreach (var msg in notify.chat.messages)
            {
                SendModel model = new SendModel
                {
                    BrandID = -1,
                    MainAccountID = "",
                    Receiver = notify.chat.id,
                    Title = "",
                    Content = msg.text,
                };

                var dt = (new DateTime(1970, 1, 1, 0, 0, 0)).AddHours(8).AddSeconds((double)msg.timestamp);
                var db = MsgController.Save(model, EProvider.LiveChat, EType.Chat, msg.author_name, dt);
                if (db.code != Code.成功)
                {
                    logger.Error(db.errorData);
                    break;
                }
            }


            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [HttpGet]
        public HttpResponseMessage ChatTokenNotify(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                logger.Error(@"if (string.IsNullOrEmpty(code))");
            }
            else
            {
                logger.Info(String.Format("ChatTokenNotify code:{0} state:{1}",code,state));
                UpdateAgentToken_2(true, code);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        public HttpResponseMessage ChatTicketNotify()
        {
            try
            {
                var content = Request.Content.ReadAsStringAsync().Result;
                logger.Info(JsonConvert.DeserializeObject(content));//用jsoncovert才能轉成unicode
                Ticket_1 notify = JsonConvert.DeserializeObject<Ticket_1>(content);
                SendModel model = new SendModel
                {
                    BrandID = -1,
                    MainAccountID = "",
                    Receiver = notify.ticket.id,
                    Title = notify.ticket.subject,
                    Content = notify.ticket.events[0].message,
                };

                var dt = notify.ticket.events[0].date;
                var db = MsgController.Save(model, EProvider.LiveChat, EType.Ticket,


                    "name:" + notify.ticket.events[0].author.name +
                    "   email:" + notify.ticket.requester.mail

                    , dt);
                if (db.code != Code.成功)
                {
                    logger.Error(db.errorData);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }


            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }


    public partial/*只有model*/ class LiveChatController
    {
        public class Author
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(name))]
            public string name;
            [JsonProperty(nameof(type))]
            public string type;
        }
        public class Events
        {
            [JsonProperty(nameof(author))]
            public Author author;
            [JsonProperty(nameof(date))]
            public DateTime date;
            [JsonProperty(nameof(message))]
            public string message;
        }
        public class Requester
        {
            [JsonProperty(nameof(mail))]
            public string mail;
            [JsonProperty(nameof(name))]
            public string name;
            [JsonProperty(nameof(utc_offset))]
            public string utc_offset;
            [JsonProperty(nameof(ip))]
            public string ip;
        }
        public class Answer
        {
            public string label;
            public bool chosen;
        }
        public class NotifyTokenCustomer
        {
            public string access_token;
            public string client_id;
            public string entity_id;
            public int expires_in;
            public string token_type;
        }
        public class NotifyTokenAgent
        {
            public string access_token;
            public string account_id;
            public string entity_id;
            public int expires_in;
            public int license_id;
            public string organization_id;
            public string refresh_token;
            public string scope;
            public string token_type;
        }
        public class Ticket_1
        {
            public string event_type;
            public string token;
            public string license_id;
            public Ticket_2 ticket;

        }
        public class Ticket_2
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(status))]
            public string status;
            [JsonProperty(nameof(subject))]
            public string subject;
            [JsonProperty(nameof(requester))]
            public Requester requester;
            [JsonProperty(nameof(events))]
            public List<Events> events;

        }
        public class Chat_1
        {
            public string event_type;
            public string event_unique_id;
            public string token;
            public string license_id;
            public string lc_version;
            public Chat_2 chat;
            public Visitor visitor;
            public List<PreChatSurvey> pre_chat_survey;
        }
        public class Chat_2
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(started_timestamp))]
            public long started_timestamp;
            [JsonProperty(nameof(ended_timestamp))]
            public long ended_timestamp;
            [JsonProperty(nameof(url))]
            public string url;
            [JsonProperty(nameof(referer))]
            public string referer;
            [JsonProperty(nameof(messages))]
            public List<Msgs> messages;
            //public List<Attach> attachments;
            //public List<Events> events;
            //public List<Tags> tags;
            //public List<Groups> groups;
        }
        public class PreChatSurvey
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(type))]
            public string type;
            [JsonProperty(nameof(label))]
            public string label;
            [JsonProperty(nameof(answer))]
            public string answer;
            [JsonProperty(nameof(answers))]
            public List<Answer> answers;
        }
        public class Visitor
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(name))]
            public string name;
            [JsonProperty(nameof(email))]
            public string email;
            //CustomVariables custom_variables;
            [JsonProperty(nameof(country))]
            public string country;
            [JsonProperty(nameof(city))]
            public string city;
            [JsonProperty(nameof(language))]
            public string language;
            [JsonProperty(nameof(page_current))]
            public string page_current;
            [JsonProperty(nameof(timezone))]
            public string timezone;
        }
        public class Msgs
        {
            [JsonProperty(nameof(user_type))]
            public string user_type;
            [JsonProperty(nameof(author_name))]
            public string author_name;
            [JsonProperty(nameof(agent_id))]
            public string agent_id;
            [JsonProperty(nameof(text))]
            public string text;
            [JsonProperty(nameof(json))]
            public string json;
            [JsonProperty(nameof(timestamp))]
            public long timestamp;
        }
        public class ListChat
        {
            public List<ChatSummary> chats_summary;
        }
        public class ChatSummary
        {
            [JsonProperty(nameof(id))]
            public string id;
            [JsonProperty(nameof(last_thread_summary))]
            public LastThreadSummary last_thread_summary;
        }
        public class LastThreadSummary
        {
            [JsonProperty(nameof(id))]
            public string id;
        }
    }
}