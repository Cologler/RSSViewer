using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Utils
{
    public class MagnetLink
    {
        public List<KeyValuePair<string, string>> QueryStrings = new();

        public IEnumerable<string> GetQueryString(string key)
        {
            return this.QueryStrings
                .Where(z => z.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                .Select(z => z.Value);
        }

        public string InfoHash
        {
            get
            {
                var xt = this.GetQueryString("xt").FirstOrDefault();
                if (xt?.StartsWith("urn:btih:") is true)
                {
                    return xt["urn:btih:".Length..];
                }
                return null;
            }
        }

        public static bool TryParse(string magnetLinkUrl, out MagnetLink magnetLink)
        {
            if (magnetLinkUrl is null)
                throw new ArgumentNullException(nameof(magnetLinkUrl));

            magnetLink = ParseInternal(magnetLinkUrl, false);
            return magnetLink is not null;
        }

        public static MagnetLink Parse(string magnetLinkUrl)
        {
            if (magnetLinkUrl is null)
                throw new ArgumentNullException(nameof(magnetLinkUrl));

            return ParseInternal(magnetLinkUrl, true);
        }

        private static MagnetLink ParseInternal(string magnetLinkUrl, bool throwIfFail)
        {
            Debug.Assert(magnetLinkUrl is not null);

            MagnetLink Throw(string message)
            {
                if (!throwIfFail)
                    return null;

                throw new MagnetLinkFormatException(message);
            }

            if (!magnetLinkUrl.StartsWith("magnet:?"))
            {
                return Throw("Magnet link should start with \"magnet:?\"");
            }

            var args = magnetLinkUrl.AsSpan().Slice("magnet:?".Length);
            var left = args;

            MagnetLink magnetLink = new();

            void AddQueryString(string key, string value)
            {
                magnetLink.QueryStrings.Add(new(key, WebUtility.UrlDecode(value)));
            }

            while (true)
            {
                var end = left.IndexOf("&");

                var part = end < 0 ? left : left.Slice(0, end);
                var sep = part.IndexOf("=");
                if (sep < 0)
                {
                    AddQueryString(part.ToString(), string.Empty);
                }
                else
                {
                    AddQueryString(part[..sep].ToString(), part[(sep + 1)..].ToString());
                }

                if (end < 0)
                {
                    break;
                }
                else
                {
                    left = left[(end + 1)..];
                } 
            }

            return magnetLink;
        }
    }
}
