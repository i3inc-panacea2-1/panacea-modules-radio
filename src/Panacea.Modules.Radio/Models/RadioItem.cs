using Panacea.Models;
using Panacea.Multilinguality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.Radio.Models
{
    [DataContract]
    public class RadioItem : ServerItem
    {
        [DataMember(Name = "stars")]
        public List<string> Stars { get; set; }

        public string StarsJoined
        {
            get { return Stars == null ? "" : String.Join(", ", Stars.ToArray()); }
        }

        [IsTranslatable]
        [DataMember(Name = "description")]
        public string Description
        {
            get => GetTranslation();
            set => SetTranslation(value);
        }

        [DataMember(Name = "rating")]
        public double Rating { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

    }

    [DataContract]
    public class Category : ServerGroupItem
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
