using Panacea.ContentControls;
using Panacea.Controls;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modularity.UiManager;
using Panacea.Modularity.UserAccount;
using Panacea.Modules.Radio.Models;
using Panacea.Modules.Radio.Views;
using Panacea.Multilinguality;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Panacea.Modules.Radio.ViewModels
{
    [View(typeof(RadioList))]
    class RadioListViewModel:ViewModelBase
    {

        public RadioListViewModel(PanaceaServices core, RadioPlugin plugin, ILazyItemProvider provider)
        {
            Provider = provider;
            //LazyCustomButtons = new ObservableCollection<ServerItem> { _favoritesItem };
            //LazyCustomCategories = plugin.Categories;

            ItemClickCommand = new RelayCommand((arg) =>
            {
                if (plugin == null) return;
                if (arg is Category)
                {
                    var c = (Category)arg;
                    provider.SelectedCategory = c;
                    //provider.GetItems(c);
                }
                else
                {
                    var c = (RadioItem)arg;
                    plugin.OpenItemAsync(c);
                }
            });
            InfoClickCommand = new RelayCommand((arg) =>
            {
                if(core.TryGetUiManager(out IUiManager ui))
                {
                    var rmp = new StationInfoViewModel(arg as RadioItem, plugin);
                    var pop = ui.ShowPopup(rmp, "", PopupType.None);
                }
               

            });

            IsFavoriteCommand = new RelayCommand((arg) =>
            {
            }, (arg) =>
            {
                var link = arg as RadioItem;
                if (plugin.Favorites == null) return false;
                return plugin.Favorites.Any(l => l.Id == link.Id);
            });

            FavoriteCommand = new RelayCommand(async (arg) =>
            {
                
                try
                {
                    if (core.TryGetUiManager(out IUiManager ui)
                    && core.TryGetUserAccountManager(out IUserAccountManager user))
                    {
                        var pluginName = "Radio";
                        if (core.UserService.User.Id == null)
                        {
                            if (await user.RequestLoginAsync(new Translator(pluginName).Translate("You need an account to add favorites")))
                            {
                                ui.Navigate(this);
                            }
                            else
                            {
                                return;
                            }
                        }
                        
                        var link = arg as RadioItem;
                        if (plugin.Favorites.Any(mm => mm.Id == link.Id))
                        {
                            if (plugin.Favorites.Any(l => l.Id == link.Id))
                                plugin.Favorites.Remove(plugin.Favorites.First(l => l.Id == link.Id));
                            ui.Toast(new Translator(pluginName).Translate("This radio station has been removed from your favorites"));
                        }
                        else
                        {
                            plugin.Favorites.Add(link);
                            ui.Toast(new Translator(pluginName).Translate("This radio station has been added to your favorites"));
                        }
                        //webSocket.Emit("set_cookie", new { pluginName = "Radio", user = userManager.User.ID, data = plugin.Favorites });
                        OnPropertyChanged("IsFavoriteCommand");
                    }
                }
                catch
                {
                }
            });

        }

        public override void Activate()
        {
            
        }

        public ILazyItemProvider Provider { get; private set; }

        private ServerItem _backItem;
        private ServerItem _favoritesItem;

        private ObservableCollection<ServerItem> _lazyCustomButtons;
        public ObservableCollection<ServerItem> LazyCustomButtons
        {
            get => _lazyCustomButtons; set
            {
                _lazyCustomButtons = value;
                OnPropertyChanged();
            }
        }
        private Brush _favoriteBtnBackground;
        public Brush FavoriteBtnBackground
        {
            get => _favoriteBtnBackground; set
            {
                _favoriteBtnBackground = value;
                OnPropertyChanged();
            }
        }

        private List<ServerGroupItem> _lazyCustomCategories;
        public List<ServerGroupItem> LazyCustomCategories
        {
            get => _lazyCustomCategories; set
            {
                _lazyCustomCategories = value;
                OnPropertyChanged();
            }
        }

        private readonly List<string> _path = new List<string>();
        private Translator _translator = new Translator("Radio");

        public ICommand InfoClickCommand { get; protected set; }
        public ICommand ItemClickCommand { get; protected set; }
        public ICommand IsFavoriteCommand { get; protected set; }
        public ICommand FavoriteCommand { get; protected set; }
    }
}
