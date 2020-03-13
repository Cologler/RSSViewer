using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RSSViewer.Utils
{
    public class RssItemXmlReader
    {
        private readonly XmlElement _element;

        public RssItemXmlReader(XmlElement element)
        {
            this._element = element;
        }

        private string ReadSingleNodeText(string tag)
        {
            return this._element.SelectSingleNode(tag) is XmlElement element 
                ? element.InnerText 
                : null;
        }

        public string Read(RssItemProperties property)
        {
            switch (property)
            {
                case RssItemProperties.Title: return this.ReadTitle();
                case RssItemProperties.Description: return this.ReadDescription();
                case RssItemProperties.PublishDate: return this.ReadPublishDate();
                case RssItemProperties.Link: return this.ReadLink();
                case RssItemProperties.MagnetLink: return this.ReadMagnetLink();
                case RssItemProperties.Category: return this.ReadCategory();
                default: return string.Empty;
            }
        }

        public string ReadTitle() => this.ReadSingleNodeText("title") ?? string.Empty;

        public string ReadDescription() => this.ReadSingleNodeText("description") ?? string.Empty;

        public string ReadLink() => this.ReadSingleNodeText("link") ?? string.Empty;

        public string ReadPublishDate() => this.ReadSingleNodeText("pubDate") ?? string.Empty;

        public string ReadCategory() => this.ReadSingleNodeText("category") ?? string.Empty;

        public string ReadMagnetLink()
        {
            if (this._element.SelectSingleNode("enclosure") is XmlElement element)
            {
                if (element.GetAttribute("type") == "application/x-bittorrent")
                {
                    return element.GetAttribute("url");
                }
            }

            if (this.ReadLink() is string link)
            {
                if (link.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                {
                    return link;
                }
            }

            return string.Empty;
        }

        public static RssItemXmlReader FromString(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return new RssItemXmlReader(doc.DocumentElement);
        }
    }
}
