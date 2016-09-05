using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.Web.Syndication;

namespace myFeed.FeedUpdater
{
    [DataContract(Namespace = "")]
    struct Categories
    {
        [DataMember]
        public List<Category> categories;
    }

    [DataContract(Namespace = "")]
    struct Category
    {
        [DataMember]
        public string title;
        [DataMember]
        public List<Website> websites;
    }

    [DataContract(Namespace = "")]
    struct Website
    {
        [DataMember(Order = 0)]
        public string url;
        [DataMember(Order = 1)]
        public string notify;
    }

    public sealed class Notify : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try
            {
                Categories cats = await DeSerialize(
                    await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                StorageFile datefile = await ApplicationData.Current.LocalFolder.GetFileAsync("datecutoff");
                DateTime cuttime = DateTime.Parse(await FileIO.ReadTextAsync(datefile));
                await FileIO.WriteTextAsync(datefile, DateTime.Now.ToString());

                List<SyndicationItem> fullfeed = new List<SyndicationItem>();
                foreach (Category cat in cats.categories)
                {
                    foreach (Website website in cat.websites)
                    {
                        if (bool.Parse(website.notify))
                        {
                            Log("Sending request to " + website.url);
                            HttpClient hc = new HttpClient();
                            hc.DefaultRequestHeaders.Add("accept", "text/html, application/xhtml+xml, */*");
                            hc.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                            string feedstr = await hc.GetStringAsync(new Uri(website.url, UriKind.Absolute));
                            SyndicationFeed feed = new SyndicationFeed();
                            feed.Load(feedstr);
                            foreach (SyndicationItem item in feed.Items)
                            {
                                if (item.PublishedDate > cuttime)
                                {
                                    item.Summary.Text = feed.Title.Text + ';' + website.url;
                                    fullfeed.Add(item);
                                }
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
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            
            deferral.Complete();
        }

        private static void Log(string str)
        {
            Debug.WriteLine("[NOTIFY TASK] " + str);
        }

        static MemoryStream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static async Task<Categories> DeSerialize(StorageFile file)
        {
            Categories cats = default(Categories);
            try
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Categories));
                string filestring = await FileIO.ReadTextAsync(file);
                using (MemoryStream ms = GenerateStreamFromString(filestring))
                {
                    cats = (Categories)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            return cats;
        }

        private void SendNotification(SyndicationItem item)
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

        private static string GetTileId(SyndicationItem item)
        {
            return + (int)item.Title.Text.First()
                   + item.PublishedDate.Second.ToString()
                   + item.PublishedDate.Minute.ToString()
                   + item.PublishedDate.Hour.ToString()
                   + item.PublishedDate.Day.ToString();
        }
    }
}
