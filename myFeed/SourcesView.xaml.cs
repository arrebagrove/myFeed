using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

namespace myFeed
{
    public sealed partial class SourcesView : Page
    {
        public SourcesView()
        {
            this.InitializeComponent();
        }

        public class ListFeed
        {
            public string feedtitle { get; set; }
            public string feedsubtitle { get; set; }
            public string feedid { get; set; }
            public string feedimg { get; set; }
        }

        List<string> list = new List<string>();
        string categoryname = string.Empty;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            list = e.Parameter as List<string>;
            CategoryTitle.Content = list.First();
            categoryname = list.First();
            list.Remove(list.First());
            list.Remove(list.Last());
            CountInCategory.Content = list.Count;
            SyndicationClient client = new SyndicationClient();
            foreach (string str in list)
            {
                ListFeed feeditem = await GetItem(client, str);
                Display.Items.Add(feeditem);
            }
            LoadStatus.IsIndeterminate = false;
            LoadStatus.Opacity = 0;
        }       

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatus.IsIndeterminate = true;
            LoadStatus.Opacity = 1;
            ResourceLoader rl = new ResourceLoader();
            string link = RssLink.Text;
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("sources");
            string filecontent = await FileIO.ReadTextAsync(file);

            if (!Regex.IsMatch(link, @"^http(s)?://*") || link.Contains(';'))
            {
                var dialog = new MessageDialog(rl.GetString("LinkNotValid"));
                dialog.Title = rl.GetString("Error");
                dialog.Commands.Add(new UICommand { Label = rl.GetString("Ok"), Id = 0 });
                var res = await dialog.ShowAsync();

                LoadStatus.IsIndeterminate = false;
                LoadStatus.Opacity = 0;
                return;
            }

            if (filecontent.Contains(link))
            {
                var dialog1 = new MessageDialog(rl.GetString("LinkExists"));
                dialog1.Title = rl.GetString("Error");
                dialog1.Commands.Add(new UICommand { Label = rl.GetString("Ok"), Id = 0 });
                var res1 = await dialog1.ShowAsync();

                LoadStatus.IsIndeterminate = false;
                LoadStatus.Opacity = 0;
                return;
            }

            await FileIO.WriteTextAsync(file, filecontent.Replace(CategoryTitle.Content + ";", CategoryTitle.Content + ";" + link + ';'));        
            ExpandItems(MainBorder.Height, MainBorder.Height + 66);
            list.Add(link);
            CountInCategory.Content = list.Count;
            Display.Items.Add(await GetItem(new SyndicationClient(), link));
            LoadStatus.IsIndeterminate = false;
            LoadStatus.Opacity = 0;
        }

        private async void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            ResourceLoader rl = new ResourceLoader();
            var dialog = new MessageDialog(rl.GetString("DeleteAction"));
            dialog.Title = rl.GetString("DeleteElement");
            dialog.Commands.Add(new UICommand { Label = rl.GetString("Delete"), Id = 0 });
            dialog.Commands.Add(new UICommand { Label = rl.GetString("Cancel"), Id = 1 });
            var res = await dialog.ShowAsync();
            if ((int)res.Id == 0)
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("sources");
                string filestring = await FileIO.ReadTextAsync(file);
                Button stack = (Button)sender;
                ListFeed p = (ListFeed)stack.DataContext;
                await FileIO.WriteTextAsync(file, filestring.Replace(p.feedid + ';', string.Empty));
                Display.Items.Remove(p);
                list.Remove(p.feedid);
                CountInCategory.Content = list.Count;
                ExpandItems(MainBorder.Height, MainBorder.Height - 66);
            }
        }

        public static async Task<ListFeed> GetItem(SyndicationClient client, string website)
        {
            ResourceLoader rl = new ResourceLoader();
            ListFeed list = new ListFeed();
            try
            {
                SyndicationFeed feed = await SyndicationConverter.RetrieveFeedAsync(website);
                if (feed.Title != null)
                {
                    list.feedimg = "http://www.google.com/s2/favicons?domain=" + website;
                    if (feed.Subtitle != null)
                    {
                        list.feedtitle = feed.Title.Text;
                        list.feedsubtitle = feed.Subtitle.Text;
                    }
                    else
                    {
                        list.feedtitle = feed.Title.Text;
                        list.feedsubtitle = rl.GetString("SiteFeed") + list.feedtitle;
                    }
                }
                else
                {
                    list.feedtitle = website;
                    list.feedsubtitle = rl.GetString("CanNotGetData");
                }
                list.feedid = website;
                return list;
            }
            catch
            {
                list.feedtitle = website;
                list.feedsubtitle = rl.GetString("CanNotGetData");
                list.feedid = website;
                return list;
            }
        }

        private void stack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MainBorder.Height == 46)
            {
                ExpandCollapse.Content = "";
                ExpandItems(46, (list.Count * 66) + 46 + 80); 
            }
            else
            {
                ExpandCollapse.Content = "";
                ExpandItems((list.Count * 66) + 46 + 80, 46);
            }
        }

        private void ExpandItems(double from, double to)
        {
            DoubleAnimation fade = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(0.2),
                EnableDependentAnimation = true,
            };
            Storyboard.SetTarget(fade, MainBorder);
            Storyboard.SetTargetProperty(fade, "Height");
            Storyboard openpane = new Storyboard();
            openpane.Children.Add(fade);
            openpane.Begin();
        }

        private void stack_Holding(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            StorageFile target = await ApplicationData.Current.LocalFolder.GetFileAsync("sources");
            IList<string> filelist = await FileIO.ReadLinesAsync(target);
            foreach (string item in filelist) if (item.Contains(categoryname)) { filelist.Remove(item); break; }
            await FileIO.WriteLinesAsync(target, filelist);
            ExpandItems(MainBorder.Height, 0);
        }
    }
}
