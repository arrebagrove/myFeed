using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Core;
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
            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(300, 300));        
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (MainFrame.CanGoBack && App.CanNavigate)
                {
                    MainFrame.GoBack();
                    a.Handled = true;
                }                   
            };
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                SolidColorBrush statuscolor = NavBackground.Background as SolidColorBrush;
                statusBar.BackgroundColor = statuscolor.Color;
                statusBar.BackgroundOpacity = 1;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.CheckFiles();
            this.ExportFromOldSitesFormat();
            BackgroundTaskManager.RegisterNotifier(App.CheckTime);
            if (string.IsNullOrEmpty(e.Parameter.ToString())) MainFrame.Navigate(typeof(PFeed)); else this.FromSecondaryTile(e.Parameter);
        }

        private async void CheckFiles()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            try
            {
                await storageFolder.GetFileAsync("sources");
            }
            catch
            {
                await storageFolder.CreateFileAsync("sources");
            }

            try
            {
                await storageFolder.GetFileAsync("datecutoff");
            }
            catch
            {
                await storageFolder.CreateFileAsync("datecutoff");
            }

            try
            {
                await storageFolder.GetFolderAsync("favorites");
            }
            catch
            {
                await storageFolder.CreateFolderAsync("favorites");
            }

            try
            {
                await storageFolder.GetFileAsync("saved_cache");
            }
            catch
            {
                await storageFolder.CreateFileAsync("saved_cache");
            }

            try
            {
                App.Read = await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("read.txt"));
                List<string> read_list = App.Read.Split(';').ToList();
                read_list.RemoveAt(read_list.Count - 1);
                if (read_list.Count > 90)
                    read_list = read_list.Skip(Math.Max(0, read_list.Count() - 90)).ToList();             
                App.Read = string.Empty;
                foreach (string item in read_list) App.Read = App.Read + item + ';';
                await FileIO.WriteTextAsync(await storageFolder.GetFileAsync("read.txt"), App.Read);
            }
            catch
            {
                await storageFolder.CreateFileAsync("read.txt", CreationCollisionOption.ReplaceExisting);
            }

            try
            {
                App.FontSize = Convert.ToInt32(await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("settings.txt")));
            }
            catch
            {
                await storageFolder.CreateFileAsync("settings.txt", CreationCollisionOption.ReplaceExisting);
            }

            try
            {
                App.DownloadImages = Convert.ToBoolean(await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("loadimg.txt")));
            }
            catch
            {
                await storageFolder.CreateFileAsync("loadimg.txt", CreationCollisionOption.ReplaceExisting);
            }

            try
            {
                App.CheckTime = Convert.ToUInt32(await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("checktime")));
            }
            catch
            {
                await FileIO.WriteTextAsync(await storageFolder.CreateFileAsync("checktime", CreationCollisionOption.ReplaceExisting), "60");
                App.CheckTime = 60;
            }
        }

        public void FromSecondaryTile(object e)
        {
            if (e.ToString() == "fav") MainFrame.Navigate(typeof(PFavorites)); else MainFrame.Navigate(typeof(PFavorites), e.ToString());
            if (MainFrame.BackStackDepth > 0) if (MainFrame.BackStack[MainFrame.BackStackDepth - 1].SourcePageType == typeof(PFavorites))
                MainFrame.BackStack.RemoveAt(MainFrame.BackStackDepth - 1);
        }

        public void FindNotification(string id)
        {
            MainFrame.Navigate(typeof(PFeed), id);
            MainFrame.BackStack.RemoveAt(MainFrame.BackStackDepth - 1);
        }

        private async void ExportFromOldSitesFormat()
        {
            try
            {
                StorageFile sitestxt = await ApplicationData.Current.LocalFolder.GetFileAsync("sites.txt");
                string content = (new ResourceLoader()).GetString("News") + ';' 
                    + await FileIO.ReadTextAsync(sitestxt) + Environment.NewLine;
                await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("sources"), content);
                await sitestxt.DeleteAsync();
            }
            catch
            {
                return;
            }
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