using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            InitializeComponent();
        }

        public class ListFeed
        {
            public string feedtitle { get; set; }
            public string feedsubtitle { get; set; }
            public string feedid { get; set; }
            public string feedimg { get; set; }
        }

        Category cat = new Category();
        string website_buffer = string.Empty;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            cat = e.Parameter as Category;
            CategoryTitle.Content = cat.title;
            CountInCategory.Content = cat.websites.Count;
            SyndicationClient client = new SyndicationClient();

            foreach (Website ws in cat.websites)
            {
                ListFeed feeditem = await GetItem(client, ws.url);
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

            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            if (!Regex.IsMatch(link, @"^http(s)?://*"))
            {
                var dialog = new MessageDialog(rl.GetString("LinkNotValid"));
                dialog.Title = rl.GetString("Error");
                dialog.Commands.Add(new UICommand { Label = rl.GetString("Ok"), Id = 0 });
                var res = await dialog.ShowAsync();

                LoadStatus.IsIndeterminate = false;
                LoadStatus.Opacity = 0;
                return;
            }

            Website wb = new Website();
            wb.url = link;
            cat.websites.Add(wb);
            foreach (Category c in cats.categories)
            {
                if (c.title != cat.title) continue;
                c.websites.Add(wb);
                break;
            }

            SerializerExtensions.SerializeObject(cats, 
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            ExpandItems(MainBorder.Height, MainBorder.Height + 66);
            CountInCategory.Content = cat.websites.Count;
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
                Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                    await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                Button stack = (Button)sender;
                ListFeed p = (ListFeed)stack.DataContext;
                string link = p.feedid;

                foreach (Website wb in cat.websites)
                {
                    if (wb.url != link) continue;
                    cat.websites.Remove(wb);
                    break;
                }

                foreach (Category c in cats.categories)
                {
                    if (c.title != cat.title) continue;
                    foreach (Website wb in c.websites)
                    {
                        if (wb.url != link) continue;
                        c.websites.Remove(wb);
                        break;
                    }
                    break;
                }

                SerializerExtensions.SerializeObject(cats, await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                Display.Items.Remove(p);
                CountInCategory.Content = cat.websites.Count;
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
                ExpandItems(46, (cat.websites.Count * 66) + 46 + 80); 
            }
            else
            {
                ExpandCollapse.Content = "";
                ExpandItems((cat.websites.Count * 66) + 46 + 80, 46);
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
            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            foreach (Category c in cats.categories)
            {
                if (c.title != cat.title) continue;
                cats.categories.Remove(c);
                break;
            }

            SerializerExtensions.SerializeObject(cats, 
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            ExpandItems(MainBorder.Height, 0);
        }

        private async void RenameCategory_Click(object sender, RoutedEventArgs e)
        {
            CategoryDialog addcat = new CategoryDialog();
            addcat.KeyDown += (s, a) =>
            {
                if (a.Key == Windows.System.VirtualKey.Enter)
                {
                    RenameCategory(addcat);
                    addcat.Hide();
                    a.Handled = true;
                }
            };

            if (await addcat.ShowAsync() == ContentDialogResult.Primary)
            {
                RenameCategory(addcat);
                addcat.Hide();
            }
        }

        private async void RenameCategory(CategoryDialog dialog)
        {
            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            foreach (Category c in cats.categories)
            {
                if (c.title == dialog.CategoryName)
                {
                    await (new MessageDialog((new ResourceLoader()).GetString("CategoryExists")).ShowAsync());
                    return;
                }
            }

            foreach (Category c in cats.categories)
            {
                if (c.title != cat.title) continue;
                c.title = dialog.CategoryName;
                cat.title = dialog.CategoryName;
                CategoryTitle.Content = dialog.CategoryName;
                break;
            }

            SerializerExtensions.SerializeObject(cats,
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
        }

        private void Grid_RightTapped(object sender, RoutedEventArgs e)
        {
            ListFeed feeditem = (ListFeed)((FrameworkElement)sender).DataContext;
            ResourceLoader rl = new ResourceLoader();

            foreach (Website website in cat.websites)
            {
                if (website.url != feeditem.feedid) continue;
                MenuFlyout menu = FlyoutBase.GetAttachedFlyout((Grid)sender) as MenuFlyout;
                MenuFlyoutItem item = menu.Items[0] as MenuFlyoutItem;
                if (website.notify)
                {
                    item.Text = rl.GetString("NotifyOff");
                }
                else
                {
                    item.Text = rl.GetString("NotifyOn");
                }
                website_buffer = feeditem.feedid;
                break;
            }

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void ToggleNotifications_Click(object sender, RoutedEventArgs e)
        {
            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            foreach (Category c in cats.categories)
            {
                if (c.title != cat.title) continue;
                foreach (Website website in c.websites) 
                    if (website.url == website_buffer)
                        website.notify = !website.notify;
                cat = c;
            }

            SerializerExtensions.SerializeObject(cats,
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
        }
    }
}
