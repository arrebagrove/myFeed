using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace myFeed
{
    public sealed partial class CategoryDialog : ContentDialog
    {
        public CategoryDialog()
        {
            switch (App.config.RequestedTheme)
            {
                case 1: this.RequestedTheme = ElementTheme.Light; break;
                case 2: this.RequestedTheme = ElementTheme.Dark; break;
                default: break;
            }
            this.InitializeComponent();
        }
        
        public string CategoryName
        {
            get
            {
                return Catname.Text;
            }
        }
    }
}
