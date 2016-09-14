using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;

namespace myFeed
{
    public sealed partial class Search : Page
    {
        public Search()
        {
            this.InitializeComponent();
            App.ChosenIndex = 4;
            App.CanNavigate = true;
        }

        private async void FindAll_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchInput.Text))
            {
                WelcomeDisabling.Begin();
                NetworkError.Visibility = Visibility.Collapsed;
                LoadStatus.IsIndeterminate = true;
                StatusBarEnabling.Begin();

                await SearchForFeeds(@"https://ajax.googleapis.com/ajax/services/feed/find?v=1.0&q=" + SearchInput.Text);

                LoadStatus.IsIndeterminate = false;
                StatusBarDisabling.Begin();
            }
        }

        private async Task SearchForFeeds(string query)
        {
            try
            {
                WebRequest request = WebRequest.Create(query);
                request.ContentType = "application/json; charset=utf-8";
                WebResponse response = await request.GetResponseAsync();

                string responseString = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                }

                if (responseString != string.Empty)
                {
                    dynamic stuff = JObject.Parse(responseString);
                    Display.Items.Clear();
                    foreach (var item in stuff.responseData.entries)
                    {
                        ListFeed lf = new ListFeed();
                        lf.feedid = item.url;
                        lf.feedlink = item.link;

                        string content;
                        content = Regex.Replace(item.title.ToString(), @"<[^>]*>", string.Empty);
                        content = Regex.Replace(content, @"(&.*?;)", " ");
                        content = Regex.Replace(content, @"\r|\n", string.Empty);
                        lf.feedtitle = content;

                        content = Regex.Replace(item.contentSnippet.ToString(), @"<[^>]*>", string.Empty);
                        content = Regex.Replace(content, @"(&.*?;)", " ");
                        content = Regex.Replace(content, @"\r|\n", string.Empty);
                        lf.feedsubtitle = content;

                        lf.feedimg = "http://www.google.com/s2/favicons?domain=" + lf.feedlink;
                        Display.Items.Add(lf);
                    }
                }

                if (Display.Items.Count < 1)
                    throw new Exception();
            }
            catch
            {
                NetworkError.Opacity = 0;
                NetworkError.Visibility = Visibility.Visible;
                ErrorEnabling.Begin();
            }
        }

        private void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            Image img = sender as Image;
            img.Opacity = 0;
            if (img.Source.ToString() == string.Empty) return;
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

        private void Grid_Holding(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((Grid)sender);
        }

        private void SearchItemCopy_Click(object sender, RoutedEventArgs e)
        {
            ListFeed feed = (ListFeed)((FrameworkElement)sender).DataContext;
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(feed.feedid);
            Clipboard.SetContent(dataPackage);
        }

        private async void SearchItemOpen_Click(object sender, RoutedEventArgs e)
        {
            ListFeed feed = (ListFeed)((FrameworkElement)sender).DataContext;
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(feed.feedlink));
            }
            catch { }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ListFeed feed = (ListFeed)((FrameworkElement)sender).DataContext;
            ResourceLoader rl = new ResourceLoader();
            bool isEmpty = false;

            SearchAddDialog addsource = new SearchAddDialog();
            Categories cats = new Categories();
            ComboBox box = new ComboBox();

            addsource.Loaded += async (s, a) =>
            {
                cats = await SerializerExtensions.DeSerializeObject<Categories>(
                    await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
                box = addsource.CategoriesComboBox;

                foreach (Category cat in cats.categories) box.Items.Add(cat.title);
                if (box.Items.Count < 1)
                {
                    addsource.CategoriesTextBlock.Visibility = Visibility.Collapsed;
                    addsource.CategoriesComboBox.Visibility = Visibility.Collapsed;
                    addsource.Title = rl.GetString("SearchCreateNewCatTitleFL");
                    addsource.CategoriesTextBlockBox.Text = rl.GetString("SearchCreateNewCatTextFL");
                    isEmpty = true;
                    return;
                }

                box.SelectedIndex = 0;
            };

            if (await addsource.ShowAsync() == ContentDialogResult.Primary)
            {
                string name = addsource.CategoriesText.Text;
                if (string.IsNullOrWhiteSpace(name) && isEmpty) return;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    Category newcat = new Category()
                    {
                        title = name,
                        websites = new List<Website>()
                        {
                            new Website()
                            {
                                notify = true,
                                url = feed.feedid
                            }
                        }
                    };

                    cats.categories.Add(newcat);
                    SerializerExtensions.SerializeObject(cats,
                        await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
                    addsource.Hide();
                    await (new MessageDialog(string.Format(rl.GetString("SearchAddMessage"), feed.feedtitle, name),
                        rl.GetString("SearchAddSuccess"))).ShowAsync();
                    return;
                }

                name = (string)box.SelectedItem;
                foreach (Category cat in cats.categories)
                {
                    if (cat.title != name) continue;
                    Website wb = new Website();
                    wb.url = feed.feedid;
                    cat.websites.Add(wb);
                    break;
                }

                SerializerExtensions.SerializeObject(cats,
                    await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
                addsource.Hide();
                await (new MessageDialog(string.Format(rl.GetString("SearchAddMessage"), feed.feedtitle, name),
                    rl.GetString("SearchAddSuccess"))).ShowAsync();
            }
        }
    }
}
