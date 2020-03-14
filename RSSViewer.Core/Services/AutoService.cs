using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using RSSViewer.StringMatchers;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace RSSViewer.Services
{
    class AutoService
    {
        private readonly IServiceProvider _serviceProvider;
        private ImmutableArray<IStringMatcher> _stringMatchers;

        public AutoService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;

            var factory = serviceProvider.GetRequiredService<StringMatcherFactory>();
            this._stringMatchers = serviceProvider.GetRequiredService<ConfigService>()
                .App.AutoReject.Matches
                .Select(z => factory.Create(z))
                .ToImmutableArray();
        }

        public void AutoReject()
        {
            var stringMatchers = this._stringMatchers;
            if (stringMatchers.Length == 0)
                return;

            var query = this._serviceProvider.GetRequiredService<RssItemsQueryService>();
            var operation = this._serviceProvider.GetRequiredService<RssItemsOperationService>();

            var items = query.List(new[] { RssItemState.Undecided });

            var shouldReject = items
                .Where(i => stringMatchers.Any(z => z.IsMatch(i.Title)))
                .ToArray();

            operation.ChangeState(shouldReject, RssItemState.Rejected);
        }
    }
}
