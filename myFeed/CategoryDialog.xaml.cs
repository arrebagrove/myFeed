using Windows.UI.Xaml.Controls;

namespace myFeed
{
    public sealed partial class CategoryDialog : ContentDialog
    {
        public CategoryDialog()
        {
            this.InitializeComponent();
        }
        
        public string CategoryName
        {
            get
            {
                return Catname.Text;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
    }
}
