﻿using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.Billing;
using Panacea.Modularity.Content;
using Panacea.Modularity.Favorites;
using Panacea.Modularity.Media.Channels;
using Panacea.Modularity.MediaPlayerContainer;
using Panacea.Modularity.UiManager;
using Panacea.Modules.Radio.Models;
using Panacea.Modules.Radio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Panacea.Modules.Radio
{
    [FriendlyName("Radio")]
    public class RadioPlugin : ICallablePlugin, IUpdatesFavorites, IContentPlugin, IHasFavoritesPlugin
    {
        VTunerLazyItemProvider _provider;
        RadioListViewModel _radioList;

        public RadioPlugin(PanaceaServices core)
        {
            _core = core;
        }

        List<ServerItem> _favorites;
        private readonly PanaceaServices _core;

        public List<ServerItem> Favorites
        {
            get => _favorites;
            set
            {
                _favorites = value;
            }
        }

        public Task BeginInit()
        {
            return Task.CompletedTask;
        }

        public void Call()
        {

            if (_core.TryGetUiManager(out IUiManager ui))
            {
                _provider = _provider ?? new VTunerLazyItemProvider(_core.HttpClient, 10);
                _radioList = _radioList ?? new RadioListViewModel(_core, this, _provider);
                ui.Navigate(_radioList);
                _core.WebSocket.PopularNotifyPage("Radio");
            }
        }

        public void Dispose()
        {

        }

        public Task EndInit()
        {
            _core.WebSocket.On<SendCookieRadioResponse>("send_cookie", dyn =>
            {
                if (dyn.PluginName == "Radio")
                {

                    Favorites = dyn.Stations.Cast<ServerItem>().ToList() ?? new List<ServerItem>();

                }
            });
            _core.WebSocket.Emit("get_cookie", new { pluginName = "Radio", user = _core.UserService.User?.Id }, false);
            return Task.CompletedTask;
        }

        public async Task OpenItemAsync(ServerItem item)
        {
            if (_core.TryGetBilling(out IBillingManager billing))
            {
                if (!await billing.RequestServiceAndConsumeItemAsync("Radio requires service", "Radio", item))
                {
                    return;
                }
            }
            var radio = item as RadioItem;
            if (_core.TryGetMediaPlayerContainer(out IMediaPlayerContainer player))
            {
                player.Play(
                new MediaRequest(new IptvMedia
                {
                    URL = HttpUtility.UrlDecode(radio.Url),
                    Name = radio.Name,
                })
                {
                    ShowVideo = false,
                    AllowPip = false,
                    FullscreenMode = FullscreenMode.NoFullscreen,
                    MediaPlayerPosition = MediaPlayerPosition.Notification
                });
            }
        }

        public Task Shutdown()
        {
            return Task.CompletedTask;
        }

        public void UpdateFavorites()
        {
            _core.WebSocket.Emit("set_cookie", new { pluginName = "Radio", user = _core.UserService.User.Id, data = Favorites });
        }

        public Type GetContentType()
        {
            return typeof(RadioItem);
        }
    }
}
