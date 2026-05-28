using System;
using System.Net.Sockets;

namespace ECQ_Soft.Utils
{
    public static class NetworkStatusUtil
    {
        public static bool IsInternetAvailable()
        {
            return CanConnect(AppConstant.Network.DefaultHost, AppConstant.Network.DefaultPort, AppConstant.Network.DefaultTimeoutMs);
        }

        public static bool CanConnect(string host, int port, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0)
            {
                return false;
            }

            try
            {
                using (var client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(timeoutMs);
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                    return client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
