using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Common
{
    public class NetworkUtils
    {
        public static List<String> GetLocalIP()
        {
            List<ServiceHost> serviceHostPool = new List<ServiceHost>();

            NetworkInterface[] interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            List<String> ipaddrs = new List<string>();

            ManagementClass mcNetworkAdapterConfig = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc_NetworkAdapterConfig = mcNetworkAdapterConfig.GetInstances();
            foreach (ManagementObject mo in moc_NetworkAdapterConfig)
            {
                string mServiceName = mo["ServiceName"] as string;

                //过滤非真实的网卡  
                if (!(bool)mo["IPEnabled"])
                { continue; }
                if (mServiceName.ToLower().Contains("vmnetadapter")
                 || mServiceName.ToLower().Contains("vmware")
                 || mServiceName.ToLower().Contains("ppoe")
                 || mServiceName.ToLower().Contains("bthpan")
                 || mServiceName.ToLower().Contains("tapvpn")
                 || mServiceName.ToLower().Contains("ndisip")
                 || mServiceName.ToLower().Contains("sinforvnic"))
                { continue; }

                string[] mIPAddress = mo["IPAddress"] as string[];

                if (mIPAddress != null)
                {

                    foreach (string ip in mIPAddress)
                    {
                        if (ip != "0.0.0.0")
                        {
                            ipaddrs.Add(ip);
                        }
                    }
                }
                mo.Dispose();
            }

            List<String> ipaddrs2 = new List<string>();
            foreach (NetworkInterface adapter in nics)
            {
                string hostIP = String.Empty;

                if (adapter.Name.Contains("vEthernet")) continue;

                //判断是否为以太网卡
                //Wireless80211         无线网卡;    Ppp     宽带连接;Ethernet              以太网卡
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    //获取单播地址集
                    UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                    foreach (UnicastIPAddressInformation ipadd in ipCollection)
                    {
                        if (ipadd.SuffixOrigin == SuffixOrigin.LinkLayerAddress) continue;
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                        //Max            MAX 位址
                        if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipaddrs2.Add(ipadd.Address.ToString());
                        }
                    }
                }
                else if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    //获取单播地址集
                    UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                    foreach (UnicastIPAddressInformation ipadd in ipCollection)
                    {
                        if (ipadd.SuffixOrigin == SuffixOrigin.LinkLayerAddress) continue;
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                        //Max            MAX 位址
                        if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipaddrs2.Add(ipadd.Address.ToString());
                        }
                    }
                }
            }

            ipaddrs.RemoveAll(p => !ipaddrs2.Contains(p));

            return ipaddrs;
        }
    }
}
