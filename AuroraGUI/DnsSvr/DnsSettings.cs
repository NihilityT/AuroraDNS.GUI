using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ARSoft.Tools.Net;
using AuroraGUI.Tools;
using MojoUnity;

namespace AuroraGUI.DnsSvr
{
    class DomainMapper
    {
        public static Dictionary<DomainName, string> map = new Dictionary<DomainName, string>();
        public static List<KeyValuePair<Regex, string>> regexList = new List<KeyValuePair<Regex, string>>();

        public void Add(string from, string to)
        {
            from = from.Trim();
            if (string.IsNullOrWhiteSpace(from))
            {
                return;
            }
            to = to.Trim();

            if (from.StartsWith("*."))
            {
                from = $"/^(.+\\.)?{from.Substring(2).Replace(".", "\\.")}\\.?$";
            }
            if (from.StartsWith("/"))
            {
                var regex = new Regex(from.Substring(1),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
                regexList.Add(new KeyValuePair<Regex, string>(regex, to));
                return;
            }

            var domain = DomainName.Parse(from);
            if (!map.ContainsKey(domain))
            {
                map.Add(domain, to);
            }
        }

        public string getMappedAddress(DomainName domain)
        {
            string ret = null;
            var domainName = domain.ToString();

            if (map.ContainsKey(domain))
            {
                ret = map[domain];
            }
            else
            {
                foreach (var item in regexList)
                {
                    if (item.Key.IsMatch(domainName))
                    {
                        ret = item.Value;
                        break;
                    }
                }
            }

            if (ret == "")
            {
                ret = domainName;
            }
            return ret;
        }

        public void Clear()
        {
            map.Clear();
            regexList.Clear();
        }
    }

    class DnsSettings
    {
        public static List<DomainName> BlackList;
        public static List<DomainName> ChinaList;
        public static DomainMapper LocalDomainMapper = new DomainMapper();
        public static DomainMapper WebDomainMapper = new DomainMapper();

        public static string HttpsDnsUrl = "https://dns.cloudflare.com/dns-query";
        public static string SecondHttpsDnsUrl = "https://1.0.0.1/dns-query";
        public static IPAddress ListenIp = IPAddress.Loopback;
        public static int ListenPort = 53;
        public static int SecondDnsPort = 53;
        public static IPAddress EDnsIp = IPAddress.Any;
        public static IPAddress SecondDnsIp = IPAddress.Parse("1.0.0.1");
        public static IPAddress StartupDoHIp = IPAddress.Parse("1.0.0.1");
        public static bool EDnsCustomize = false;
        public static bool ProxyEnable  = false;
        public static bool DebugLog = false;
        public static bool BlackListEnable  = false;
        public static bool WhiteListEnable  = false;
        public static bool ChinaListEnable = false;
        public static bool DnsMsgEnable = false;
        public static bool DnsCacheEnable = false;
        public static bool Http2Enable = false;
        public static bool AutoCleanLogEnable = false;
        public static bool Ipv6Disable = false;
        public static bool Ipv4Disable = false;
        public static bool StartupOverDoH = false;
        public static bool AllowSelfSignedCert = false;
        public static bool AllowAutoRedirect = true;
        public static bool HTTPStatusNotify = false;
        public static bool TtlRewrite = false;
        public static int TtlMinTime = 300;
        public static WebProxy WProxy = new WebProxy("127.0.0.1:1080");

        public static string getMappedAddress(DomainName domain)
        {
            string ret;
            if ((ret = LocalDomainMapper.getMappedAddress(domain)) != null) { }
            else if ((ret = WebDomainMapper.getMappedAddress(domain)) != null) { }
            return ret;
        }

