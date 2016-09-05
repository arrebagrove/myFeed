using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PFavorites : Page
    {
        public PFavorites()
        {
            this.InitializeComponent();
            App.ChosenIndex = 2;
            App.CanNavigate = false;          
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (ArticleFrame.CanGoBack)
                {
                    ArticleFrame.GoBack();
                    a.Handled = true;
                    if (!ArticleFrame.CanGoBack) App.CanNavigate = true;
                }
            };
        }

        List<PFeedItem> itemlist = new List<PFeedItem>();

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ArticleFrame.Navigate(typeof(Page));
            App.CanNavigate = true;
            await Go(Display, e.Parameter);
            LoadStatus.IsIndeterminate = false;
            StatusBarDisabling.Begin();
        }

        private async Task Go(ItemsControl list, object NavUri)
        {
            StorageFolder favorites = await (ApplicationData.Current.LocalFolder.GetFolderAsync("favorites"));
            IReadOnlyList<StorageFile> files = await favorites.GetFilesAsync();

            if (files.Count == 0) Welcome.Visibility = Visibility.Visible;

            foreach (StorageFile file in files) itemlist.Add(await SerializerExtensions.DeSerializeObject<PFeedItem>(file));
            await Task.Delay(TimeSpan.FromMilliseconds(5));

            if (NavUri != null)
            {
                foreach (PFeedItem item in itemlist)
                {
                    CompareAddTimeRead(item, list, false);
                    if (item.link == NavUri.ToString()) ArticleFrame.Navigate(typeof(PArticle), item);
                }
            }
            else
            {
                foreach (PFeedItem item in itemlist) CompareAddTimeRead(item, list, false);
            }
        }

        private void Header_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Scroller.ChangeView(null, 0, null, false);
        }

        private void ListTapped(object sender, TappedRoutedEventArgs e)
        {
            Button stack = (Button)sender;
            PFeedItem p = (PFeedItem)stack.DataContext;
            ArticleFrame.Navigate(typeof(PArticle), p);
        }

        private void ReadItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem stack = (MenuFlyoutItem)sender;
            PFeedItem p = (PFeedItem)stack.DataContext;
            ArticleFrame.Navigate(typeof(PArticle), p);
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem stack = (MenuFlyoutItem)sender;
            PFeedItem p = (PFeedItem)stack.DataContext;
            int index = Display.Items.IndexOf(p);

            StorageFile saved_cache = await ApplicationData.Current.LocalFolder.GetFileAsync("saved_cache");
            string cache = await FileIO.ReadTextAsync(saved_cache);
            await FileIO.WriteTextAsync(saved_cache, cache.Replace(p.link + ';', string.Empty));

            StorageFolder filefolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");
            IReadOnlyList<StorageFile> files = await filefolder.GetFilesAsync();
            int filecount = files.Count;

            await files[index].DeleteAsync();
            Display.Items.Remove(p);

            if ((filecount > 1) && (filecount - 1 != index))
            {
                for (int x = index + 1; x < filecount; x++)
                {
                    await files[x].RenameAsync((x - 1).ToString());
                }
            }

            if (filecount - 1 < 1) WelcomeEnabling.Begin();
            
            IReadOnlyList<SecondaryTile> sTiles = await SecondaryTile.FindAllAsync();
            SecondaryTile tile = (await SecondaryTile.FindAllAsync()).FirstOrDefault((t) => t.TileId == p.GetTileId());
            if (tile != null) await tile.RequestDeleteAsync();
        }

        private void CompareAddTimeRead(PFeedItem item, ItemsControl display, bool checkRead)
        {
            item.dateoffset = (DateTime.Now.Date == item.PublishedDate.Date) 
                ? item.PublishedDate.DateTime.ToString("HH:mm") 
                : item.dateoffset = item.PublishedDate.DateTime.ToString("MM.dd");
            display.Items.Add(item);
        }

        private async void OpenInEdge_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem stack = (MenuFlyoutItem)sender;
            PFeedItem p = (PFeedItem)stack.DataContext;
            await Windows.System.Launcher.LaunchUriAsync(new Uri(p.link));
        }

        private void ImageBrush_ImageOpened(object sender, RoutedEventArgs e)
        {
            ImageBrush img = sender as ImageBrush;
            img.Opacity = 0;
            if (img.ImageSource.ToString() == string.Empty) return;
            DoubleAnimation fade = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.2),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fade, img);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard openpane = new Storyboard();
            openpane.Children.Add(fade);
            openpane.Begin();
        }

        private void SavedItem_Holding(object sender, RoutedEventArgs e)
        {
            PFeedItem feeditem = (PFeedItem)((FrameworkElement)sender).DataContext;
            MenuFlyout menu = FlyoutBase.GetAttachedFlyout((Button)sender) as MenuFlyout;
            MenuFlyoutItem itempin = menu.Items[2] as MenuFlyoutItem;
            MenuFlyoutItem itemunpin = menu.Items[3] as MenuFlyoutItem;

            if (!SecondaryTile.Exists(feeditem.GetTileId()))
            {
                itempin.Visibility = Visibility.Collapsed;
                itemunpin.Visibility = Visibility.Visible;
            }
            else
            {
                itemunpin.Visibility = Visibility.Collapsed;
                itempin.Visibility = Visibility.Visible;
            }

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void UnpinFromStart_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem feeditem = (PFeedItem)((FrameworkElement)sender).DataContext;
            IReadOnlyList<SecondaryTile> sTiles = await SecondaryTile.FindAllAsync();
            SecondaryTile tile = (await SecondaryTile.FindAllAsync()).FirstOrDefault((t) => t.TileId == feeditem.GetTileId());
            if (tile != null) await tile.RequestDeleteAsync();
        }

        private async void PinBtn_Click(object sender, RoutedEventArgs e)
        {
            string title = "Избранное";
            SecondaryTile secondaryTile = new SecondaryTile(
                title,
                title,
                "fav",
                new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png"),
                TileSize.Square150x150
            );
            await secondaryTile.RequestCreateAsync();
        }

        private async void PinToStart_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem feeditem = (PFeedItem)((FrameworkElement)sender).DataContext;
            string TileId = feeditem.GetTileId();

            SecondaryTile secondaryTile = new SecondaryTile(
                TileId,
                "myFeed Article",
                feeditem.link,
                new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png"),
                TileSize.Square150x150
            );

            await secondaryTile.RequestCreateAsync();
            string contents = string.Format(@"
                <tile>
                    <visual>
                        <binding template='TileMedium'>
                            <text hint-wrap='true' hint-maxLines='2' hint-style='caption'>{0}</text>
                            <text hint-style='captionSubtle'>{1}</text>
                            <text hint-style='captionSubtle'>{2}</text>
                        </binding>
                        <binding template='TileWide'>
                            <text hint-wrap='true' hint-maxLines='3' hint-style='caption'>{0}</text>
                            <text hint-style='captionSubtle'>{1}</text>
                            <text hint-style='captionSubtle'>{3}</text>
                        </binding>
                    </visual>
                </tile> ", feeditem.title, feeditem.feed, feeditem.PublishedDate.ToString("dd.MM.yyyy"), 
                feeditem.PublishedDate.ToString("dd.MM.yyyy HH: mm"));

            Windows.Data.Xml.Dom.XmlDocument xmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
            xmlDoc.LoadXml(contents);
            
            TileNotification notifyTile = new TileNotification(xmlDoc);
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(TileId).Update(notifyTile);
        }
    }
}
