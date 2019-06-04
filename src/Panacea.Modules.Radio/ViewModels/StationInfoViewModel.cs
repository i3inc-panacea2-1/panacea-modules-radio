using Panacea.Controls;
using Panacea.Modularity.UiManager;
using Panacea.Modules.Radio.Models;
using Panacea.Modules.Radio.Views;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Panacea.Modules.Radio.ViewModels
{
    [View(typeof(StationInfo))]
    class StationInfoViewModel:PopupViewModelBase<object>
    {
        public RadioItem RadioItem { get;  }
        public StationInfoViewModel(RadioItem item, RadioPlugin plugin)
        {
            RadioItem = item;
            OpenItemCommand = new RelayCommand(async args =>
            {
                SetResult(null);
                await plugin.OpenItemAsync(RadioItem);
            });
        }

        public ICommand OpenItemCommand { get; }
    }
}
