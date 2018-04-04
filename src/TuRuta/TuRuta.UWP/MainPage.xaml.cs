﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using TuRuta.Client;
using TuRuta.Client.Routes;
using Windows.UI.Xaml.Controls.Maps;

namespace TuRuta.UWP
{
    public sealed partial class MainPage : Page
    {
        private RoutesClient RoutesClient { get; } = TuRutaClient.RoutesClientAndroid;

        public MainPage()
        {
            this.InitializeComponent();

            var locationTask = Location();
        }

        private async Task Location()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if(accessStatus == GeolocationAccessStatus.Allowed)
            {
                var locator = new Geolocator();
                var currentPosition = await locator.GetGeopositionAsync();
                Map.Center = currentPosition.Coordinate.Point;
                Map.ZoomLevel = 12;
            }
            else
            {
                var dialog = new MessageDialog("Necesitamos tu ubicacion para centrar el mapa, pero lo puedes centrar tu");
                await dialog.ShowAsync();
            }
        }

        private async void SuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if(args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && sender.Text.Length > 2)
            {
                var suggestions = await RoutesClient.Find(sender.Text);
                SuggestBox.ItemsSource = suggestions;
                return;
            }

            SuggestBox.ItemsSource = null;
        }

        private async void SuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if(Map.Layers.Count != 0)
            {
                Map.Layers.Remove(Map.Layers.Where(layer => layer.ZIndex == 1).First());
            }

            var routeName = args.SelectedItem as string;
            var route = await RoutesClient.Get(routeName);
            var markers = route.Stops.Select(stop => new MapIcon()
            {
                Location = new Geopoint(new BasicGeoposition
                {
                    Latitude = stop.Location.Latitude,
                    Longitude = stop.Location.Longitude
                }),
                Title = stop.Name,
                Tag = stop.Id,
                NormalizedAnchorPoint = new Point(0.5, 1.0),
                ZIndex = 0
            } as MapElement);

            Map.Layers.Add(new MapElementsLayer
            {
                ZIndex = 1,
                MapElements = markers.ToList()
            });

            var middlePoint = route.Stops[route.Stops.Count / 2];
            
            Map.Center = new Geopoint(new BasicGeoposition
            {
                Latitude = middlePoint.Location.Latitude,
                Longitude = middlePoint.Location.Longitude
            });
            Map.ZoomLevel = 12;
        }
    }
}
