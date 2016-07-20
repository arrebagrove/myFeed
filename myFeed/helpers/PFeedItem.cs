using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace myFeed
{
    /// <summary>
    /// Контейнер, предназначенный для обмена информацией между PFeed и ItemsControlView.
    /// </summary>
    public class Bag
    {
        /// <summary>
        /// Информация от уведомления.
        /// </summary>
        public string notification;

        /// <summary>
        /// Список сайтов для категории.
        /// </summary>
        public List<string> list;

        /// <summary>
        /// Ссылка на фрейм со статьями.
        /// </summary>
        public Windows.UI.Xaml.Controls.Frame frame;
    }

    /// <summary>
    /// Контейнер, содержащий статью из канала синдикации.
    /// </summary>
    [DataContract]
    public class PFeedItem
    {
        public string title { get; set; }

        public string feed { get; set; }

        public string link { get; set; }

        public string image { get; set; }

        public string content { get; set; }
        
        public string dateoffset { get; set; }
        
        public double opacity { get; set; }

        public int iconopacity { get; set; } = 1;
        
        [XmlIgnore]
        public DateTimeOffset PublishedDate { get; set; }

        [XmlElement("PublishedDate")]
        public string SomeDateString
        {
            get
            {
                return this.PublishedDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            set
            {
                this.PublishedDate = DateTime.Parse(value);
            }
        }
        
        public string GetTileId()
        {
            return + (int)this.title.First()
                   + PublishedDate.Second.ToString()
                   + PublishedDate.Minute.ToString()
                   + PublishedDate.Hour.ToString()
                   + PublishedDate.Day.ToString();
        }
    }
}
