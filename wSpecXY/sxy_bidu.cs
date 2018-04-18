using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace wSpecXY
{
    class sxy_bidu
    {
        private static string grant_type;
        private static string client_id;
        private static string client_secret;
        private static string access_token;
        private static string user_id;
        private static string language;

        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[bidu] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        public static void config(string _grant_type, string _client_id, string _client_secret,
            string _access_token, string _user_id, string _language)
        {
            grant_type      = _grant_type;
            client_id       = _client_id;
            client_secret   = _client_secret;
            access_token    = _access_token;
            user_id         = _user_id;
            language        = _language;
        }

        [DataContract]
        public class OathResp
        {
            [DataMember(Name = "access_token"   )]
            public string access_token      { get; set; }
            [DataMember(Name = "expires_in"     )]
            public int expires_in           { get; set; }
            [DataMember(Name = "refresh_token"  )]
            public string refresh_token     { get; set; }
            [DataMember(Name = "scope"          )]
            public string scope             { get; set; }
            [DataMember(Name = "session_key"    )]
            public string session_key       { get; set; }
            [DataMember(Name = "session_secret" )]
            public string session_secret    { get; set; }
        }

        public static string do_update_access_token()
        {
            string result = string.Empty;

            string url = "http://openapi.baidu.com/oauth/2.0/token?" + 
                "&grant_type="       + grant_type +
                "&client_id="       + client_id +
                "&client_secret="   + client_secret;

            HttpWebRequest web_request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse web_response = web_request.GetResponse();
            Stream data_stream = web_response.GetResponseStream();

            DataContractJsonSerializer json_serializer = new DataContractJsonSerializer(typeof(OathResp));
            OathResp oath_resp = (OathResp)json_serializer.ReadObject(data_stream);

            data_stream.Close();
            web_response.Close();

            result = oath_resp.access_token;

            return result;
        }

        [DataContract]
        public class VopResp
        {
            [DataMember(Name = "err_no" )]
            public int err_no       { get; set; }
            [DataMember(Name = "err_msg")]
            public string err_msg   { get; set; }
            [DataMember(Name = "sn"     )]
            public string sn        { get; set; }
            [DataMember(Name = "result" )]
            public string[] result  { get; set; }
        }

        public static string exec(byte[] request_byte)
        {
            string result = string.Empty;

            string url = "http://vop.baidu.com/server_api?lan=zh&cuid=" + client_id + "&token=" + access_token
                + "&format=pcm&rate=16000&channel=1";

            HttpWebRequest web_request = (HttpWebRequest)WebRequest.Create(url);
            web_request.Method = "POST";
            web_request.ContentType = "audio/wav;rate=16000";
            web_request.ContentLength = request_byte.Length;
            web_request.ServicePoint.Expect100Continue = false;
            web_request.Timeout = 10000;

            log("Req:" + request_byte.Length + "B");

            try
            {
                Stream data_stream = web_request.GetRequestStream();
                data_stream.Write(request_byte, 0, request_byte.Length);
                data_stream.Close();

                WebResponse web_response = web_request.GetResponse();
                data_stream = web_response.GetResponseStream();

                DataContractJsonSerializer json_serializer = new DataContractJsonSerializer(typeof(VopResp));
                VopResp vop_resp = (VopResp)json_serializer.ReadObject(data_stream);

                result = vop_resp.result[0];

                log("Rpl:" + result);

                data_stream.Close();
                web_response.Close();
            }
            catch (Exception ex)
            {
                log(LOG_TYPE.LOG_ERROR, ex.Message);
            }

            return result;
        }
    }
}
