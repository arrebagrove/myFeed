﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PArticle : Page
    {
        private static string Link;
        private static PFeedItem Value;

        public PArticle()
        {
            this.InitializeComponent();
            App.ChosenIndex = -1;
            App.CanNavigate = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Value = (PFeedItem)e.Parameter;          
            StorageFile saved_cache = await ApplicationData.Current.LocalFolder.GetFileAsync("saved_cache");
            string cache = await FileIO.ReadTextAsync(saved_cache);
            if (cache.Contains(Value.link)) FavBtn.IsEnabled = false;

            Title.Text = Value.title;
            Feed.Text = Value.feed;
            Date.Text = Value.PublishedDate.DateTime.ToString();
            Link = Value.link;

            ContentRTB.FontSize = App.config.FontSize;
            ContentRTB.LineHeight = App.config.FontSize * 1.5;

            string cleancontent = Regex.Replace(Value.content, @"(&.*?;)", " ");
            cleancontent = Regex.Replace(cleancontent, @"\r\n?|\n", "");

            List<Block> blocks = HtmlToRichTextBlock.GenerateBlocksForHtml(cleancontent);
            ContentRTB.Blocks.Clear();
            foreach (Block b in blocks) ContentRTB.Blocks.Add(b);
            
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new Windows.Foundation.TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareTextHandler);
        }

        private async void ArticleLink_Tapped(object sender, RoutedEventArgs e)
        {
            ResourceLoader rl = new ResourceLoader();
            try
            {
                await Launcher.LaunchUriAsync(new Uri(Link));
            }
            catch
            {
                await (new MessageDialog(rl.GetString("ArticleOpenEdgeError"))).ShowAsync();
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = Title.Text;
            request.Data.Properties.Description = Link;
            request.Data.SetText(Title.Text + Environment.NewLine + Link + Environment.NewLine + "via myFeed for Windows 10");
        }

        private async void FavButton_Click(object sender, RoutedEventArgs e)
        {
            FavBtn.IsEnabled = false;

            StorageFile cache = await ApplicationData.Current.LocalFolder.GetFileAsync("saved_cache");
            StorageFolder favoritesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");       
            
            int filecount = (await favoritesFolder.GetFilesAsync()).Count;
            string favcache = await FileIO.ReadTextAsync(cache);

            if (!favcache.Contains(Value.link))
            {
                StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");
                SerializerExtensions.SerializeObject(Value, await storageFolder.CreateFileAsync(filecount.ToString()));
                await FileIO.AppendTextAsync(cache, Value.link + ";");
            }
        }

        private void HeadingCapsBlock_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Scroller.ChangeView(null, 0, null);
        }

        private void ArticleLink_Holding(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(Link);
            Clipboard.SetContent(dataPackage);
        }
    }
}
