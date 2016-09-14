using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
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
            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(300, 300));

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                SolidColorBrush statuscolor = NavBackground.Background as SolidColorBrush;
                statusBar.BackgroundColor = statuscolor.Color;
                statusBar.BackgroundOpacity = 1;
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await CheckFiles();
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
            MainFrame.Navigate(typeof(PFeed), id);
        }

        private async Task CheckFiles()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            try
            {
                StorageFile configfile = await storageFolder.GetFileAsync("config");
                App.config = await SerializerExtensions.DeSerializeObject<App.ConfigFile>(configfile);
            }
            catch
            {
                StorageFile configfile = await storageFolder.CreateFileAsync("config");
                SerializerExtensions.SerializeObject(App.config, configfile);
            }

            string temp = await MigrateData("loadimg.txt");
            if (temp != string.Empty) App.config.DownloadImages = bool.Parse(temp);

            temp = await MigrateData("checktime");
            if (temp != string.Empty) App.config.CheckTime = uint.Parse(temp);

            temp = await MigrateData("settings.txt");
            if (temp != string.Empty) App.config.FontSize = int.Parse(temp);

            try
            {
                await storageFolder.GetFileAsync("sites");
            }
            catch
            {
                await storageFolder.CreateFileAsync("sites");
                Categories cats = new Categories();
                cats.categories = new List<Category>();
                SerializerExtensions.SerializeObject(cats, await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
            }

            /// Here we do some stupid stuff in order to migrade 
            /// from old strange data storing format.
            temp = await MigrateData("sources");
            if (temp != string.Empty)
            {
                try
                {
                    Categories cats = new Categories();
                    cats.categories = new List<Category>();
                    List<string> sourceslist = temp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
                    if (sourceslist.Count > 0) sourceslist.Remove(sourceslist.Last());
                    foreach (string str in sourceslist)
                    {
                        Category cat = new Category();
                        List<string> slist = str.Split(';').ToList();
                        if (slist.Count > 0) slist.RemoveAt(slist.Count - 1);
                        if (slist.Count > 0) cat.title = slist.First();
                        if (slist.Count > 0) slist.Remove(slist.First());
                        cat.websites = new List<Website>();
                        foreach (string s in slist)
                        {
                            Debug.WriteLine(s);
                            Website wb = new Website();
                            wb.url = s;
                            cat.websites.Add(wb);
                        }
                        cats.categories.Add(cat);
                    }
                    SerializerExtensions.SerializeObject(cats, await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                }
                catch { }
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
                /// Here we delete info about old articles.
                App.Read = await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("read.txt"));
                List<string> read_list = App.Read.Split(';').ToList();
                read_list.RemoveAt(read_list.Count - 1);
                if (read_list.Count > 90) read_list = read_list.Skip(Math.Max(0, read_list.Count() - 90)).ToList();
                App.Read = string.Empty;
                foreach (string item in read_list) App.Read = App.Read + item + ';';
                await FileIO.WriteTextAsync(await storageFolder.GetFileAsync("read.txt"), App.Read);
            }
            catch
            {
                await storageFolder.CreateFileAsync("read.txt", CreationCollisionOption.ReplaceExisting);
            }

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

        private async Task<string> MigrateData(string filename)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                string setting = await FileIO.ReadTextAsync(file);
                await file.DeleteAsync();
                return setting;
            }
            catch
            {
                return string.Empty;
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