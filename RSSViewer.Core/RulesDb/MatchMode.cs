namespace RSSViewer.RulesDb
{
    public enum MatchMode
    {
        // never match
        None = 0,

        // match string:
        Contains = 1,
        StartsWith = 2,
        EndsWith = 3,
        Wildcard = 4,
        Regex = 5,

        // match tags:
        Tags = 11,

        // match all/any items
        All = 99,
    }
}
