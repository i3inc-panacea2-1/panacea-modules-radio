using Panacea.Modules.Radio.Models;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Panacea.Modules.Radio
{
    public class Node
    {
        public string Title { get; set; }
        public string URL { get; set; }
    }

    [DataContract]
    public class SendCookieRadioResponse
    {
        [DataMember(Name = "pluginName")]
        public string PluginName { get; set; }


        [DataMember(Name = "data")]
        public List<RadioItem> Stations { get; set; }
    }

    public class Station : PropertyChangedBase
    {
        private string image;
        public String Title { get; set; }

        public String Image
        {
            get { return image; }
            set
            {
                image = value;
                OnPropertyChanged("Image");
            }
        }

        public string IconFormat { get; set; }
        public string Summary { get; set; }
        public String Rating { get; set; }
    }

    public class WebClientEx : WebClient
    {
        public CookieContainer _cookieContainer = new CookieContainer();
        
        public WebClientEx()
        {
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = _cookieContainer;
                request.Timeout = 4000;
            }
            return request;
        }

        public async Task<XDocument> Fetch(string url)
        {
            var ct = new CancellationTokenSource(10000).Token;
            Encoding = Encoding.UTF8;
            var xml = await DownloadStringTaskAsync(url, ct);
            return XDocument.Parse(xml);
        }

        public async Task<string> DownloadStringTaskAsync(string address, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (cancellationToken.Register(CancelAsync))
            {
                return await DownloadStringTaskAsync(address);
            }
        }
    }

    public class WorkerObject
    {
        public WorkerObject()
        {
            IsCancelled = false;
        }

        public bool IsCancelled { get; set; }
        public Action Action { get; set; }
    }
}
