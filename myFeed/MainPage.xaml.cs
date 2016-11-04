using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class MainPage : Page
    {
        private bool _manipulationsEnabled = true;

        public MainPage()
        {
            switch (App.config.RequestedTheme)
            {
                case 1: this.RequestedTheme = ElementTheme.Light; break;
                case 2: this.RequestedTheme = ElementTheme.Dark; break;
                default: break;
            }

            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(300, 300));
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                try
                {
                    var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    SolidColorBrush statuscolor = NavBackground.Background as SolidColorBrush;
                    statusBar.BackgroundColor = statuscolor.Color;
                    if (App.config.RequestedTheme == 1) { statusBar.ForegroundColor = Colors.Black; }
                    else if (App.config.RequestedTheme == 2) { statusBar.ForegroundColor = Colors.White; }
                    statusBar.BackgroundOpacity = 1;
                    
                }
                catch
                {

                }
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (MainFrame.CanGoBack && App.CanNavigate)
                {
                    MainFrame.GoBack();
                    a.Handled = true;
                }                   
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackgroundTaskManager.RegisterNotifier(App.config.CheckTime);
            if (string.IsNullOrEmpty(e.Parameter.ToString()))
            {
                if (e.Parameter.ToString() == "N") return;
                MainFrame.Navigate(typeof(PFeed));
            }
            else
            {
                if (e.Parameter.ToString() == "N") return;
                FromSecondaryTile(e.Parameter);
            }
        }

        public void FindNotification(string id)
        {
            MainLoading.IsActive = true;
            MainFrame.Navigate(typeof(PFeed), id);
            MainLoading.IsActive = false;
        }

        public void FromSecondaryTile(object e)
        {
            if (e.ToString() == "fav") MainFrame.Navigate(typeof(PFavorites));
            else MainFrame.Navigate(typeof(PFavorites), e.ToString());

            if (MainFrame.BackStackDepth > 0)
                if (MainFrame.BackStack[MainFrame.BackStackDepth - 1].SourcePageType == typeof(PFavorites))
                    MainFrame.BackStack.RemoveAt(MainFrame.BackStackDepth - 1);
        }

        private async void HamburgerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (HamburgerListBox.SelectedIndex)
            {
                case 0:
                    return;
                case 1:
                    MainFrame.Navigate(typeof(PFeed));
                    break;
                case 2:
                    MainFrame.Navigate(typeof(PFavorites));
                    break;
                case 3:
                    MainFrame.Navigate(typeof(PFeedList));
                    break;
                case 4:
                    MainFrame.Navigate(typeof(Search));
                    break;
                case 5:
                    MainFrame.Navigate(typeof(PSettings));
                    break;
            }

            await Task.Delay(150);
            ClosePane();
        }
        
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            if (SplitviewLayer.Width > 0) ClosePane(); else OpenPane();
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            HamburgerListBox.SelectedIndex = App.ChosenIndex;
        }

        private void SplitviewLayer_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            double dx = e.Delta.Translation.X;
            if (SplitviewLayer.Width + dx < 220 && SplitviewLayer.Width + dx > 0)
            {
                SplitviewLayer.Width += dx;
            }
        }

        private async void SplitviewLayer_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            HamburgerListBox.IsEnabled = false;
            double v = e.Velocities.Linear.X;
            if (v < 0)
            {
                ClosePane();
                await Task.Delay(150);
            }
            else if (v >= 0)
            {
                OpenPane();
                await Task.Delay(150);
            }
            HamburgerListBox.IsEnabled = true;
        }

        private async void LayoutController_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            if (!_manipulationsEnabled) return;
            double v = e.Velocities.Linear.X;
            if (v < 0)
            {
                ClosePane();
                await Task.Delay(150);
            }
            else if (v >= 0)
            {
                OpenPane();
                await Task.Delay(150);
            }
        }

        private void LayoutController_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
            if (!_manipulationsEnabled) return;
            SplitviewLayer.Width = LayoutController.Width;
        }

        private void LayoutController_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            if (!_manipulationsEnabled) return;
            double dx = e.Delta.Translation.X;
            if (SplitviewLayer.Width + dx <= 220 &&
                SplitviewLayer.Width + dx >= 0 &&
                LayoutController.Width + dx < 220)
            {
                LayoutController.Width += dx;
                SplitviewLayer.Width += dx;
            }
        }

        private void LayoutController_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ClosePane();
        }

        private void OpenPane()
        {
            _manipulationsEnabled = false;
            LayoutController.Width = double.NaN;
            LayoutController.HorizontalAlignment = HorizontalAlignment.Stretch;
            DoubleAnimation line = new DoubleAnimation()
            {
                From = SplitviewLayer.Width,
                To = 220,
                Duration = TimeSpan.FromMilliseconds(150),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(line, SplitviewLayer);
            Storyboard.SetTargetProperty(line, "Width");
            Storyboard openpane = new Storyboard();
            openpane.Children.Add(line);
            openpane.Begin();
        }

        private void ClosePane()
        {
            _manipulationsEnabled = true;
            LayoutController.HorizontalAlignment = HorizontalAlignment.Left;
            LayoutController.Width = 12;
            DoubleAnimation line = new DoubleAnimation()
            {
                From = SplitviewLayer.Width,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(line, SplitviewLayer);
            Storyboard.SetTargetProperty(line, "Width");
            Storyboard openpane = new Storyboard();
            openpane.Children.Add(line);
            openpane.Begin();
        }

    }
}