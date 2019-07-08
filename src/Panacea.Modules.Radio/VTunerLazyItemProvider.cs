using Panacea.ContentControls;
using Panacea.Core;
using Panacea.Models;
using Panacea.Modules.Radio.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Panacea.Modules.Radio
{
    public class VTunerLazyItemProvider : ILazyItemProvider, INotifyPropertyChanged
    {
        private PanaceaServices _core;
        protected string categoriesUrl;

        string search;
        private string _initvector = "";
        private string _bkey = "";
        private readonly WebClientEx _client;
        private readonly int _pageSize;
        protected string vTunerUrl = "http://dotbydotdemo.vtuner.com/setupapp/dotbydotdemo/";
        private string _token = "";
        private readonly IHttpClient _pclient;
        private string Mac = "000111000111";
        bool _loggedIn = false;

        int _currentPage;
        int _totalPages;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                if (string.IsNullOrEmpty(Search))
                {
                    GetItemsAsync();
                }
                else
                {
                    SearchAsync(Search);
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
            }
        }

        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            protected set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        string _search;
        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                CurrentPage = 1;
            }
        }

        List<ServerItem> _items;
        public List<ServerItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        Category _selectedCategory;
        public ServerGroupItem SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = (Category)value;
                OnPropertyChanged();
                if (value != null)
                {
                    TotalPages = 1;
                    CurrentPage = 1;
                }
            }
        }

        List<ServerGroupItem> _categories;
        public List<ServerGroupItem> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler Refreshed;
        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public VTunerLazyItemProvider(IHttpClient client, int pageSize)
        {
            _pclient = client;
            var firstMacAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();
            Mac = firstMacAddress.Replace("-", "");
            _client = new WebClientEx();
            _pageSize = pageSize;
        }
        private static byte[] HexToByte(String hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private static String ByteToHex(byte[] hex)
        {
            var sb = new StringBuilder();
            foreach (byte b in hex) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }


        private async Task EnsureVTunerLogin()
        {
            if (_loggedIn) return;
            var response =
                    await _pclient.GetObjectAsync<RadioGetSettingsResponse>(
                        "radio/get_settings/");

            if (response.Success)
            {
                _bkey = response.Result.Key;
                _initvector = response.Result.Iv;

                var b = new BlowFish(HexToByte(_bkey)) { IV = HexToByte(_initvector) };


                var doc = await _client.Fetch(vTunerUrl + "asp/browsexml/loginxml.asp?token=0");
                dynamic root = new ExpandoObject();
                XmlToDynamic.Parse(root, doc.Elements().First());
                var token = root.EncryptedToken;
                _token = ByteToHex(b.Decrypt_CBC(HexToByte(token)));
                var combined = Mac + _token + "0000";
                var encryptedtoken = ByteToHex(b.Encrypt_CBC(HexToByte(combined)));
                doc = await _client.Fetch(vTunerUrl + "asp/browsexml/loginxml.asp?mac=" + encryptedtoken);
                _loggedIn = true;
            }
        }

        public virtual async Task<List<ServerGroupItem>> GetCategoriesAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureVTunerLogin();
                var response =
                    await _pclient.GetObjectAsync<RadioGetSettingsResponse>(
                        "radio/get_settings/"
                        );
                return response.Result.Buttons.Cast<ServerGroupItem>().ToList();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public virtual async Task GetItemsAsync()
        {
            IsBusy = true;
            try
            {
                Items = null;
                var items = await GetItems(_selectedCategory.Url);
                if (items != null)
                    Items = items;
            }
            finally
            {
                IsBusy = false;
            }
        }

        CancellationTokenSource _cts;

        public async Task<List<ServerItem>> GetItems(string url)
        {
            CancellationTokenSource local = _cts;
            if (_cts != null)
            {
                _cts.Cancel();
            }
            _cts = new CancellationTokenSource();
            local = _cts;
            await EnsureVTunerLogin();
            if (local.IsCancellationRequested) return null;
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["startitems"] = ((_currentPage - 1) * _pageSize + 1).ToString();
            query["enditems"] = ((_currentPage - 1) * _pageSize + _pageSize).ToString();
            builder.Query = query.ToString();
            var doc = await _client.Fetch(builder.ToString());
            if (local.IsCancellationRequested) return null;
            if (doc == null)
            {
                throw new Exception("Document is null");
            }
            TotalPages = (int)Math.Ceiling(int.Parse(doc.Element("ListOfItems")?
                    .Elements("ItemCount")
                    .First().
                    Value) / (double)_pageSize);
            var dirs =
                doc.Element("ListOfItems")?
                    .Elements("Item")
                    .Where(el => el.Elements("ItemType").First().Value == "Dir")
                    .OrderBy(el => el.Element("Title")?.Value)
                    .ToList();
            var categories = dirs?.Select(dir => new Category
            {
                Name = dir.Element("Title")?.Value,
                Url = dir.Element("UrlDir")?.Value,
                ImgThumbnail = new Thumbnail
                {
                    Image = "pack://application:,,,/UserPlugins.Radio;component/resources/images/folder.png"
                }
            });


            var stations =
                doc.Element("ListOfItems")?
                    .Elements("Item")
                    .Where(el => el.Elements("ItemType").First().Value == "Station")
                    .Take(150)
                    .ToList();

            var radioItems = stations?.Select(dir =>
            {
                var item = new RadioItem
                {
                    Name = dir.Element("StationName")?.Value,
                    Rating = Double.Parse(dir.Element("Relia")?.Value ?? "0") * 2,
                    Id = dir.Element("StationId")?.Value,
                    Url = dir.Element("StationUrl")?.Value,
                    Description = dir.Element("StationDesc")?.Value,
                    ImgThumbnail = new Thumbnail
                    {
                        Image =
                            "pack://application:,,,/UserPlugins.Radio;component/resources/images/radio.png"
                    }
                };

                if (dir.Element("LogoPl") != null && !string.IsNullOrEmpty(dir.Element("LogoPl")?.Value))
                {
                    item.ImgThumbnail.Image = dir.Element("LogoPl")?.Value;
                }
                else if (dir.Element("Logo") != null && !string.IsNullOrEmpty(dir.Element("Logo")?.Value))
                {
                    item.ImgThumbnail.Image = dir.Element("LogoPl")?.Value;
                }
                return item;
            });

            var items = categories.Cast<ServerItem>().Concat(radioItems).ToList();
            return items;
        }


        public async Task SearchAsync(string wildcard)
        {
           
            try
            {
                search = wildcard;
                _cts?.Cancel();
                var source = new CancellationTokenSource();
                _cts = source;
                await Task.Delay(1000);
                IsBusy = true;
                if (source.IsCancellationRequested) return;
                Items = await GetItems(vTunerUrl + "asp/browsexml/search.asp?sSearchtype=2&search=" + HttpUtility.UrlEncode(wildcard));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Refresh()
        {
            Refreshed?.Invoke(this, EventArgs.Empty);
        }



        public async Task Initialize()
        {
            Categories = await GetCategoriesAsync();
            if (Categories.Count > 0)
            {
                SelectedCategory = Categories[0];
            }
        }
    }
}
