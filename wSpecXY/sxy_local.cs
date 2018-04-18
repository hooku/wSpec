using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

namespace wSpecXY
{
    class sxy_local
    {
        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[loca] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        public class LocalRegExItem
        {
            [XmlElement(ElementName = "Request" )]
            public string request;

            [XmlElement(ElementName = "Url"     )]
            public string url;

            [XmlElement(ElementName = "Parser"  )]
            public string parser;

            [XmlElement(ElementName = "Reply"   )]
            public string reply;

            public LocalRegExItem(string _request, string _url, string _parser, string _replay)
            {
                request = _request;
                url     = _url;
                parser  = _parser;
                reply   = _replay;
            }

            public LocalRegExItem()
            {
                ;
            }
        }

        public static List<LocalRegExItem> intrp_table = new List<LocalRegExItem>
        {
            new LocalRegExItem(
                @"(.*)今(天|日)(.*?)天气(.*)",
                @"http://m.baidu.com/s?word=上海天气",
                @"(?<=今天.*?\)).*?(?=明天)",
                @"今天天气：$1"),
            new LocalRegExItem(
                @"(.*)明(天|日)(.*?)天气(.*)",
                @"http://m.baidu.com/s?word=上海天气",
                @"(?<=明天.*?\)).*?(?=后天)",
                @"明天天气：$1"),
            new LocalRegExItem(
                @"(.*)后(天|日)(.*?)天气(.*)",
                @"http://m.baidu.com/s?word=上海天气",
                @"(?<=后天.*?\)).*?(?=中国)",
                @"后天天气：$1"),
            new LocalRegExItem(
                @"(.*)今天(.*?)(星期|日期|几号|日子)(.*)",
                @"http://m.baidu.com/s?word=今天星期几",
                @"(?<=万年历).*?(?=open.baidu.com)",
                @"今天是：$1"),
            new LocalRegExItem(
                @"(.*)明天(.*?)(星期|日期|几号|日子)(.*)",
                @"http://m.baidu.com/s?word=明天星期几",
                @"(?<=万年历).*?(?=open.baidu.com)",
                @"明天是：$1"),
            new LocalRegExItem(
                @"(.*)(股票|证券|上证|大盘|指数)(.*)",
                @"http://m.baidu.com/s?word=上证指数",
                @"(?<=证券之星).*?(?=&#160)",
                @"大盘当前：$1点"),
            new LocalRegExItem(
                @"(.*)(你是谁|你叫什么)(.*)",
                @"",
                @"",
                @"我是MCU语音交互系统"),
            new LocalRegExItem(
                @"(.*)(你多大|你几岁)(.*)",
                @"",
                @"",
                @"3个月了"),
            new LocalRegExItem(
                @"(.*)(你住(.?)哪|家在哪)(.*)",
                @"",
                @"",
                @"华东师范大学信息楼525"),
            new LocalRegExItem(
                @"(.*)开灯(.*)",
                @"",
                @"",
                @"GPIO_ON"),
            new LocalRegExItem(
                @"(.*)关灯(.*)",
                @"",
                @"",
                @"GPIO_OFF"),
            new LocalRegExItem(
                @"(.*)(有(哪些|什么)功能)|(如何使|怎么)用|帮助|说明|help(.*)",
                @"",
                @"",
                @"你可以询问我今天天气怎么样，或者你也可以说“开灯”，这样我将帮你打开LED灯"),
            new LocalRegExItem(
                @"(.*)关机|重启|关闭电源|休眠|待机|睡眠|低功耗(.*)",
                @"",
                @"",
                @"OK，已执行"),
            new LocalRegExItem(
                @"(.*)系统|内存|CPU(.*)",
                @"",
                @"",
                @"Cortex-M 处理器，几百KB内存，MICO系统"),
            new LocalRegExItem(
                @"(.*)搜|放|音乐|歌(.*)",
                @"",
                @"",
                @"对不起，不支持这个功能"),
        };

//         public static string serialize()
//         {
//             StringWriter str_writer = new StringWriter();
//             XmlSerializer xml_ser = new XmlSerializer(typeof(LocalIntrpItem), new XmlRootAttribute("LocalIntrp"));
//             xml_ser.Serialize(str_writer, intrp_table);
// 
//             return str_writer.ToString();
//         }
// 
//         private static string deserialize()
//         {
// 
//         }

        private static string do_exec(string url, string parser)
        {
            string result = string.Empty;

            // download the http data:
            HttpWebRequest web_request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse web_response = web_request.GetResponse();
            Stream data_stream = web_response.GetResponseStream();
            StreamReader data_reader = new StreamReader(data_stream);
            string web_result = data_reader.ReadToEnd();

            data_reader.Close();
            data_stream.Close();
            web_response.Close();

            // strip out html tags:
            web_result = Regex.Replace(web_result, @"<[^>]+>|&nbsp;", "").Trim();

            // parse the http data:
            Regex reg_parser = new Regex(parser);
            MatchCollection match_parser = reg_parser.Matches(web_result);

            if (match_parser.Count > 0)
            {
                result = match_parser[0].ToString();
            }

            return result;
        }

        public static string exec(string request)
        {
            string result = string.Empty;

            foreach (LocalRegExItem intrp_item in intrp_table)
            {
                Regex reg_req = new Regex(intrp_item.request);
                MatchCollection match_req = reg_req.Matches(request);

                // if the "user request" matches "request regex", we continue:
                if (match_req.Count > 0)
                {
                    if (intrp_item.url != string.Empty)
                    {
                        result = do_exec(intrp_item.url, intrp_item.parser).Trim();
                        result = intrp_item.reply.Replace("$1", result);
                    }
                    else
                    {
                        result = intrp_item.reply;
                    }
                    
                    log("Rpl:" + result);
                    break;
                }
            }

            return result;
        }
    }
}
