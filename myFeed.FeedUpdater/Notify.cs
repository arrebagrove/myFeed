using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.Web.Syndication;

namespace myFeed.FeedUpdater
{
    public sealed class Notify : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            try
            {
                string sources = await FileIO.ReadTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("sources"));
                if (string.IsNullOrWhiteSpace(sources)) return;

                List<string> sourceslist = sources.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
                sourceslist.Remove(sourceslist.Last());

                StorageFile datefile = await ApplicationData.Current.LocalFolder.GetFileAsync("datecutoff");
                DateTime cuttime = DateTime.Parse(await FileIO.ReadTextAsync(datefile));

                await FileIO.WriteTextAsync(datefile, DateTime.Now.ToString());

                List<SyndicationItem> fullfeed = new List<SyndicationItem>();
                foreach (string list in sourceslist)
                {
                    List<string> catlist = list.Split(';').ToList();
                    catlist.Remove(catlist.Last());
                    catlist.Remove(catlist.First());

                    foreach (string website in catlist)
                    {
                        Log("Sending request to " + website);
                        HttpClient hc = new HttpClient();
                        hc.DefaultRequestHeaders.Add("accept", "text/html, application/xhtml+xml, */*");
                        hc.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                        string feedstr = await hc.GetStringAsync(new Uri(website, UriKind.Absolute));
                        SyndicationFeed feed = new SyndicationFeed();
                        feed.Load(feedstr);
                        foreach (SyndicationItem item in feed.Items)
                        {
                            if (item.PublishedDate > cuttime)
                            {
                                item.Summary.Text = feed.Title.Text + ';' + website;
                                fullfeed.Add(item);
                            }
                        }
                    }
                }

                fullfeed = fullfeed.OrderBy(x => x.PublishedDate).ToList();
                Log("Sending notifications");

                foreach (SyndicationItem item in fullfeed)
                {
                    SendNotification(item);
                    await Task.Delay(100);
                }
            }
            catch
            {
                Log("Пиздец!");
            }
            
            deferral.Complete();
        }

        private void Log(string str)
        {
            Debug.WriteLine("[NOTIFY TASK] " + str);
        }

        public void SendNotification(SyndicationItem item)
        {
            try
            {
                string[] data = item.Summary.Text.Split(';');
                string notifytext = @"
                    <toast launch='" + GetTileId(item) + ';' + data[1] + @"'>
                      <visual>
                        <binding template='ToastGeneric'>
                          <text>" + item.Title.Text + @"</text>
                          <text>" + data[0] + @"</text>
                        </binding>
                      </visual>
                    </toast>";
                Windows.Data.Xml.Dom.XmlDocument xml = new Windows.Data.Xml.Dom.XmlDocument();
                xml.LoadXml(notifytext);
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(xml));
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        public static string GetTileId(SyndicationItem item)
        {
            return + (int)item.Title.Text.First()
                   + item.PublishedDate.Second.ToString()
                   + item.PublishedDate.Minute.ToString()
                   + item.PublishedDate.Hour.ToString()
                   + item.PublishedDate.Day.ToString();
        }
    }
}
