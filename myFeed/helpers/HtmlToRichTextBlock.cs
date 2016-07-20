using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace myFeed
{
    public static class HtmlToRichTextBlock
    {
        public static List<Block> GenerateBlocksForHtml(string xhtml)
        {
            List<Block> blocks = new List<Block>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(xhtml);
            Block block = GenerateParagraph(doc.DocumentNode);
            blocks.Add(block);
            return blocks;
        }

        private static Block GenerateParagraph(HtmlNode node)
        {
            Paragraph paragraph = new Paragraph();
            AddChildren(paragraph, node);
            return paragraph;
        }

        private static void AddChildren(Paragraph paragraph, HtmlNode node)
        {
            bool IsAdded = false;
            foreach (HtmlNode child in node.ChildNodes)
            {
                Inline inline = GenerateBlockForNode(child);
                if (inline != null)
                {
                    paragraph.Inlines.Add(inline);
                    IsAdded = true;
                }
            }
            if (!IsAdded) paragraph.Inlines.Add(new Run() { Text = node.InnerText });            
        }

        private static void AddChildren(Span span, HtmlNode node)
        {
            bool IsAdded = false;
            foreach (HtmlNode child in node.ChildNodes)
            {
                Inline inline = GenerateBlockForNode(child);
                if (inline != null)
                {
                    span.Inlines.Add(inline);
                    IsAdded = true;
                }
            }
            if (!IsAdded) span.Inlines.Add(new Run() { Text = node.InnerText });            
        }

        private static Inline GenerateBlockForNode(HtmlNode node)
        {
            switch (node.Name.ToLower())
            {
                case "p":
                case "div":
                    return GenerateInnerParagraph(node);
                case "a":
                    if (node.ChildNodes.Count >= 1 && (node.FirstChild.Name.ToLower() == "img")) return GenerateImage(node.FirstChild);
                    return GenerateHyperLink(node);
                case "img":
                    return GenerateImage(node);
                case "br":
                    return new LineBreak();
                case "i":
                case "em":
                case "blockquote":
                    return GenerateItalic(node);
                case "b":
                case "strong":
                    return GenerateBold(node);
                case "li":
                case "dt":
                    return GenerateLI(node);
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    return GenerateH(node, App.FontSize * 1.3);
                default:
                    return GenerateSpan(node);
            }
        }

        private static Inline GenerateLI(HtmlNode node)
        {
            Span span = new Span();
            InlineUIContainer iui = new InlineUIContainer();
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = (Windows.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["SystemControlBackgroundAccentBrush"];
            ellipse.Width = 6;
            ellipse.Height = 6;
            ellipse.Margin = new Thickness(0, 0, 9, 2);
            iui.Child = ellipse;
            span.Inlines.Add(new LineBreak());
            span.Inlines.Add(iui);
            AddChildren(span, node);
            span.Inlines.Add(new LineBreak());
            return span;
        }

        private static Inline GenerateH(HtmlNode node, double size)
        {
            Span span = new Span() { FontSize = size, FontWeight = Windows.UI.Text.FontWeights.SemiBold };
            Run run = new Run() { Text = node.InnerText };
            span.Inlines.Add(new LineBreak());
            span.Inlines.Add(run);
            span.Inlines.Add(new LineBreak());
            return span;
        }

        private static Inline GenerateItalic(HtmlNode node)
        {
            Italic italic = new Italic();
            AddChildren(italic, node);
            return italic;
        }

        private static Inline GenerateBold(HtmlNode node)
        {
            Bold bold = new Bold();
            AddChildren(bold, node);
            return bold;
        }

        private static Inline GenerateInnerParagraph(HtmlNode node)
        {
            Span span = new Span();
            if (!node.HasChildNodes && string.IsNullOrWhiteSpace(node.InnerText)) return null;
            if ((node.ChildNodes.Count == 1) && !node.FirstChild.HasChildNodes)
            {
                switch (node.FirstChild.Name.ToLower())
                {
                    case "span":
                    case "br":
                        if (string.IsNullOrWhiteSpace(node.FirstChild.InnerText)) return null;
                        break;
                    case "img":
                        AddChildren(span, node);
                        return span;
                }
            }
            
            if (node.Name.ToLower() != "div") span.Inlines.Add(new LineBreak());
            AddChildren(span, node);
            span.Inlines.Add(new LineBreak());
            return span;
        }

        private static Inline GenerateSpan(HtmlNode node)
        {
            Span span = new Span();
            AddChildren(span, node);
            return span;
        }

        private static Inline GenerateHyperLink(HtmlNode node)
        {
            Span span = new Span();
            Hyperlink link = new Hyperlink();
            try
            {
                link.NavigateUri = new Uri(node.Attributes["href"].Value, UriKind.Absolute);
            }
            catch { }
            link.Inlines.Add(new Run { Text = node.InnerText });
            span.Inlines.Add(link);
            return span;
        }

        private static Inline GenerateImage(HtmlNode node)
        {
            Span span = new Span();
            if (!App.DownloadImages) return span;
            try
            {
                InlineUIContainer iui = new InlineUIContainer();
                var sourceUri = System.Net.WebUtility.HtmlDecode(node.Attributes["src"].Value);
                Image img = new Image()
                {
                    Source = new BitmapImage(new Uri(sourceUri, UriKind.Absolute))
                };
                img.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
                img.VerticalAlignment = VerticalAlignment.Top;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Margin = new Thickness(-12, 12, -12, 12);
                img.Opacity = 0;
                img.MaxHeight = 500;

                img.ImageOpened += (sender, e) =>
                {
                    BitmapImage bmp = img.Source as BitmapImage;
                    if (bmp.PixelHeight >= bmp.PixelWidth) img.Margin = new Thickness(0, 12, 0, 12);
                    DoubleAnimation fade = new DoubleAnimation()
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.2),
                        EnableDependentAnimation = true
                    };
                    Storyboard.SetTarget(fade, img);
                    Storyboard.SetTargetProperty(fade, "Opacity");
                    Storyboard openpane = new Storyboard();
                    openpane.Children.Add(fade);
                    openpane.Begin();
                };

                img.RightTapped += (sender, e) =>
                {
                    MenuFlyout menu = new MenuFlyout();
                    Windows.ApplicationModel.Resources.ResourceLoader rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    MenuFlyoutItem menuitem = new MenuFlyoutItem() { Text = rl.GetString("OpenFullSize") };
                    menuitem.Click += async (s, o) =>
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(sourceUri));
                    };
                    menu.Items.Add(menuitem);
                    menu.ShowAt((Image)sender);
                };

                iui.Child = img;
                span.Inlines.Add(iui);
            }
            catch { }
            return span;
        }
    }
}