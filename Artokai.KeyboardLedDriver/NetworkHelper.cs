using System;
using System.Net.NetworkInformation;

namespace Artokai.KeyboardLedDriver
{
    public class NetworkHelper
    {
        public static bool IsVpnOn()
        {
            bool vpnState = false;
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface networkInterface in interfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        if (networkInterface.Description.Contains("Juniper Networks"))
                            return true;
                        if (networkInterface.Description.Contains("OpenVPN Connect"))
                            return true;
                        if (networkInterface.Description.Contains("PANGP Virtual Ethernet Adapter"))
                            return true;
                    }
                }
            }
            return vpnState;
        }
    }
}