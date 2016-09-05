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
    }
}
