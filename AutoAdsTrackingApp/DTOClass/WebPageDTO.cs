using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoAdsTrackingApp.Class
{
    class WebPageDTO 
    {
        public int WebPageId { get; set; }
        public string WebPageName { get; set; }
        public string HomePageAdress { get; set; }
        public string CarBrandKey { get; set; }
        public string CarModelKey { get; set; }
        public string CarYearsFromKey { get; set; }
        public string CarYearsToKey { get; set; }
        public string CarPriceFromKey { get; set; }
        public string CarPriceToKey { get; set; }
        public string CarFuelKey { get; set; }
        public string AdditionalKey { get; set; }

        public virtual List<CarBrandDTO> CarBrands { get; set; }
    }
}
