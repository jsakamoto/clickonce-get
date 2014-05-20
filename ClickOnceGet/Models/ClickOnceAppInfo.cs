using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ClickOnceGet.Models
{
    [DataContract]
    public class ClickOnceAppInfo
    {
        [DataMember]
        public string Name { get; set; }

        public string OwnerId { get; set; }

        [DataMember]
        public DateTime RegisteredAt { get; set; }
    }
}