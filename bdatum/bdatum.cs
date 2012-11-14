﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Collections.Specialized;

namespace bdatum
{

    /*
     *  Not in correct way yet, just for DRY
     */

    public class b_http
    {

        private static string url = "https://api.b-datum.com/";

        /*  TODO: Make parameters optional
         * 
         *  Make authentication optional 
         */

        public static string GET ( string path, string auth_key )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "GET";

            string authotization_header =  ("Authorization: Basic " + auth_key );
            request.Headers.Add( authotization_header );

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;            
        }

        public static string POST ( string path, string post_data )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "POST";

            byte[] byte_post_data = Encoding.UTF8.GetBytes( post_data );

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byte_post_data.Length;

            Stream data_stream = request.GetRequestStream();
            data_stream.Write( byte_post_data, 0, byte_post_data.Length );
            data_stream.Close();

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            data_stream = response.GetResponseStream();

            StreamReader response_stream = new StreamReader( data_stream );
            string responseFromServer =  response_stream.ReadToEnd();

            return responseFromServer;

        }

        public static string DELETE(string path, string auth_key)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "DELETE";

            string authotization_header = ("Authorization: Basic " + auth_key);
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;
        }

        private static string _GetMd5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /*
         *   Hand made post, the dot net client wasn´t working ok so 
         *   I wrote it byhand to help debug and fine adjust
         */ 

        public static long UPLOAD(string path, string auth_key, string file)
        {

            NameValueCollection nvc = new NameValueCollection();

            String file_hash = _GetMd5HashFromFile(file).ToUpper();          
            
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url + "/storage/"  + path );
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, "value", file, "multipart/form-data");
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                // clean webbrowser                

                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                var content = reader2.ReadToEnd();

            }
            catch (Exception ex)
            {

                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            return 0;
        }
    }

    public class b_datum
    {
        public string api_key { get; set; }
        public string partner_key { get; set; }
        public string organization_id { get; set; }
        public string user_name { get; set; }

        public b_node add_node()
        {
            return null;
        }

        public b_node node()
        {
            return null;
        }

        /*
         *  TODO:
         * 
         *  The constructor of the node should be here.
         *  The node should be not directly allocated.
         *  Finish the code organization. 
         * 
         */

    }

    public class b_node
    {
        public string name { get; set; }
        public string id { get; set; }
        public string organization { get; set; }
        public string partner_key { get; set; }
        public string activation_key { get; set; }
        public string node_key { get; set; }

        private string _auth_key()
        {
            string to_encode = node_key + ":" + partner_key;
            byte[] bytes_to_encode = System.Text.ASCIIEncoding.ASCII.GetBytes(to_encode);
            return System.Convert.ToBase64String(bytes_to_encode);
        }

        public string activate()
        {
            string activate_parameters = "activation_key=" + activation_key + "&partner_key=" + partner_key;
            return b_http.POST("node/activate", activate_parameters);
        }

        public string list ()
        {
            return b_http.GET("storage", _auth_key() );
        }

        public string info ( string path, int version )
        {
            return "TODO";
        }

        public string download_file( string path )
        {
            return b_http.GET("storage/" + path, _auth_key());
        }

        public string download_file( string path, int version )
        {
            return b_http.GET("storage/" + path + "?version=" + version.ToString(), _auth_key());
        }
    
        public string delete( string path )
        {
            return b_http.DELETE("storage/" + path, _auth_key());
        }

        public string upload( string path )
        {
            return "TODO";
        }

    }

}
