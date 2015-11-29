using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using xSocks.Model;
using xSocks.Properties;

namespace xSocks.Controller
{
    public class GFWListUpdater
    {
        private const string GfwlistUrl = "https://autoproxy-gfwlist.googlecode.com/svn/trunk/gfwlist.txt";

        private static readonly string PacFile = PACServer.PacFile;

        private static readonly string UserRuleFile = PACServer.UserRuleFile;

        public event EventHandler<ResultEventArgs> UpdateCompleted;

        public event ErrorEventHandler Error;

        public class ResultEventArgs : EventArgs
        {
            public bool Success { get; set; }

            public ResultEventArgs(bool success)
            {
                Success = success;
            }
        }

        private void HttpDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                List<string> lines = ParseResult(e.Result);
                if (File.Exists(UserRuleFile))
                {
                    string local = File.ReadAllText(UserRuleFile, Encoding.UTF8);
                    string[] rules = local.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string rule in rules)
                    {
                        if (rule.StartsWith("!") || rule.StartsWith("["))
                            continue;
                        lines.Add(rule);
                    }
                }
                string abpContent = Utils.UnGzip(Resources.abp_js);
                abpContent = abpContent.Replace("__RULES__", SimpleJson.SimpleJson.SerializeObject(lines));
                if (File.Exists(PacFile))
                {
                    string original = File.ReadAllText(PacFile, Encoding.UTF8);
                    if (original == abpContent)
                    {
                        if (UpdateCompleted != null) UpdateCompleted(this, new ResultEventArgs(false));
                        return;
                    }
                }
                File.WriteAllText(PacFile, abpContent, Encoding.UTF8);
                if (UpdateCompleted != null)
                {
                    UpdateCompleted(this, new ResultEventArgs(true));
                }
            }
            catch (Exception ex)
            {
                if (Error != null)
                {
                    Error(this, new ErrorEventArgs(ex));
                }
            }
        }

        public void UpdatePacFromGfwList(Configuration config)
        {
            WebClient http = new WebClient();
            http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), config.HttpProxyPort);
            http.DownloadStringCompleted += HttpDownloadStringCompleted;
            http.DownloadStringAsync(new Uri(GfwlistUrl));
        }

        public List<string> ParseResult(string response)
        {
            byte[] bytes = Convert.FromBase64String(response);
            string content = Encoding.ASCII.GetString(bytes);
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> validLines = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (line.StartsWith("!") || line.StartsWith("["))
                    continue;
                validLines.Add(line);
            }
            return validLines;
        }
    }

}
