using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoAdsTrackingApp.Class
{
    class CarModelDTO
    {
        public int CarModelId { get; set; }
        public int CarBrandId { get; set; }
        public string CarModelName { get; set; }
        public string CarModelValue { get; set; }

        public virtual CarBrandDTO CarBrand { get; set; }
    }
}
