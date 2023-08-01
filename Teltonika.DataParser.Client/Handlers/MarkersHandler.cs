using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Teltonika.DataParser.Client.Infrastructure;
using Teltonika.DataParser.Client.Models;

namespace Teltonika.DataParser.Client.Handlers
{
    public class MarkersHandler
    {
        private readonly GMapControl _gMap;
        private GpsData _previouslySelectedGpsData;

        public MarkersHandler(MapInitializer mapInitializer)
        {
            _gMap = mapInitializer.GetGMapControl();
        }

        public void UpdateSelectedMarker(GpsData gpsData)
        {
            if (_previouslySelectedGpsData != null)
                ChangeMarkerColor(_previouslySelectedGpsData, GMarkerGoogleType.red_small);

            ChangeMarkerColor(gpsData, GMarkerGoogleType.green_small);

            _previouslySelectedGpsData = gpsData;
        }

        private void ChangeMarkerColor(GpsData data, GMarkerGoogleType markerType)
        {
            RemoveMarker(data.Timestamp);
            var marker = new GMarkerGoogle(data.Coordinates, markerType)
            {
                ToolTipText = $"{data.Timestamp}",
                Tag = $"{data.Timestamp}"
            };
            AddMarker(marker);
        }

        public void LoadMarkers(IList<GpsData> gpsData)
        {
            _gMap.Overlays.Clear();

            if ((gpsData?.Count ?? 0) == 0) return;

            var markers = new GMapOverlay("markers");
            IList<PointLatLng> points = new List<PointLatLng>();
            foreach (var selector in gpsData)
            {
                selector.Coordinates = GetPointLatLng(selector.Latitude, selector.Longitude);
                points.Add(selector.Coordinates);
                GMapMarker marker = new GMarkerGoogle(selector.Coordinates,
                    GMarkerGoogleType.red_small)
                {
                    ToolTipText = $"{selector.Timestamp}",
                    Tag = $"{selector.Timestamp}"
                };

                markers.Markers.Add(marker);
            }

            _gMap.Zoom = 19;
            _gMap.Position = GetPointLatLng($"{points.Last().Lat}", $"{points.Last().Lng}");

            _gMap.Overlays.Add(markers);

            DrawPolygon(points);
        }

        public void CenterMapToMarker(GpsData gpsData)
        {
            _gMap.Position = gpsData.Coordinates;
        }

        private PointLatLng GetPointLatLng(string lat, string lng)
        {
            var latitude = double.Parse(lat);
            var longitude = double.Parse(lng);
            return new PointLatLng(latitude, longitude);
        }

        private void AddMarker(GMapMarker marker)
        {
            var overlay = GetOverlayById("markers");
            overlay.Markers.Add(marker);
        }

        private void RemoveMarker(string timestamp)
        {
            var overlay = GetOverlayById("markers");
            var marker = overlay.Markers.First(t => t.Tag.ToString() == timestamp);
            overlay.Markers.Remove(marker);
        }

        private void DrawPolygon(IList<PointLatLng> points)
        {
            var polyOverlay = new GMapOverlay("overlay");
            var polygon = new GMapRoute(points, "mypolygon")
            {
                Stroke = new Pen(Color.Red, 1)
            };
            polyOverlay.Routes.Add(polygon);
            _gMap.Overlays.Add(polyOverlay);

            _gMap.Zoom++;
            _gMap.Zoom--;
        }

        private GMapOverlay GetOverlayById(string id)
        {
            return _gMap.Overlays.Where(m => m.Id == id).FirstOrDefault();
        }
    }
}