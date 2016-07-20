using Windows.UI.Xaml.Controls;

namespace myFeed
{
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();
            App.CanNavigate = true;
        }
    }
}
