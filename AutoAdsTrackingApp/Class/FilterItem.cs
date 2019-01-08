using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAdsTrackingApp.Class
{
    public class FilterItem
    {
        public int FilterID { get; set; }
        public int FilterIndex { get; set; }
        public string CarBrand { get; set; }
        public string CarModel { get; set; }
        public string YearsFrom { get; set; }
        public string YearsTo { get; set; }
        public string PriceFrom { get; set; }
        public string PriceTo { get; set; }
        public string FuelType { get; set; }
        public string MarkUrl { get; set; }
        public string SpeuUrl { get; set; }
        public string NedeUrl { get; set; }
        public string LastMarkAdID { get; set; }
        public string LastSpeuAdID { get; set; }
        public string LastNedeAdID { get; set; }
    }
}
