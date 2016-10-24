using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class PFeedList : Page
    {
        public PFeedList()
        {
            InitializeComponent();
            LoaderHelper();
            App.ChosenIndex = 3;
            App.CanNavigate = true;
        }

        private async void LoaderHelper()
        {
            await LoadContent();
            Tools.AnimateOpacity(CategoryAdd, 0, 1, 200);
            Tools.AnimateOpacity(FeedSearch, 0, 1, 200);
        }

        private async Task LoadContent()
        {
            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
            
            if (cats != null)
            {
                foreach (Category cat in cats.categories)
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(SourcesView), cat);
                    SourcesList.Items.Add(frame);
                    await Task.Delay(100);
                }
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
                }
            };

            if (await addcat.ShowAsync() == ContentDialogResult.Primary)
            {
                AddNewCategory(addcat);
                addcat.Hide();
            }
        }

        private async void AddNewCategory(CategoryDialog dialog)
        {
            Categories cats = await SerializerExtensions.DeSerializeObject<Categories>(
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            Category cat = new Category();
            cat.title = dialog.CategoryName;
            cat.websites = new List<Website>();

            foreach (Category c in cats.categories)
            {
                if (c.title == dialog.CategoryName)
                {
                    await (new MessageDialog((new ResourceLoader()).GetString("CategoryExists")).ShowAsync());
                    return;
                }
            }

            cats.categories.Add(cat);          
            SerializerExtensions.SerializeObject(cats, 
                await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

            Frame frame = new Frame();
            frame.Navigate(typeof(SourcesView), cat);
            SourcesList.Items.Add(frame);
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Search));
        }
    }
}
