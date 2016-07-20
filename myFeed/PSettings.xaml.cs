using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PSettings : Page
    {
        public PSettings()
        {
            this.InitializeComponent();
            App.ChosenIndex = 4;
            App.CanNavigate = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            toggleSwitch.IsOn = App.DownloadImages;
            switch (App.FontSize)
            {
                case 15: FontCombo.SelectedIndex = 0; break;
                case 17: FontCombo.SelectedIndex = 1; break;
                case 19: FontCombo.SelectedIndex = 2; break;
            }
            switch (App.CheckTime)
            {
                case 30: NotifyCombo.SelectedIndex = 0; break;
                case 60: NotifyCombo.SelectedIndex = 1; break;
                case 180: NotifyCombo.SelectedIndex = 2; break;
                case 0: NotifyCombo.SelectedIndex = 3; break;
            }
        }

        private async void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (FontCombo.SelectedIndex)
            {
                case 0: App.FontSize = 15; break;
                case 1: App.FontSize = 17; break;
                case 2: App.FontSize = 19; break;
            }

            await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("settings.txt"), App.FontSize.ToString());
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ResourceLoader rl = new ResourceLoader();
            MessageDialog dialog = new MessageDialog(rl.GetString("ShowMessageAlert"));
            dialog.Title = rl.GetString("ShowMessageTitle");
            dialog.Commands.Add(new UICommand { Label = rl.GetString("Delete"), Id = 0 });
            dialog.Commands.Add(new UICommand { Label = rl.GetString("Cancel"), Id = 1 });
            IUICommand res = await dialog.ShowAsync();
            if ((int)res.Id == 0)
            {
                IReadOnlyList<StorageFile> files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
                foreach (StorageFile file in files) await file.DeleteAsync(StorageDeleteOption.Default);
                await (await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites")).DeleteAsync(StorageDeleteOption.Default);
                Application.Current.Exit();
            }
        }

        private async void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            App.DownloadImages = toggleSwitch.IsOn;
            await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("loadimg.txt"), App.DownloadImages.ToString());
        }

        private async void NotifyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (NotifyCombo.SelectedIndex)
            {
                case 0: App.CheckTime = 30; break;
                case 1: App.CheckTime = 60; break;
                case 2: App.CheckTime = 180; break;
                case 3: App.CheckTime = 0; break;
            }

            await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("checktime"), App.CheckTime.ToString());
            BackgroundTaskManager.RegisterNotifier(App.CheckTime);
        }
    }
}
