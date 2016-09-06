using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

namespace myFeed
{
    public sealed partial class PFeed : Page
    {
        private List<string> sourceslist = new List<string>();
        private bool CanUpdate = true;

        public PFeed()
        {
            this.InitializeComponent();
            App.ChosenIndex = 1;
            ArticleFrame.Navigate(typeof(BlankPage));
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (ArticleFrame.CanGoBack)
                {
                    ArticleFrame.GoBack();
                    a.Handled = true;
                    if (!ArticleFrame.CanGoBack) App.CanNavigate = true;
                }
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
                bool internet = connections != null && connections.GetNetworkConnectivityLevel() == 
                    NetworkConnectivityLevel.InternetAccess;
                if (!internet) { NetworkError.Visibility = Visibility.Visible; return; }

                Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                    await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                if (e != null)
                {
                    if (e.Parameter != null)
                    {
                        NotificationStatus.IsActive = true;
                        string[] data = e.Parameter.ToString().Split(';');
                        await SearchArticleFromNotification(data[0], data[1]);
                        NotificationStatus.IsActive = false;
                    }
                }

                if (cats != null)
                {
                    foreach (Category cat in cats.categories)
                    {
                        PivotItem item = new PivotItem();
                        item.Header = cat.title;
                        Frame frame = new Frame();

                        Bag bag = new Bag();
                        bag.list = cat.websites;
                        bag.frame = ArticleFrame;
                        bag.parentframe = Frame;

                        frame.Navigate(typeof(ItemsControlView), bag);
                        item.Content = frame;
                        item.Margin = new Thickness(0, 0, 0, 0);
                        Categories.Items.Add(item);
                    }

                    if (cats.categories.Count == 0) Welcome.Visibility = Visibility.Visible;
                }
                else
                {
                    Welcome.Visibility = Visibility.Visible;
                }
                                
                await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("datecutoff"), 
                    DateTime.Now.ToString());
            }
            catch { }
        }

        private async Task SearchArticleFromNotification(string theid, string website)
        {
            SyndicationFeed feed = await SyndicationConverter.RetrieveFeedAsync(website);
            foreach (SyndicationItem item in feed.Items)
            {
                string itemid = (int)item.Title.Text.First()
                    + item.PublishedDate.Second.ToString()
                    + item.PublishedDate.Minute.ToString()
                    + item.PublishedDate.Hour.ToString()
                    + item.PublishedDate.Day.ToString();

                if (theid == itemid)
                {
                    PFeedItem target = new PFeedItem();
                    target.title = Regex.Replace(item.Title.Text, @"(&.*?;)", " ");
                    target.link = item.Links.FirstOrDefault().Uri.ToString();
                    target.feed = feed.Title.Text;
                    target.PublishedDate = item.PublishedDate;
                    target.opacity = 0.5;
                    App.Read = App.Read + itemid + ';';
                    await FileIO.AppendTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("read.txt"), 
                        itemid + ';');

                    try
                    {
                        target.content = item.Summary.Text;
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
                    }

                    ArticleFrame.Navigate(typeof(PArticle), target);
                }
            }
        }

        private async void UpdBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CanUpdate) return;
            CanUpdate = false;
            NetworkError.Visibility = Visibility.Collapsed;
            Categories.Items.Clear();
            OnNavigatedTo(null);
            await Task.Delay(2000);
            CanUpdate = true;
        }

        private void NavigateToSourcePage_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PFeedList));
        }
    }
}

