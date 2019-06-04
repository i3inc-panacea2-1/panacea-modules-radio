using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.Radio.Models
{
    [DataContract]
    public class RadioGetSettingsResponse
    {
        [DataMember(Name = "IV")]
        public string Iv { get; set; }


        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "buttons")]
        public List<Category> Buttons { get; set; }
    }
}
