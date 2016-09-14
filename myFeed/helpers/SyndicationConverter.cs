using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Syndication;

namespace myFeed
{
    public static class SyndicationConverter
    {
        public static async Task<SyndicationFeed> RetrieveFeedAsync(string website)
        {
            HttpClient hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("accept", "text/html, application/xhtml+xml, */*");
            hc.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            string feedstr = await hc.GetStringAsync(new Uri(website, UriKind.Absolute));
            
            SyndicationFeed feed = new SyndicationFeed();
            feed.Load(feedstr);
            return feed;
        }
    }
}
