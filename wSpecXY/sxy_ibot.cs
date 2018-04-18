using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.IO;

namespace wSpecXY
{
    class sxy_ibot
    {
        private static string app_key, app_secret, user_id;

        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[ibot] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        private static String Hex(byte[] data)
        {
            String r = "";
            for (int i = 0; i < data.Length; i++)
                r += data[i].ToString("X2");
            return r.ToLower();
        }

        private static string calc_x_auth()
        {
            String realm = "xiaoi.com";
            String method = "POST";
            String uri = "/ask.do";
            byte[] b = new byte[20];
            new Random().NextBytes(b);
            String nonce = Hex(b);
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] HA1 = sha.ComputeHash(Encoding.UTF8.GetBytes(app_key + ":" + realm + ":" + app_secret));
            byte[] HA2 = sha.ComputeHash(Encoding.UTF8.GetBytes(method + ":" + uri));
            String sign = Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(Hex(HA1)+":"+nonce+":"+Hex(HA2))));

            return "app_key=\"" + app_key + "\", nonce=\"" + nonce + "\"" + ", signature=\"" + sign + "\"";
        }

        public static void config(string _app_key, string _app_secret, string _user_id)
        {
            app_key       = _app_key;
            app_secret    = _app_secret;
            user_id       = _user_id;
        }

        public static string exec(string request)
        {
            string request_string = "question=" + request;
            string result = string.Empty;

            string x_auth = calc_x_auth();
            log(x_auth);

            byte[] request_byte = Encoding.UTF8.GetBytes(request_string);

            HttpWebRequest  web_request = (HttpWebRequest)WebRequest.Create("http://nlp.xiaoi.com/ask.do?platform=custom");
            web_request.Method = "POST";
            web_request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            web_request.ContentLength = request_byte.Length;
            web_request.Headers.Add("X-Auth", x_auth);
            web_request.ServicePoint.Expect100Continue = false;
            web_request.Timeout = 1500;

            log("Req:" + request);

            try
            {
                Stream data_stream = web_request.GetRequestStream();
                data_stream.Write(request_byte, 0, request_byte.Length);
                data_stream.Close();

                WebResponse web_response = web_request.GetResponse();
                data_stream = web_response.GetResponseStream();
                StreamReader data_reader = new StreamReader(data_stream);
                result = data_reader.ReadToEnd();

                log("Rpl:" + result);

                data_reader.Close();
                data_stream.Close();
                web_response.Close();
            }
            catch (Exception ex)
            {
                log(LOG_TYPE.LOG_ERROR, ex.Message);
            }

            if ((result.Length == 0) || (result.StartsWith("主人还没给我设置这类话题的回复")))
            {
                log(LOG_TYPE.LOG_ERROR, "ibot cannot handle");
                result = string.Empty;
            }

            return result;
        }
    }
}
