using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace wSpecXY
{
    class sxy_simsimi
    {
        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[simi] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        private static string do_exec(string url, string request)
        {
            string result = string.Empty;

            byte[] request_byte = Encoding.UTF8.GetBytes(request);

            HttpWebRequest web_request = (HttpWebRequest)WebRequest.Create(url);
            web_request.Method = "POST";
            web_request.ContentType = "application/x-www-form-urlencoded";
            web_request.ContentLength = request_byte.Length;
            web_request.ServicePoint.Expect100Continue = false;
            web_request.Timeout = 3000;

            try
            {
                Stream data_stream = web_request.GetRequestStream();
                data_stream.Write(request_byte, 0, request_byte.Length);
                data_stream.Close();

                WebResponse web_response = web_request.GetResponse();
                data_stream = web_response.GetResponseStream();
                StreamReader data_reader = new StreamReader(data_stream);
                result = data_reader.ReadToEnd();

                data_reader.Close();
                data_stream.Close();
                web_response.Close();
            }
            catch (Exception ex)
            {
                log(LOG_TYPE.LOG_ERROR, ex.Message);
            }

            return result;
        }

        public static string exec(string request)
        {
            string result = string.Empty;

            string[,] simi_apis = new string[,]
            {
                {"http://www.niurenqushi.com/app/simsimi/ajax.aspx" , "txt="    },
                {"http://www.xiaohuangji.com/ajax.php"              , "para="   },
            };

            for (int i_simi = 0; i_simi < simi_apis.GetLength(0); i_simi ++)
            {
                log("Req:" + request + ", Url:" + simi_apis[i_simi, 0]);
                result = do_exec(simi_apis[i_simi, 0], simi_apis[i_simi, 1] + request);
                log("Rpl:" + result);

                // test if result is success:
                if (result.Length > 0)
                {
                    break;
                }
                else
                {
                    log(LOG_TYPE.LOG_WARN, "simi" + i_simi.ToString() + " failed");
                }
            }

            if ((result.Length == 0) || (result.StartsWith("主人还没给我设置这类话题的回复")))
            {
                log(LOG_TYPE.LOG_ERROR, "simi cannot handle");
                result = string.Empty;
            }

            return result;
        }
    }
}
