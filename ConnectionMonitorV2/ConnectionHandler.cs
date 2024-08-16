using ConnectionMonitorV2.Model;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ConnectionMonitorV2
{
    public partial class ConnectionHandler
    {
        private static readonly List<string> _filterIpsList = ["127.0.0.1", "192.168.1.1", "::1", string.Empty];
        [GeneratedRegex(@"\d+\.\d+\.\d+\.\d+", RegexOptions.Compiled)]
        private static partial Regex IpRegex();
        private readonly Regex _ipAddressRegex = IpRegex();

        public async Task<ObservableCollection<ConnectionInfo>> GetConnectionInfo()
        {
            var conInfo = new ObservableCollection<ConnectionInfo>();
            var connections = GetActiveConnections();
            if (connections.Any())
            {

                Console.WriteLine("Active connections and their details:");

                foreach (var ip in connections)
                {
                    if (_filterIpsList.Contains(ip)) continue;

                    var ipInfo = await GetIpInfo(ip);
                    if (ipInfo == null) continue;

                    var isLocalHost = IsInvalidIp(ipInfo["ip"]?.ToString() ?? string.Empty);
                    if (isLocalHost) continue;

                    var connectionInfo = ParseToConnectionInfo(ipInfo);
                    if(connectionInfo != null) conInfo.Add(connectionInfo);
                }
            }

            return conInfo;

        }

        private bool IsInvalidIp(string ip)
        {
            return _filterIpsList.Contains(ip);
        }

        private List<string> GetActiveConnections()
        {
            List<string> remoteIps = new List<string>();

            ProcessStartInfo psi = new ProcessStartInfo("netstat", "-n")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains("ESTABLISHED"))
                    {
                        MatchCollection matches = _ipAddressRegex.Matches(line);
                        if (matches.Count > 1)
                        {
                            remoteIps.Add(matches[1].Value); // Second IP is the remote one
                        }
                    }
                }
            }

            return remoteIps;
        }

        private async Task<JObject?> GetIpInfo(string ip)
        {
            try
            {
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync($"https://ipinfo.io/{ip}/json"); // TODO: api key
                string json = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    //string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json);
                }
                Debug.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data for IP {ip}: {ex.Message}");
            }

            return null;
        }

        private ConnectionInfo? ParseToConnectionInfo(JObject ipInfo)
        {
            if (ipInfo == null) return null;

            var geoLocSplit = ipInfo["loc"]?.ToString().Split(",");

            if (geoLocSplit != null && geoLocSplit.Length == 2)
            {
                return new ConnectionInfo
                {
                    Ip = ipInfo["ip"]?.ToString() ?? "Unknown",
                    Country = ipInfo["country"]?.ToString() ?? "Unknown",
                    Company = ipInfo["org"]?.ToString() ?? "Unknown",
                    HostName = ipInfo["hostname"]?.ToString() ?? "Unknown",
                    City = ipInfo["city"]?.ToString() ?? "Unknown",
                    Region = ipInfo["region"]?.ToString() ?? "Unknown",
                    PostalCode = ipInfo["postal"]?.ToString() ?? "Unknown",
                    TimeZone = ipInfo["timezone"]?.ToString() ?? "Unknown",
                    Latitude = geoLocSplit[0] ?? "",
                    Longitude = geoLocSplit[1] ?? "",
                };
            }
            else
            {
                return null;
            }
        }
    }
}
