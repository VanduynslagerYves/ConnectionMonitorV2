using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ConnectionMonitorV2.Model;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace ConnectionMonitorV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double ownLat = 0.0;
        private double ownLon = 0.0;

        private ObservableCollection<ConnectionInfo> _items;
        private readonly ConnectionHandler _connectionHandler;

        private ConnectionInfo? _homeInfo = null;

        public MainWindow()
        {
            InitializeComponent();
            _connectionHandler = new ConnectionHandler();
            _items = new ObservableCollection<ConnectionInfo>();
        }

        private async void InitializeView(object sender, RoutedEventArgs e)
        {
            SetHomeInfo();

            AddHomePinToMap();

            await GetData();
            GenerateDistinctRandomBrushes();
            SetInfoList();
            AddPinsToMap();
        }

        private async void RefreshData()
        {
            _items.Clear();
            mapControl.Markers.Clear();

            AddHomePinToMap();

            await GetData();
            GenerateDistinctRandomBrushes();
            SetInfoList();
            AddPinsToMap();
        }

        private void SetHomeInfo()
        {
            string filePath = "HomeCoords.txt"; //No, you can't know my homelocation

            // Read the first line
            var line = File.ReadLines(filePath).First();
            var lineSplit = line.Split(';', StringSplitOptions.TrimEntries);
            if (lineSplit != null && lineSplit.Length == 2 && !string.IsNullOrWhiteSpace(lineSplit[0]) && !string.IsNullOrWhiteSpace(lineSplit[1]))
            {
                var lat = lineSplit[0];
                var lon = lineSplit[1];

                // Parse to double and save to private fields that are used to center the map on the home coords
                double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out ownLat);
                double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out ownLon);

                _homeInfo = new ConnectionInfo
                {
                    City = string.Empty,
                    Company = "Home",
                    Country = string.Empty,
                    HostName = string.Empty,
                    Ip = string.Empty,
                    Latitude = lat,
                    Longitude = lon,
                    Region = string.Empty,
                    PostalCode = string.Empty,
                    TimeZone = string.Empty,
                };
            }

            
        }

        private void GenerateDistinctRandomBrushes()
        {
            double hueStep = 360.0 / _items.Count; // Divide the hue range evenly

            Random random = new Random();

            for (int i = 0; i < _items.Count; i++)
            {
                // Generate the hue for the current color with a random offset
                double hue = (i * hueStep + random.NextDouble() * hueStep) % 360;

                // Convert HSV to RGB and create the brush
                var color = HsvToRgb(hue, 0.8, 0.9); // Higher saturation and brightness for vivid colors
                _items[i].Color = color;
            }
        }

        // Function to convert HSV to RGB
        private static Color HsvToRgb(double hue, double saturation, double value) //TODO: Refactor so it returns a SolidColorBrush. now we need to convert it again in the ui
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromRgb(v, t, p);
            else if (hi == 1)
                return Color.FromRgb(q, v, p);
            else if (hi == 2)
                return Color.FromRgb(p, v, t);
            else if (hi == 3)
                return Color.FromRgb(p, q, v);
            else if (hi == 4)
                return Color.FromRgb(t, p, v);
            else
                return Color.FromRgb(v, p, q);
        }

        private void SetInfoList()
        {
            listControl.ItemsSource = new ListCollectionView(_items);
        }

        private async Task GetData()
        {
            _items = await _connectionHandler.GetConnectionInfo();
        }

        private void AddHomePinToMap()
        {
            if(_homeInfo != null) AddPinToMap(_homeInfo, Brushes.Green);
        }

        private void AddPinsToMap()
        {
            foreach (var item in _items)
            {
                AddPinToMap(item, new SolidColorBrush(item.Color));
            }
        }

        private void AddPinToMap(ConnectionInfo item, SolidColorBrush color)
        {
            double.TryParse(item.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);
            double.TryParse(item.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);
            PointLatLng point = new PointLatLng(lat, lon);

            GMapMarker marker = new GMapMarker(point)
            {
                // StackPanel to arrange the Ellipse and TextBlock horizontally
                Shape = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Stroke = color,
                            StrokeThickness = 1.5,
                            Fill = color,
                        },

                        // Add the TextBlock for visible text on the right side of the Ellipse
                        new TextBlock
                        {
                            Text = item.Company,
                            Foreground = Brushes.White,
                            Background = color,
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5, 0, 0, 0) // Add some space between the Ellipse and the text
                        }
                    },
                    ToolTip = $"{item.Company} - Lat: {point.Lat}, Lng: {point.Lng}, Ip: {item.Ip}"
                }
            };

            mapControl.Markers.Add(marker);
        }

        private void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up the map
            mapControl.MapProvider = GMapProviders.OpenStreetMap;
            mapControl.Position = new PointLatLng(ownLat, ownLon);
            mapControl.MinZoom = 3;
            mapControl.MaxZoom = 17;
            mapControl.Zoom = 3;
            mapControl.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            mapControl.CanDragMap = true;
            mapControl.DragButton = MouseButton.Left;
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}