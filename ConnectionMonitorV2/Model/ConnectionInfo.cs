using System.Windows.Media;

namespace ConnectionMonitorV2.Model
{
    public class ConnectionInfo
    {
        public required string Ip { get; set; }
        public required string Country { get; set; }
        public required string Company { get; set; }
        public required string HostName { get; set; }
        public required string City { get; set; }
        public required string Region { get; set; }
        public required string PostalCode { get; set; }
        public required string TimeZone { get; set; }
        public required string Longitude { get; set; }
        public required string Latitude { get; set; }
        public Color Color { get; set; }
        public string Address => $"{City} - {Region} {Country}";
    }
}
