using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Teltonika.DataParser.Client.Handlers;
using Teltonika.DataParser.Client.Infrastructure;
using Teltonika.DataParser.Client.Infrastructure.Visitor;
using Teltonika.DataParser.Client.Models;

namespace Teltonika.DataParser.Client
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MarkersHandler _markersHandler;
        private IList<GpsData> _gpsData;

        public MainWindow()
        {
            InitializeComponent();
            var mapInitializer = new MapInitializer();
            _markersHandler = new MarkersHandler(mapInitializer);
            gmapHost.Child = mapInitializer.GetGMapControl();
        }


        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            var text = Regex.Replace(new TextRange(TextInput.Document.ContentStart, TextInput.Document.ContentEnd).Text,
                @"\t|\n|\r|\s", "");
            ExpandTreeViewButton.Visibility = Visibility.Visible;
            TextInput.Document.Blocks.Clear();
            TextInput.Document.Blocks.Add(new Paragraph(new Run(text.ToUpper())));

            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Insert data.");
                return;
            }

            try
            {
                var bytes = StringToBytes(text);
                var reader = new DataReader(bytes);
                var packetDecoder = new PacketDecoder();


                CompositeData data;
                if (tcpRadioButton.IsChecked != null && (bool) tcpRadioButton.IsChecked)
                    data = packetDecoder.DecodeTCPData(reader);
                else
                    data = packetDecoder.DecodeUdpData(reader);

                HandleGpsListView(data);

                HandleAvlTableDataGrid(data);

                treeView.ItemsSource = data.Data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Corrupted data inserted. Error: {ex.Message}");
            }
        }

        private void HandleGpsListView(CompositeData data)
        {
            var gpsDataVisitor = new GpsDataVisitor();
            data.Accept(gpsDataVisitor);
            _gpsData = gpsDataVisitor.GpsData;
            gpsElementsListView.ItemsSource = _gpsData;
            _markersHandler.LoadMarkers(_gpsData);
        }

        private void HandleAvlTableDataGrid(CompositeData data)
        {
            var listViewVisitor = new TransposedAvlDataVisitor();
            data.Accept(listViewVisitor);
            var dataTable = listViewVisitor.DataTable;
            AvlTableDataGrid.DataContext = dataTable.DefaultView;
        }

        private static byte[] StringToBytes(string data)
        {
            var array = new byte[data.Length / 2];

            var substring = 0;
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = byte.Parse(data.Substring(substring, 2), NumberStyles.AllowHexSpecifier);
                substring += 2;
            }

            return array;
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var data = e.NewValue as BaseData;

            if (data == null) return;

            if (treeView.SelectedItem is CompositeData composite)
            {
                var timestamp = composite.Data.First();

                foreach (GpsData gpsData in gpsElementsListView.Items)
                    if (gpsData.Timestamp == timestamp.Value)
                    {
                        gpsElementsListView.SelectedItem = gpsData;
                        _markersHandler.CenterMapToMarker(gpsData);
                    }
            }
            else
            {
                gpsElementsListView.SelectedItem = null;
            }

            var text = new TextRange(TextInput.Document.ContentStart, TextInput.Document.ContentEnd);
            text.ClearAllProperties();

            var startPosition = data.ArraySegment.Offset * 2;
            var endPosition = startPosition + data.ArraySegment.Count * 2;

            var start = text.Start.GetPositionAtOffset(startPosition);
            var end = text.Start.GetPositionAtOffset(endPosition);

            var range = new TextRange(start, end);
            range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
        }

        private void GpsElementsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (GpsData item in e.AddedItems) _markersHandler.UpdateSelectedMarker(item);
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer1.ScrollToHorizontalOffset(e.HorizontalOffset);
        }


        private void ShowHideMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowHideMapButton.Content.ToString() == "Hide Map")
            {
                GridColumn2.Visibility = Visibility.Collapsed;
                mainGridColumn2.Width = new GridLength(0);
                GridColumn3.Visibility = Visibility.Collapsed;
                mainGridColumn3.Width = new GridLength(0);

                ShowHideMapButton.Content = "Show Map";
            }
            else if (ShowHideMapButton.Content.ToString() == "Show Map")
            {
                GridColumn2.Visibility = Visibility.Visible;
                mainGridColumn2.Width = new GridLength(5);
                GridColumn3.Visibility = Visibility.Visible;
                mainGridColumn3.Width = new GridLength(300, GridUnitType.Auto);
                ShowHideMapButton.Content = "Hide Map";
            }
        }

        private void ExpandTreeView_Click(object sender, RoutedEventArgs e)
        {
            if (ExpandTreeViewButton.Content.ToString() == "Expand")
            {
                ExpandTreeViewButton.Content = "Collapse";
                foreach (var item in treeView.Items)
                {
                    var dObject = treeView.ItemContainerGenerator.ContainerFromItem(item);
                    ((TreeViewItem) dObject).ExpandSubtree();
                }
            }
            else if (ExpandTreeViewButton.Content.ToString() == "Collapse")
            {
                ExpandTreeViewButton.Content = "Expand";
                foreach (var item in treeView.Items)
                {
                    var dObject = treeView.ItemContainerGenerator.ContainerFromItem(item);
                    CollapseTreeViewItems((TreeViewItem) dObject);
                }
            }
        }

        private void CollapseTreeViewItems(TreeViewItem treeItem)
        {
            treeItem.IsExpanded = false;

            foreach (var item in treeItem.Items)
            {
                var dObject = treeView.ItemContainerGenerator.ContainerFromItem(item);

                if (dObject != null)
                {
                    ((TreeViewItem) dObject).IsExpanded = false;

                    if (((TreeViewItem) dObject).HasItems) CollapseTreeViewItems((TreeViewItem) dObject);
                }
            }
        }

        private void AvlTableDataGrid_Checked(object sender, RoutedEventArgs e)
        {
            if (ScrollViewer1 != null) ScrollViewer1.Visibility = Visibility.Hidden;

            if (treeView != null) treeView.Visibility = Visibility.Hidden;
            if (AvlTableDataGrid != null) AvlTableDataGrid.Visibility = Visibility.Visible;
        }


        private void TreeView_Checked(object sender, RoutedEventArgs e)
        {
            if (ScrollViewer1 != null) ScrollViewer1.Visibility = Visibility.Visible;

            if (treeView != null) treeView.Visibility = Visibility.Visible;
            if (AvlTableDataGrid != null) AvlTableDataGrid.Visibility = Visibility.Hidden;
        }
    }
}