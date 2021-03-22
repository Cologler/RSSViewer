namespace RSSViewer.StringMatchers
{
    public struct StringMatchArguments
    {
        public StringMatchArguments(StringMatchMode mode, string value, bool ignoreCase)
        {
            this.Mode = mode;
            this.Value = value;
            this.IgnoreCase = ignoreCase;
        }

        public StringMatchMode Mode { get; }

        public string Value { get; }

        public bool IgnoreCase { get; }
    }
}
