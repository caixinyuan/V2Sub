using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using V2Sub.Base;
using V2Sub.Model;
using V2Sub.Tools;

namespace V2Sub.Sub
{
    public class GetSub
    {

        public static Config GetSubN(int index = 0)
        {
            IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "./appsettings.json"))
            .Build();

            var SubString = GetWebRequest(configuration.GetSection("Url").Value); 
            SubString = Utils.Base64Decode(SubString);
            string[] arrData = SubString.Split(Environment.NewLine.ToCharArray());
            List<VmessItem> vmessItem = new List<VmessItem>();
            Config config = new Config();
            LoadConfig(ref config);
            foreach (string str in arrData)
            {
                vmessItem.Add(ImportFromClipboardConfig(Utils.Base64Decode(str.Substring(0,8))));
            }
            config.vmess = vmessItem;
            config.index = index;
            Utils.ToJsonFile(config,"");

            return config;
        }


        /// <summary>
        /// 从剪贴板导入URL
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static VmessItem ImportFromClipboardConfig(string clipboardData)
        {
            VmessItem vmessItem = new VmessItem();

            try
            {
                //载入配置文件 
                string result = clipboardData.TrimEx();// Utils.GetClipboardData();
                if (Utils.IsNullOrEmpty(result))
                {
                    return null;
                }

                if (result.StartsWith(Global.vmessProtocol))
                {
                    int indexSplit = result.IndexOf("?");
                    if (indexSplit > 0)
                    {
                        vmessItem = ResolveVmess4Kitsunebi(result);
                    }
                    else
                    {
                        vmessItem.configType = (int)EConfigType.Vmess;
                        result = result.Substring(Global.vmessProtocol.Length);
                        result = Utils.Base64Decode(result);

                        //转成Json
                        VmessQRCode vmessQRCode = Utils.FromJson<VmessQRCode>(result);
                        if (vmessQRCode == null)
                        {
                            return null;
                        }
                        vmessItem.security = Global.DefaultSecurity;
                        vmessItem.network = Global.DefaultNetwork;
                        vmessItem.headerType = Global.None;


                        vmessItem.configVersion = Utils.ToInt(vmessQRCode.v);
                        vmessItem.remarks = Utils.ToString(vmessQRCode.ps);
                        vmessItem.address = Utils.ToString(vmessQRCode.add);
                        vmessItem.port = Utils.ToInt(vmessQRCode.port);
                        vmessItem.id = Utils.ToString(vmessQRCode.id);
                        vmessItem.alterId = Utils.ToInt(vmessQRCode.aid);

                        if (!Utils.IsNullOrEmpty(vmessQRCode.net))
                        {
                            vmessItem.network = vmessQRCode.net;
                        }
                        if (!Utils.IsNullOrEmpty(vmessQRCode.type))
                        {
                            vmessItem.headerType = vmessQRCode.type;
                        }

                        vmessItem.requestHost = Utils.ToString(vmessQRCode.host);
                        vmessItem.path = Utils.ToString(vmessQRCode.path);
                        vmessItem.streamSecurity = Utils.ToString(vmessQRCode.tls);
                    }
                }
                else if (result.StartsWith(Global.ssProtocol))
                {
                    vmessItem.configType = (int)EConfigType.Shadowsocks;
                    result = result.Substring(Global.ssProtocol.Length);
                    //remark
                    int indexRemark = result.IndexOf("#");
                    if (indexRemark > 0)
                    {
                        try
                        {
                            vmessItem.remarks = WebUtility.UrlDecode(result.Substring(indexRemark + 1, result.Length - indexRemark - 1));
                        }
                        catch { }
                        result = result.Substring(0, indexRemark);
                    }
                    //part decode
                    int indexS = result.IndexOf("@");
                    if (indexS > 0)
                    {
                        result = Utils.Base64Decode(result.Substring(0, indexS)) + result.Substring(indexS, result.Length - indexS);
                    }
                    else
                    {
                        result = Utils.Base64Decode(result);
                    }

                    string[] arr1 = result.Split('@');
                    if (arr1.Length != 2)
                    {
                        return null;
                    }
                    string[] arr21 = arr1[0].Split(':');
                    //string[] arr22 = arr1[1].Split(':');
                    int indexPort = arr1[1].LastIndexOf(":");
                    if (arr21.Length != 2 || indexPort < 0)
                    {
                        return null;
                    }
                    vmessItem.address = arr1[1].Substring(0, indexPort);
                    vmessItem.port = Utils.ToInt(arr1[1].Substring(indexPort + 1, arr1[1].Length - (indexPort + 1)));
                    vmessItem.security = arr21[0];
                    vmessItem.id = arr21[1];
                }
                else if (result.StartsWith(Global.socksProtocol))
                {
                    vmessItem.configType = (int)EConfigType.Socks;
                    result = result.Substring(Global.socksProtocol.Length);
                    //remark
                    int indexRemark = result.IndexOf("#");
                    if (indexRemark > 0)
                    {
                        try
                        {
                            vmessItem.remarks = WebUtility.UrlDecode(result.Substring(indexRemark + 1, result.Length - indexRemark - 1));
                        }
                        catch { }
                        result = result.Substring(0, indexRemark);
                    }
                    //part decode
                    int indexS = result.IndexOf("@");
                    if (indexS > 0)
                    {
                    }
                    else
                    {
                        result = Utils.Base64Decode(result);
                    }

                    string[] arr1 = result.Split('@');
                    if (arr1.Length != 2)
                    {
                        return null;
                    }
                    string[] arr21 = arr1[0].Split(':');
                    //string[] arr22 = arr1[1].Split(':');
                    int indexPort = arr1[1].LastIndexOf(":");
                    if (arr21.Length != 2 || indexPort < 0)
                    {
                        return null;
                    }
                    vmessItem.address = arr1[1].Substring(0, indexPort);
                    vmessItem.port = Utils.ToInt(arr1[1].Substring(indexPort + 1, arr1[1].Length - (indexPort + 1)));
                    vmessItem.security = arr21[0];
                    vmessItem.id = arr21[1];
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return vmessItem;
        }


        private static VmessItem ResolveVmess4Kitsunebi(string result)
        {
            VmessItem vmessItem = new VmessItem
            {
                configType = (int)EConfigType.Vmess
            };
            result = result.Substring(Global.vmessProtocol.Length);
            int indexSplit = result.IndexOf("?");
            if (indexSplit > 0)
            {
                result = result.Substring(0, indexSplit);
            }
            result = Utils.Base64Decode(result);

            string[] arr1 = result.Split('@');
            if (arr1.Length != 2)
            {
                return null;
            }
            string[] arr21 = arr1[0].Split(':');
            string[] arr22 = arr1[1].Split(':');
            if (arr21.Length != 2 || arr21.Length != 2)
            {
                return null;
            }

            vmessItem.address = arr22[0];
            vmessItem.port = Utils.ToInt(arr22[1]);
            vmessItem.security = arr21[0];
            vmessItem.id = arr21[1];

            vmessItem.network = Global.DefaultNetwork;
            vmessItem.headerType = Global.None;
            vmessItem.remarks = "Alien";
            vmessItem.alterId = 0;

            return vmessItem;
        }

        /// <summary>
        /// 获取网络资源
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetWebRequest(string address)
        {
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create(address);
            var response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            var result = "";
            using (StreamReader sr = new StreamReader(stream))
            {
                result = sr.ReadToEnd();
            }
            return result;
        }

        /// <summary>
        /// 载入配置文件
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static int LoadConfig(ref Config config)
        {
            //载入配置文件 
            string result = Utils.LoadResource(Utils.GetPath("V2ray.json"));
            if (!Utils.IsNullOrEmpty(result))
            {
                //转成Json
                config = Utils.FromJson<Config>(result);
            }
            if (config == null)
            {
                config = new Config
                {
                    index = -1,
                    logEnabled = false,
                    loglevel = "warning",
                    vmess = new List<VmessItem>(),

                    //Mux
                    muxEnabled = true,

                    ////默认监听端口
                    //config.pacPort = 8888;

                    // 默认不开启统计
                    enableStatistics = false,

                    // 默认中等刷新率
                    statisticsFreshRate = (int)Global.StatisticsFreshRate.medium
                };
            }

            //本地监听
            if (config.inbound == null)
            {
                config.inbound = new List<InItem>();
                InItem inItem = new InItem
                {
                    protocol = Global.InboundSocks,
                    localPort = 10808,
                    udpEnabled = true,
                    sniffingEnabled = true
                };

                config.inbound.Add(inItem);

                //inItem = new InItem();
                //inItem.protocol = "http";
                //inItem.localPort = 1081;
                //inItem.udpEnabled = true;

                //config.inbound.Add(inItem);
            }
            else
            {
                //http协议不由core提供,只保留socks
                if (config.inbound.Count > 0)
                {
                    config.inbound[0].protocol = Global.InboundSocks;
                }
            }
            //路由规则
            if (Utils.IsNullOrEmpty(config.domainStrategy))
            {
                config.domainStrategy = "IPIfNonMatch";
            }
            if (Utils.IsNullOrEmpty(config.routingMode))
            {
                config.routingMode = "0";
            }
            if (config.useragent == null)
            {
                config.useragent = new List<string>();
            }
            if (config.userdirect == null)
            {
                config.userdirect = new List<string>();
            }
            if (config.userblock == null)
            {
                config.userblock = new List<string>();
            }
            //kcp
            if (config.kcpItem == null)
            {
                config.kcpItem = new KcpItem
                {
                    mtu = 1350,
                    tti = 50,
                    uplinkCapacity = 12,
                    downlinkCapacity = 100,
                    readBufferSize = 2,
                    writeBufferSize = 2,
                    congestion = false
                };
            }
            if (config.uiItem == null)
            {
                config.uiItem = new UIItem();
            }
            //// 如果是用户升级，首次会有端口号为0的情况，不可用，这里处理
            //if (config.pacPort == 0)
            //{
            //    config.pacPort = 8888;
            //}
            if (Utils.IsNullOrEmpty(config.speedTestUrl))
            {
                config.speedTestUrl = Global.SpeedTestUrl;
            }
            if (Utils.IsNullOrEmpty(config.speedPingTestUrl))
            {
                config.speedPingTestUrl = Global.SpeedPingTestUrl;
            }
            if (Utils.IsNullOrEmpty(config.urlGFWList))
            {
                config.urlGFWList = Global.GFWLIST_URL;
            }
            //if (Utils.IsNullOrEmpty(config.remoteDNS))
            //{
            //    config.remoteDNS = "1.1.1.1";
            //}

            if (config.subItem == null)
            {
                config.subItem = new List<SubItem>();
            }
            if (config.userPacRule == null)
            {
                config.userPacRule = new List<string>();
            }
            return 0;
        }

    }
}
