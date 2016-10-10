using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PSettings : Page
    {
        private bool canSet = false;

        public PSettings()
        {
            this.InitializeComponent();
            App.ChosenIndex = 5;
            App.CanNavigate = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            toggleSwitch.IsOn = App.config.DownloadImages;

            switch (App.config.FontSize)
            {
                case 15: FontCombo.SelectedIndex = 0; break;
                case 17: FontCombo.SelectedIndex = 1; break;
                case 19: FontCombo.SelectedIndex = 2; break;
            }

            switch (App.config.CheckTime)
            {
                case 30: NotifyCombo.SelectedIndex = 0; break;
                case 60: NotifyCombo.SelectedIndex = 1; break;
                case 180: NotifyCombo.SelectedIndex = 2; break;
                case 0: NotifyCombo.SelectedIndex = 3; break;
            }

            switch (App.config.RequestedTheme)
            {
                case 0: First.IsChecked = true; break;
                case 1: Second.IsChecked = true; break;
                case 2: Third.IsChecked = true; break;
                default: break;
            }

            canSet = true;
        }

        private void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!canSet) return;
            switch (FontCombo.SelectedIndex)
            {
                case 0: App.config.FontSize = 15; break;
                case 1: App.config.FontSize = 17; break;
                case 2: App.config.FontSize = 19; break;
            }

            Save();
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
                await (await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites")).DeleteAsync(
                    StorageDeleteOption.Default);
                Application.Current.Exit();
            }
        }

        private void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!canSet) return;
            App.config.DownloadImages = toggleSwitch.IsOn;
            Save();
        }

        private void NotifyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!canSet) return;
            switch (NotifyCombo.SelectedIndex)
            {
                case 0: App.config.CheckTime = 30; break;
                case 1: App.config.CheckTime = 60; break;
                case 2: App.config.CheckTime = 180; break;
                case 3: App.config.CheckTime = 0; break;
            }

            Save();
            BackgroundTaskManager.RegisterNotifier(App.config.CheckTime);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!canSet) return;
            App.config.RequestedTheme = 0;
            Save();
            ThemeAlert();
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            if (!canSet) return;
            App.config.RequestedTheme = 1;
            Save();
            ThemeAlert();
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            if (!canSet) return;
            App.config.RequestedTheme = 2;
            Save();
            ThemeAlert();
        }

        private async void ThemeAlert()
        {
            ResourceLoader rl = new ResourceLoader();
            MessageDialog dialog = new MessageDialog(rl.GetString("SettingsNeedRestartMessage"));
            dialog.Title = rl.GetString("SettingsNeedRestart");
            dialog.Commands.Add(new UICommand { Label = rl.GetString("SettingsNeedRestartButton"), Id = 0 });
            dialog.Commands.Add(new UICommand { Label = rl.GetString("Cancel"), Id = 1 });
            IUICommand res = await dialog.ShowAsync();
            if ((int)res.Id == 0)
            {
                Application.Current.Exit();
            }
        }

        private async void Save()
        {
            SerializerExtensions.SerializeObject(App.config, 
                await ApplicationData.Current.LocalFolder.GetFileAsync("config"));
        }

        private async void WebSiteBtn_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("http://worldbeater.github.io"));
            }
            catch
            {
            }
        }

        private async void MailMeBtn_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            EmailMessage emailMessage = new EmailMessage();
            emailMessage.To.Add(new EmailRecipient("worldbeater-dev@yandex.ru"));
            emailMessage.Subject = "myFeed Feedback";
            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        private async void FeedbackBtn_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9nblggh4nw02"));
            }
            catch
            {
            }
        }
    }
}
