using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace myFeed
{
    public sealed partial class SearchAddDialog : ContentDialog
    {
        public SearchAddDialog()
        {
            switch (App.config.RequestedTheme)
            {
                case 1: this.RequestedTheme = ElementTheme.Light; break;
                case 2: this.RequestedTheme = ElementTheme.Dark; break;
                default: break;
            }

            this.InitializeComponent();
            this.MaxHeight = 600;
        }

        public ComboBox CategoriesComboBox
        {
            get
            {
                return CategoriesBox;
            }
        }

        public TextBox CategoriesText
        {
            get
            {
                return CategoriesTextBox;
            }
        }

        public TextBlock CategoriesTextBlock
        {
            get
            {
                return CategoriesBoxTitle;
            }
        }

        public TextBlock CategoriesTextBlockBox
        {
            get
            {
                return CategoriesTextBoxTitle;
            }
        }
    }
}
