using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PFeedList : Page
    {
        private List<string> sourceslist = new List<string>();
        
        public PFeedList()
        {
            this.InitializeComponent();
            App.ChosenIndex = 3;
            App.CanNavigate = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string filecontent = await FileIO.ReadTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("sources"));
            sourceslist = filecontent.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            sourceslist.Remove(sourceslist.Last());
            foreach (string cat in sourceslist)
            {
                List<string> catlist = cat.Split(';').ToList();
                Frame frame = new Frame();
                frame.Navigate(typeof(SourcesView), catlist);
                SourcesList.Items.Add(frame);
            }
            CategoryAdd.Visibility = Visibility.Visible;
        }

        private async void CategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            CategoryDialog addcat = new CategoryDialog();
            addcat.KeyDown += (s, a) => 
            {
                if (a.Key == Windows.System.VirtualKey.Enter)
                {
                    AddNewCategory(addcat);
                    addcat.Hide();
                    a.Handled = true;
                    return;
                }
            };
            var res = await addcat.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                AddNewCategory(addcat);
                addcat.Hide();
                return;
            }
        }

        private async void AddNewCategory(CategoryDialog dialog)
        {
            string newcat = dialog.CategoryName;
            if (newcat.Contains(';')) return;
            StorageFile targetfile = await ApplicationData.Current.LocalFolder.GetFileAsync("sources");
            string filecontent = await FileIO.ReadTextAsync(targetfile);
            if (filecontent.Contains(newcat)) return;
            await FileIO.AppendTextAsync(targetfile, newcat + ";" + Environment.NewLine);
            sourceslist.Add(dialog.CategoryName + ";" + Environment.NewLine);
            List<string> catlist = sourceslist.Last().Split(';').ToList();
            Frame frame = new Frame();
            frame.Navigate(typeof(SourcesView), catlist);
            SourcesList.Items.Add(frame);
        }
    }
}
