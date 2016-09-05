using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Windows.UI.Xaml.Controls;

namespace myFeed
{
    public class Bag
    {
        public string notification;
        public List<Website> list;
        public Frame frame;
        public Frame parentframe;
    }

    [DataContract]
    public class Categories
    {
        public List<Category> categories;
    }

    [DataContract]
    public class Category
    {
        public string title;
        public List<Website> websites;
    }

    [DataContract]
    public class Website
    {
        public string url;
        public bool notify = true;
    }

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
