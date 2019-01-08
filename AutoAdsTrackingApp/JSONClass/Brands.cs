using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;

namespace AutoAdsTrackingApp.Class
{
    class Brands
    {
        IEnumerable<CarBrand> getBrands { set; get; }

        //async private void getCarBrands()
        //{

        //    var httpClient = new HttpClient();
        //    var html = await httpClient.GetStringAsync("https://www.marktplaats.nl/c/auto-s/c91.html");
        //    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
        //    htmlDocument.LoadHtml(html);

        //    var brands = htmlDocument.DocumentNode.Descendants("optgroup")
        //        .Where(node => node.GetAttributeValue("label", "")
        //        .Contains("Alle merken"))
        //        .FirstOrDefault()
        //        .ChildNodes
        //        .Where(n => n.Name.Contains("option")).ToList()
        //        .Select(n => new CarBrand
        //        {
        //            CarBrandValue = n.Attributes["value"].Value,
        //            CarBrandName = n.InnerText,
        //            WebPageId = 1
        //        });
        //    using (ScraperEntities context = new ScraperEntities())
        //    {

        //        context.CarBrands.AddRange(brands);
        //        context.SaveChanges();
        //    }
        //}

        //async private void getCarBrands()
        //{

        //    var httpClient = new HttpClient();
        //    var html = await httpClient.GetStringAsync("https://www.speurders.nl/overzicht/autos/");
        //    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
        //    htmlDocument.LoadHtml(html);

        //    var brands = htmlDocument.DocumentNode.Descendants("select")
        //        .Where(node => node.GetAttributeValue("name", "")
        //        .Contains("subcategory"))
        //        .FirstOrDefault()
        //        .ChildNodes
        //        .Where(n => n.Name.Contains("option") && n.Attributes["value"].Value != "").ToList()
        //        .Select(n => new CarBrand
        //        {
        //            CarBrandValue = n.Attributes["value"].Value,
        //            CarBrandName = n.InnerText,
        //            WebPageId = 2
        //        });
        //    using (ScraperEntities context = new ScraperEntities())
        //    {

        //        context.CarBrands.AddRange(brands);
        //        context.SaveChanges();
        //    }
        //}

        //async private void getCarBrands()
        //{

        //    var httpClient = new HttpClient();
        //    var html = await httpClient.GetStringAsync("https://www.nederlandmobiel.nl/index.php?module=uitgebreid_zoeken&voertuig=auto");
        //    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
        //    htmlDocument.LoadHtml(html);

        //    var brands = htmlDocument.DocumentNode.Descendants("select")
        //        .Where(node => node.GetAttributeValue("name", "")
        //        .Equals("merk"))
        //        .FirstOrDefault()
        //        .ChildNodes
        //        .Where(n => n.Name.Contains("option") && n.Attributes["value"].Value != "").ToList()
        //        .Select(n => new CarBrand
        //        {
        //            CarBrandValue = n.Attributes["value"].Value,
        //            CarBrandName = n.InnerText,
        //            WebPageId = 3
        //        });
        //    using (ScraperEntities context = new ScraperEntities())
        //    {

        //        context.CarBrands.AddRange(brands);
        //        context.SaveChanges();
        //    }
        //}


        //private void getModels()
        //{
        //    List<CarBrand> brandsList;
        //    using (ScraperEntities context = new ScraperEntities())
        //    {
        //        brandsList = context.CarBrands.Where(x => (x.WebPageId == 1)).ToList();
        //        context.SaveChanges();
        //    }
        //    foreach (var brand in brandsList)
        //    {
        //        using (WebClient wc = new WebClient())
        //        {
        //            var json = wc.DownloadString("https://www.marktplaats.nl/auto-s/" + brand.CarBrandValue + "/attributes.json");
        //            MarkClass model = Newtonsoft.Json.JsonConvert.DeserializeObject<MarkClass>(json);
        //            IEnumerable<CarModel> modelList;
        //            if (model.Value.ToList().Count > 1)
        //            {
        //                modelList = model.Value
        //               .ToList()[1]
        //               .Values
        //               .Select(c => { c.Id = c.Id.Substring(6); return c; })
        //               .ToList()
        //               .Select(n => new CarModel
        //               {
        //                   CarBrandId = brand.CarBrandId,
        //                   CarModelName = n.Label,
        //                   CarModelValue = n.Id
        //               });
        //                using (ScraperEntities context = new ScraperEntities())
        //                {
        //                    context.CarModels.AddRange(modelList);
        //                    context.SaveChanges();
        //                }
        //            }
        //        }
        //    }
        //}



        //private void getModels()
        //{
        //    List<CarBrand> brandsList;
        //    using (ScraperEntities context = new ScraperEntities())
        //    {
        //        brandsList = context.CarBrands.Where(x => (x.WebPageId == 2)).ToList();
        //        context.SaveChanges();
        //    }
        //    foreach (var brand in brandsList)
        //    {
        //        using (WebClient wc = new WebClient())
        //        {
        //            var json = wc.DownloadString("https://www.speurders.nl/cars/models/" + brand.CarBrandValue);
        //            try
        //            {
        //                SpeuClass model = JsonConvert.DeserializeObject<SpeuClass>(json);
        //                var modelList = model.Items
        //                    .Select(n => new CarModel
        //                    {
        //                        CarBrandId = brand.CarBrandId,
        //                        CarModelName = n.Name,
        //                        CarModelValue = n.Value
        //                    }).ToList();
        //                modelList.RemoveAt(0);

        //                using (ScraperEntities context = new ScraperEntities())
        //                {
        //                    context.CarModels.AddRange(modelList);
        //                    context.SaveChanges();
        //                }
        //            }
        //            catch { }
        //        }
        //    }
        //}
        //private void getModels()
        //{
        //    List<CarBrand> brandsList;
        //    using (ScraperEntities context = new ScraperEntities())
        //    {

        //        brandsList = context.CarBrands.Where(x => (x.WebPageId == 3)).ToList();
        //        context.SaveChanges();
        //    }
        //    foreach (var brand in brandsList)
        //    {
        //        using (WebClient wc = new WebClient())
        //        {
        //            var json = wc.DownloadString("https://www.nederlandmobiel.nl/json/model.php?merk=" + brand.CarBrandValue);
        //            var model = JsonConvert.DeserializeObject<List<NedClass>>(json)
        //                .Select(n => new CarModel
        //                {
        //                    CarBrandId = brand.CarBrandId,
        //                    CarModelName = n.Name,
        //                    CarModelValue = n.Id
        //                });

        //            using (ScraperEntities context = new ScraperEntities())
        //            {

        //                context.CarModels.AddRange(model);
        //                context.SaveChanges();
        //            }
        //        }
        //    }

        //}

    }
}
