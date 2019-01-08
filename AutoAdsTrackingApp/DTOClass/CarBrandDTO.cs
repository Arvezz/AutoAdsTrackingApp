using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoAdsTrackingApp.Class
{
    class CarBrandDTO 
    {       
        public int CarBrandId { get; set; }
        public int WebPageId { get; set; }
        public string CarBrandName { get; set; }
        public string CarBrandValue { get; set; }

        public virtual WebPage WebPage { get; set; }
        public virtual List<CarModelDTO> CarModels { get; set; }
    }
}
