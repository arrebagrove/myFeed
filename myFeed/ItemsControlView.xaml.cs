using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

namespace myFeed
{
    public sealed partial class ItemsControlView : Page
    {
        private int itemscount = 10;
        private List<PFeedItem> Fullfeed = new List<PFeedItem>();
        private Bag bag;

        public ItemsControlView()
        {
            InitializeComponent();
            Loaded += async (s, a) =>
            {
                if (bag.list.Count == 0) Welcome.Visibility = Visibility.Visible;
                await Go(bag.list);
                foreach (PFeedItem item in Fullfeed.GetRange(0, (Fullfeed.Count >= itemscount) 
                    ? itemscount : Fullfeed.Count)) CompareAddTimeRead(item);
                Showmore.Visibility = (Fullfeed.Count > itemscount) ? Visibility.Visible : Visibility.Collapsed;
                ProgRing.IsActive = false;
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            bag = e.Parameter as Bag;
        }

        private void CompareAddTimeRead(PFeedItem item)
        {
            item.dateoffset = (DateTime.Now.Date == item.PublishedDate.Date) ? item.PublishedDate.DateTime.ToString("HH:mm") : 
                item.dateoffset = item.PublishedDate.DateTime.ToString("MM.dd");
            item.opacity = App.Read.Contains(item.GetTileId()) ? 0.5 : 1;
            Display.Items.Add(item);
        }

        private void Showmore_Click(object sender, RoutedEventArgs e)
        {
            Showmore.IsEnabled = false;
            if ((Fullfeed.Count - itemscount) < 20)
            {
                foreach (PFeedItem item in Fullfeed.GetRange(itemscount, Fullfeed.Count - itemscount)) CompareAddTimeRead(item);
                Showmore.Visibility = Visibility.Collapsed;
                itemscount = Fullfeed.Count;
            }
            else
            {
                foreach (PFeedItem item in Fullfeed.GetRange(itemscount, 10)) CompareAddTimeRead(item);
                itemscount += 10;
            }
            Showmore.IsEnabled = true;
        }

        private async Task Go(List<Website> sites)
        {
            List<PFeedItem> fullfeed = new List<PFeedItem>();
            foreach (Website website in sites)
            {
                SyndicationFeed feed = new SyndicationFeed();

                try
                {
                    feed = await SyndicationConverter.RetrieveFeedAsync(website.url);
                }
                catch
                {
                    continue;
                }

                foreach (SyndicationItem item in feed.Items)
                {
                    PFeedItem target = new PFeedItem();
                    try
                    {
                        target.title = Regex.Replace(item.Title.Text, @"(&.*?;)", " ");
                        target.feed = feed.Title.Text;
                        target.PublishedDate = item.PublishedDate;
                        target.content = item.Summary.Text;
                        target.link = item.Links.FirstOrDefault().Uri.ToString();
                        if (App.config.DownloadImages)
                        {
                            Match match = Regex.Match(target.content, @"<img(.*?)>", RegexOptions.Singleline);
                            if (match.Success)
                            {
                                string val = match.Groups[1].Value;
                                Match match2 = Regex.Match(val, @"src=\""(.*?)\""", RegexOptions.Singleline);
                                if (match2.Success)
                                {
                                    target.image = match2.Groups[1].Value;
                                    target.iconopacity = 0;
                                }
                            }
                        }
                    }
                    catch
                    {
                        target.content = string.Empty;
                        target.link = string.Empty;
                    }

                    fullfeed.Add(target);
                }
            }

            Fullfeed = fullfeed.OrderByDescending(x => x.PublishedDate).ToList();
        }

        private async void MarkAsRead(PFeedItem item)
        {
            if (App.Read.Contains(item.GetTileId())) return;
            Button button = VisualTreeHelper.GetChild((FrameworkElement)Display.ContainerFromItem(item), 0) as Button;
            await FileIO.AppendTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("read.txt"), 
                item.GetTileId() + ";");
            App.Read = App.Read + item.GetTileId() + ";";
            button.Opacity = 0.5;
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
                Duration = TimeSpan.FromSeconds(0.3),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fade, img);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard openpane = new Storyboard();
            openpane.Children.Add(fade);
            openpane.Begin();
        }

        private async void BarWeb_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((MenuFlyoutItem)sender).DataContext;
            ResourceLoader rl = new ResourceLoader();
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(item.link));
            }
            catch
            {
                await (new MessageDialog(rl.GetString("ArticleOpenEdgeError"))).ShowAsync();
            }
            MarkAsRead(item);
        }

        private void ReadItem_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((MenuFlyoutItem)sender).DataContext;
            bag.frame.Navigate(typeof(PArticle), item);
            MarkAsRead(item);
        }

        private void ListTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((Button)sender).DataContext;
            bag.frame.Navigate(typeof(PArticle), item);
            MarkAsRead(item);
        }

        private async void Button_RightTapped(object sender, RoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((Button)sender).DataContext;
            MenuFlyout menu = FlyoutBase.GetAttachedFlyout((Button)sender) as MenuFlyout;
            MenuFlyoutItem favbutton = menu.Items[4] as MenuFlyoutItem;
            MenuFlyoutSeparator favsep = menu.Items[3] as MenuFlyoutSeparator;

            StorageFile cache = await ApplicationData.Current.LocalFolder.GetFileAsync("saved_cache");
            string favcache = await FileIO.ReadTextAsync(cache);
            if (favcache.Contains(item.link))
            {
                favbutton.Visibility = Visibility.Collapsed;
                favsep.Visibility = Visibility.Collapsed;
            }

            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void NavigateToSourcePage_Click(object sender, RoutedEventArgs e)
        {
            bag.parentframe.Navigate(typeof(PFeedList));
        }

        private async void AddToFavs_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((MenuFlyoutItem)sender).DataContext;
            StorageFile cache = await ApplicationData.Current.LocalFolder.GetFileAsync("saved_cache");
            string favcache = await FileIO.ReadTextAsync(cache);
            if (!favcache.Contains(item.link))
            {
                StorageFolder favoritesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");
                int filecount = (await favoritesFolder.GetFilesAsync()).Count;

                StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");
                SerializerExtensions.SerializeObject(item, await storageFolder.CreateFileAsync(filecount.ToString()));
                await FileIO.AppendTextAsync(cache, item.link + ";");
            }
        }

        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            PFeedItem item = (PFeedItem)((MenuFlyoutItem)sender).DataContext;
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(item.link);
            Clipboard.SetContent(dataPackage);
        }
    }
}
