using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.Services;

namespace RSSViewer.Helpers
{
    class AddMagnetOptions : IAddMagnetOptions
    {
        private readonly ConfigService _configService;
        private readonly TrackersService _trackersService;

        public AddMagnetOptions(ConfigService configService, TrackersService trackersService)
        {
            this._configService = configService;
            this._trackersService = trackersService;
        }

        public ValueTask<bool> IsAddMagnetToQueueTopAsync() => new(this._configService.AppConf.AddToQueueTop);

        public ValueTask<string[]> GetExtraTrackersAsync() => this._trackersService.GetExtraTrackersAsync();
    }
}
