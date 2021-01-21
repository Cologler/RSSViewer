using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Utils
{
    public class MagnetLink
    {
        public List<KeyValuePair<string, string>> QueryStrings = new();

        public IEnumerable<string> GetQueryString(string key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            return this.QueryStrings
                .Where(z => key.Equals(z.Key, StringComparison.OrdinalIgnoreCase))
                .Select(z => z.Value);
        }

        public void AddQueryString(string key, string value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            this.QueryStrings.Add(new(key, value));
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
                if (key == "tr")
                    value = WebUtility.UrlDecode(value);
                magnetLink.QueryStrings.Add(new(key, value));
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

            Debug.Assert(magnetLink.ToString() == magnetLinkUrl, $"{magnetLink} != {magnetLinkUrl}");

            return magnetLink;
        }

        public override string ToString()
        {
            return "magnet:?" + string.Join("&", this.QueryStrings.Select(z =>
            {
                if (z.Key == "tr")
                    return $"{z.Key}={WebUtility.UrlEncode(z.Value)}";
                else
                    return $"{z.Key}={z.Value}";
            }));
        }
    }
}