        public static void ReadConfig(string path)
        {
            string configStr = File.ReadAllText(path);
            JsonValue configJson = Json.Parse(configStr);

            if (configStr.Contains("\"SecondDns\""))
                SecondDnsIp = IPAddress.Parse(configJson.AsObjectGetString("SecondDns"));
            if (configStr.Contains("\"SecondHttpsDns\""))
                SecondHttpsDnsUrl = configJson.AsObjectGetString("SecondHttpsDns");
            if (configStr.Contains("\"EnableDnsMessage\""))
                DnsMsgEnable = configJson.AsObjectGetBool("EnableDnsMessage");
            if (configStr.Contains("\"EnableDnsCache\""))
                DnsCacheEnable = configJson.AsObjectGetBool("EnableDnsCache");
            if (configStr.Contains("\"EnableHttp2\""))
                Http2Enable = configJson.AsObjectGetBool("EnableHttp2");
            if (configStr.Contains("\"EnableAutoCleanLog\""))
                AutoCleanLogEnable = configJson.AsObjectGetBool("EnableAutoCleanLog");
            if (configStr.Contains("\"Port\""))
                ListenPort = configJson.AsObjectGetInt("Port", 53);
            if (configStr.Contains("\"SecondDnsPort\""))
                SecondDnsPort = configJson.AsObjectGetInt("SecondDnsPort", 53);
            if (configStr.Contains("\"ChinaList\""))
                ChinaListEnable = configJson.AsObjectGetBool("ChinaList");
            if (configStr.Contains("\"StartupOverDoH\""))
                StartupOverDoH = configJson.AsObjectGetBool("StartupOverDoH");
            if (configStr.Contains("\"StartupDoHIp\""))
                StartupDoHIp = IPAddress.Parse(configJson.AsObjectGetString("StartupDoHIp"));
            if (configStr.Contains("\"AllowSelfSignedCert\""))
                AllowSelfSignedCert = configJson.AsObjectGetBool("AllowSelfSignedCert");
            if (configStr.Contains("\"AllowAutoRedirect\""))
                AllowAutoRedirect = configJson.AsObjectGetBool("AllowAutoRedirect");
            if (configStr.Contains("\"HTTPStatusNotify\""))
                HTTPStatusNotify = configJson.AsObjectGetBool("HTTPStatusNotify");

            if (configStr.Contains("\"Ipv6Disable\""))
                Ipv6Disable = configJson.AsObjectGetBool("Ipv6Disable");
            if (configStr.Contains("\"Ipv4Disable\""))
                Ipv4Disable = configJson.AsObjectGetBool("Ipv4Disable");

            if (configStr.Contains("\"TTLRewrite\""))
                TtlRewrite = configJson.AsObjectGetBool("TTLRewrite");
            if (configStr.Contains("\"TTLMinTime\""))
                TtlMinTime = configJson.AsObjectGetInt("TTLMinTime");

            ListenIp = IPAddress.Parse(configJson.AsObjectGetString("Listen"));
            BlackListEnable = configJson.AsObjectGetBool("BlackList");
            WhiteListEnable = configJson.AsObjectGetBool("RewriteList");
            ProxyEnable = configJson.AsObjectGetBool("ProxyEnable");
            EDnsCustomize = configJson.AsObjectGetBool("EDnsCustomize");
            DebugLog = configJson.AsObjectGetBool("DebugLog");
            HttpsDnsUrl = configJson.AsObjectGetString("HttpsDns").Trim();

            if (EDnsCustomize)
                EDnsIp = IPAddress.Parse(configJson.AsObjectGetString("EDnsClientIp"));
            if (ProxyEnable)
                WProxy = new WebProxy(configJson.AsObjectGetString("Proxy"));
        }

        public static void ReadBlackList(string path = "black.list")
        {
            string[] blackListStrs = File.ReadAllLines(path);
            BlackList = Array.ConvertAll(blackListStrs, DomainName.Parse).ToList();
        }

        public static void ReadChinaList(string path = "china.list")
        {
            string[] chinaListStrs = File.ReadAllLines(path);
            ChinaList = Array.ConvertAll(chinaListStrs, DomainName.Parse).ToList();
        }

        static void AddList(string[] list,  DomainMapper domainMapper)
        {
            foreach (var itemStr in list)
            {
                try
                {
                    var str = Regex.Replace(itemStr.Trim(), "#.*", "");
                    if (string.IsNullOrWhiteSpace(str))
                    {
                        return;
                    }
                    var strings = Regex.Split(str, "\\s+|,");
                    var added = strings.Length > 1;
                    for (int i = 1; i < strings.Length; ++i)
                    {
                        domainMapper.Add(strings[i], strings[0]);
                    }
                    if (!added)
                    {
                        domainMapper.Add(strings[0], "");
                    }
                }
                catch (Exception e)
                {
                    MyTools.BackgroundLog(e.ToString());
                }
            }
        }

        public static void ReadWhiteList(string path = "white.list")
        {
            string[] whiteListStrs = File.ReadAllLines(path);
            AddList(whiteListStrs, LocalDomainMapper);
        }

        public static void ReadWhiteListWeb(string webUrl)
        {
            try
            {
                string[] whiteListStrs = new WebClient().DownloadString(webUrl).Split('\n');
                AddList(whiteListStrs, WebDomainMapper);
            }
            catch (Exception e)
            {
                MyTools.BackgroundLog(e.ToString());
            }
        }

        public static void ReadWhiteListSubscribe(string path)
        {
            string[] whiteListSubStrs = File.ReadAllLines(path);
            foreach (var item in whiteListSubStrs)
            {
                if (item.ToLower().Contains("http://") || item.ToLower().Contains("https://"))
                    ReadWhiteListWeb(item);
            }
        }
    }

    class UrlSettings
    {
        public static string GeoIpApi = "https://api.ip.sb/geoip/";
        public static string WhatMyIpApi = "https://api.ipify.org/";
        public static string MDnsList = "http://gh.mili.one/github.com/mili-tan/AuroraDNS.GUI/blob/master/List/DNS.list";
        public static string MDohList = "http://gh.mili.one/github.com/mili-tan/AuroraDNS.GUI/blob/master/List/DoH.list";

        public static void ReadConfig(string path)
        {
            string configStr = File.ReadAllText(path);
            JsonValue configJson = Json.Parse(configStr);

            if (configStr.Contains("\"GeoIPAPI\""))
                GeoIpApi = configJson.AsObjectGetString("GeoIPAPI");
            if (configStr.Contains("\"WhatMyIPAPI\""))
                WhatMyIpApi = configJson.AsObjectGetString("WhatMyIPAPI");
            if (configStr.Contains("\"DNSList\""))
                MDnsList = configJson.AsObjectGetString("DNSList");
            if (configStr.Contains("\"DoHList\""))
                MDohList = configJson.AsObjectGetString("DoHList");
        }
    }
}
