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
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class MainPage : Page
    {
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

        private void HamburgerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HamburgerListBox.SelectedIndex == App.ChosenIndex) return;
            switch (HamburgerListBox.SelectedIndex)
            {
                case 0:
                    return;
                case 1:
                    MySplitView.IsPaneOpen = false;
                    MainFrame.Navigate(typeof(PFeed));
                    break;
                case 2:
                    MySplitView.IsPaneOpen = false;
                    MainFrame.Navigate(typeof(PFavorites));
                    break;
                case 3:
                    MySplitView.IsPaneOpen = false;
                    MainFrame.Navigate(typeof(PFeedList));
                    break;
                case 4:
                    MySplitView.IsPaneOpen = false;
                    MainFrame.Navigate(typeof(Search));
                    break;
                case 5:
                    MySplitView.IsPaneOpen = false;
                    MainFrame.Navigate(typeof(PSettings));
                    break;
            }
        }
        
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            HamburgerListBox.SelectedIndex = App.ChosenIndex;
        }

        private void SplitView_Open(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = true;
        }

        private void SplitView_Close(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = false;
        }
    }
}