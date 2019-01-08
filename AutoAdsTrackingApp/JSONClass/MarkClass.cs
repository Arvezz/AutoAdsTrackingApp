using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoAdsTrackingApp.Class
{
    public class Value
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("values")]
        public List<Value1> Values { get; set; }
    }

    public class Value1
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class MarkClass
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("callback")]
        public object Callback { get; set; }
        [JsonProperty("value")]
        public List<Value> Value { get; set; }
        [JsonProperty("errors")]
        public object Errors { get; set; }
        [JsonProperty("messages")]
        public object Messages { get; set; }
    }
}
