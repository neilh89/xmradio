using System;
using System.Collections;
using System.Net;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace XMRadio
{
    class XM
    {
        private CookieContainer cred = new CookieContainer();
        private string user;
        private string password;

        public bool Login(string username, string pass)
        {
            try
            {
                string url = "http://www.xmradio.com/player/login/xmlogin.action";
                user = username;
                password = pass;
                string html = ExecuteURL(url, ref cred, true);
                return true;
            }
            catch (System.FormatException)
            {
                return false;
            }
        }
        public void Disconnect()
        {
            ExecuteURL("http://www.xmradio.com/player/login/xmrologout.action", ref cred, false);
            cred = new CookieContainer();
        }
        public string GetAlbumArt(string artist, string track, string lastFMKey)
        {
            string url = "http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key=" + lastFMKey + "&artist=" + artist + "&track=" + track;
            url = url.Replace(" ", "%20");
            string tempXMLString = "";
            try
            {
                CookieContainer temp = new CookieContainer();
                tempXMLString = ExecuteURL(url, ref temp, false);
                temp = null;
                XmlDocument XML = new XmlDocument();
                XML.LoadXml(tempXMLString);
                XmlNodeList nodes = XML.SelectNodes("lfm/track/album/image");
                if (nodes[0]["error code=\"6\""] == null)
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (nodes[i].Attributes["size"].Value == "medium")
                        {
                            return nodes[i].InnerText;
                        }
                    }
                }
                return "false";
            }
            catch (Exception)
            {
                return "false";
            }
        }
        public ArrayList GetAllChanInfo(ref ArrayList xmArray)
        {
            XmlDocument allChanXML = new XmlDocument();
            XmlTextReader reader;
            string tempXMLString = "";
            try
            {
                tempXMLString = ExecuteURL("http://www.xmradio.com/padData/pad_data_servlet.jsp?all_channels=true", ref cred, false);
                allChanXML.LoadXml(tempXMLString);
                reader = new XmlTextReader("http://www.xmradio.com/padData/pad_data_servlet.jsp?all_channels=true");
                while (reader.Read())
                {
                    XmlNodeType nType = reader.NodeType;

                   // System.Windows.MessageBox.Show(reader.Name.ToString());
                }
            }
            catch (Exception)
            {
                //nothing
            }
            XmlNodeList nodes = allChanXML.SelectNodes("paddata/event");
            for (int i = 0; i < nodes.Count; i++)
            {
                xmArray.Add(new XMChan(Convert.ToInt32(nodes[i]["channelnumber"].InnerText), nodes[i]["channelname"].InnerText,
                    nodes[i]["artist"].InnerText, nodes[i]["songtitle"].InnerText, nodes[i]["album"].InnerText));
            }
            allChanXML = null;
            nodes = null;

            xmArray.Sort();

            return xmArray;
        }
        public string GetStreamURL(int chanNum)
        {
            string html = ExecuteURL("http://www.xmradio.com/player/listen/play.action?channelKey=" + chanNum.ToString() + "&newBitRate=high", ref cred, false);
            string[] temp = GetStringInBetween("<PARAM NAME=\"FileName\" VALUE=\"", "\">", html, false, false);
            return temp[0];
        }
        private string ExecuteURL(string URL, ref CookieContainer CookieJar, bool Post)
        {
            try
            {
                string strOutput;
                HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(URL);
                objRequest.CookieContainer = CookieJar;


                if (Post)
                {
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    string postData = "userName=" + user + "&password=" + password + "&playerToLaunch=xm&encryptPassword=true&x=0&y=0";
                    byte[] data = encoding.GetBytes(postData);

                    objRequest.Method = "POST";
                    objRequest.ContentType = "application/x-www-form-urlencoded";
                    objRequest.ContentLength = data.Length;
                    Stream sendStream = objRequest.GetRequestStream();
                    sendStream.Write(data, 0, data.Length);
                    sendStream.Close();
                }

                HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
                Stream objRespStream = objResponse.GetResponseStream();
                StreamReader objReader = new StreamReader(objRespStream, Encoding.ASCII);
                strOutput = objReader.ReadToEnd();
                objReader.Close();
                return strOutput;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to execute URL (" + URL + ")", ex);
            }
        }

        public static string[] GetStringInBetween(string strBegin, string strEnd, string strSource, bool includeBegin, bool includeEnd)
        {

            string[] result = { "", "" };
            int iIndexOfBegin = strSource.IndexOf(strBegin);
            if (iIndexOfBegin != -1)
            {
                if (includeBegin)
                    iIndexOfBegin -= strBegin.Length;
                strSource = strSource.Substring(iIndexOfBegin
                    + strBegin.Length);
                int iEnd = strSource.IndexOf(strEnd);
                if (iEnd != -1)
                {
                    if (includeEnd)
                        iEnd += strEnd.Length;
                    result[0] = strSource.Substring(0, iEnd);
                    if (iEnd + strEnd.Length < strSource.Length)
                        result[1] = strSource.Substring(iEnd
                            + strEnd.Length);
                }
            }
            else
                result[1] = strSource;
            return result;
        }
    }
}