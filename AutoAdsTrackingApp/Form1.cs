using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using AutoAdsTrackingApp.Class;
using System.Reflection;

namespace AutoAdsTrackingApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            loadDataToFilters();
            AdsDataGridView.AutoGenerateColumns = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AdsDataGridView.Rows.Count > 0)
            {
            }
        }

        private bool StopChecking = false;

        private void InsertFilter_Click(object sender, EventArgs e)
        {
            if (MarkComboBox.SelectedIndex != -1)
            {
                var data = new List<WebPageDTO>();
                using (ScraperEntities context = new ScraperEntities())
                {
                    var selectedModel = ModelComboBox.Text;
                    var selectedBrand = MarkComboBox.Text;

                    data = context.WebPages
                        .Select(w => new WebPageDTO
                        {
                            AdditionalKey = w.AdditionalKey,
                            WebPageId = w.WebPageId,
                            HomePageAdress = w.HomePageAdress,
                            WebPageName = w.WebPageName,
                            CarYearsToKey = w.CarYearsToKey,
                            CarYearsFromKey = w.CarYearsFromKey,
                            CarPriceToKey = w.CarPriceToKey,
                            CarPriceFromKey = w.CarPriceFromKey,
                            CarModelKey = w.CarModelKey,
                            CarFuelKey = w.CarFuelKey,
                            CarBrandKey = w.CarBrandKey,
                            CarBrands = context.CarBrands
                                .Where(x => x.CarBrandName == selectedBrand)
                                .Where(x => x.WebPageId == w.WebPageId)
                                .Select(x => new CarBrandDTO
                                {
                                    CarBrandValue = x.CarBrandValue,
                                    WebPageId = x.WebPageId,
                                    CarBrandId = x.CarBrandId,
                                    CarBrandName = x.CarBrandName,
                                    CarModels = context.CarModels
                                        .Where(a => a.CarModelName == selectedModel)
                                        .Where(s => s.CarBrandId == x.CarBrandId)
                                        .Select(d =>
                                        new CarModelDTO
                                        {
                                            CarBrandId = d.CarBrandId,
                                            CarModelValue = d.CarModelValue,
                                            CarModelName = d.CarModelName,
                                            CarModelId = d.CarModelId
                                        }).ToList()
                                }).ToList()
                        }).ToList();
                }

                var urlsList = new Dictionary<string, int>();
                foreach (var item in data)
                {
                    StringBuilder urlBuilder = new StringBuilder();
                    urlBuilder.Append(item.HomePageAdress);
                    if (!String.IsNullOrEmpty(fuelcomboBox.Text))
                        urlBuilder.Append(item.CarFuelKey);
                    urlBuilder.Append(item.WebPageId == 2 ? fuelcomboBox.Text.ToLower() : fuelcomboBox.Text);
                    urlBuilder.Append(item.CarBrandKey);
                    urlBuilder.Append(item.CarBrands.FirstOrDefault()?.CarBrandValue);
                    urlBuilder.Append(item.CarModelKey);
                    urlBuilder.Append(item.CarBrands.FirstOrDefault()?.CarModels.FirstOrDefault()?.CarModelValue);
                    urlBuilder.Append(item.CarPriceFromKey);
                    urlBuilder.Append(PriceFromTextBox.Text);
                    urlBuilder.Append(item.CarPriceToKey);
                    urlBuilder.Append(PriceToTextBox.Text);
                    urlBuilder.Append(item.CarYearsFromKey);
                    urlBuilder.Append(YearsFromTextBox.Text);
                    urlBuilder.Append(item.CarYearsToKey);
                    urlBuilder.Append(YearsToTextBox.Text);

                    urlBuilder.Replace("\r", "");
                    urlBuilder.Replace("\n", "");
                    if(item.CarBrands.Count > 0 && (item.CarBrands.FirstOrDefault()?.CarModels.Count > 0 || ( ModelComboBox.SelectedIndex == -1 || ModelComboBox.SelectedIndex == 0)))
                        urlsList.Add(urlBuilder.ToString(), item.WebPageId);
                }

                int count;
                if (AdsDataGridView.Rows.Count > 0)
                    count = Convert.ToInt32(AdsDataGridView.Rows[AdsDataGridView.Rows.Count - 1].Cells["Id"].Value.ToString()) + 1;
                else
                    count = 1;

                var filterItem = new FilterItem();
                filterItem.FilterID = count;
                filterItem.CarBrand = MarkComboBox.Text;
                filterItem.CarModel = ModelComboBox.Text;
                filterItem.YearsFrom = YearsFromTextBox.Text;
                filterItem.YearsTo = YearsToTextBox.Text;
                filterItem.PriceFrom = PriceFromTextBox.Text;
                filterItem.PriceTo = PriceToTextBox.Text;
                filterItem.FuelType = fuelcomboBox.Text;
                filterItem.MarkUrl = urlsList.Where(x => x.Value == 1).FirstOrDefault().Key;
                filterItem.SpeuUrl = urlsList.Where(x => x.Value == 2).FirstOrDefault().Key;
                filterItem.NedeUrl = urlsList.Where(x => x.Value == 3).FirstOrDefault().Key;

                var filtersList = AdsDataGridView.DataSource == null ? new BindingList<FilterItem>() :(BindingList<FilterItem>)AdsDataGridView.DataSource;
                filtersList.Add(filterItem);
                var list = new BindingList<FilterItem>(filtersList);
                AdsDataGridView.DataSource = filtersList;
            }
        }

        private async void GetAdsbutton_Click(object sender, EventArgs e)
        {
            GetAdsbutton.Enabled = false;
            StopChecking = false;
            while (!StopChecking && AdsDataGridView.Rows.Count > 0)
            {
                foreach (DataGridViewRow filter in AdsDataGridView.Rows)
                {
                    if (StopChecking) continue;

                    try
                    {
                        var filterItem = (FilterItem)filter.DataBoundItem;
                        filterItem.FilterIndex = filter.Index;
                        filter.DefaultCellStyle.BackColor = Color.Green;

                        if (!String.IsNullOrEmpty(filterItem.MarkUrl))
                        {
                            var htmlDocument = await getHtmlDocument(filterItem.MarkUrl);
                            var adsList = htmlDocument.DocumentNode.Descendants("article")
                                .Where(node => node.GetAttributeValue("class", "")
                                .Contains("row search-result defaultSnippet")
                                && node.GetAttributeValue("class", "")
                                .Contains("listing-aurora")).ToList();

                            if (adsList.Count > 0)
                            {
                                await SearchMarkAd(adsList, filterItem);

                                AddLog("Gauti skelbimai www.marktplaats.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Green);
                            }
                            else
                                AddLog("Nera skelbimų www.marktplaats.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Red);
                        }
                        if (!String.IsNullOrEmpty(filterItem.SpeuUrl))
                        {
                            var htmlDocument = await getHtmlDocument(filterItem.SpeuUrl);
                            var adsListSpeu = htmlDocument.DocumentNode.Descendants("div")
                                .Where(node => node.GetAttributeValue("class", "")
                                .Contains("box mod-bordered mod-shadow mod-no-bottom-margin mod-white")).ToList();

                            if (adsListSpeu.Count > 0)
                            {
                                await SearchSpeuAd(adsListSpeu, filterItem);

                                AddLog("Gauti skelbimai www.speurders.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Green);
                            }
                            else
                                AddLog("Nera skelbimų www.speurders.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Red);
                        }

                        if (!String.IsNullOrEmpty(filterItem.NedeUrl))
                        {
                            var htmlDocument = await getHtmlDocument(filterItem.NedeUrl);
                            var adsListNeder = htmlDocument.DocumentNode.Descendants("tr")
                                .Where(node => node.GetAttributeValue("class", "").Equals("item") ||
                                                node.GetAttributeValue("class", "").Equals("item dark")).ToList();

                            if (adsListNeder.Count > 0 )
                            {
                                await SearchNedeAd(adsListNeder, filterItem);

                                AddLog("Gauti skelbimai www.nederlandmobiel.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Green);
                            }
                            else
                                AddLog("Nera skelbimų www.nederlandmobiel.nl pagal filtrą: ", filterItem.FilterID.ToString(), Color.Red);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog("Klaida: ", ex.Message.ToString(), Color.Red);
                    }
                    filter.DefaultCellStyle.BackColor = Color.White;
                }
            }
            GetAdsbutton.Enabled = true;
        }

        public async Task SearchNedeAd(List<HtmlNode> adsList, FilterItem filter)
        {
            var firstAdID = "";
            if (filter.LastNedeAdID != null)
            {
                var existAd = false;
                foreach (var ad in adsList)
                {
                    var adID = ad.GetAttributeValue("onclick", "").ToString();
                    adID = adID.Substring(0, adID.LastIndexOf("search&") + 7);
                    if (adID == filter.LastNedeAdID)
                    {
                        existAd = true;
                        break;
                    }
                }
                if (!existAd)
                {
                    AddLog("Nerastas skelbimas ", "Nederland " + filter.LastNedeAdID, Color.Red);
                    filter.LastNedeAdID = "";
                }
            }
            foreach (var ad in adsList)
            {
                var adID = ad.GetAttributeValue("onclick", "").ToString();
                adID = adID.Substring(0, adID.LastIndexOf("search&") + 7);

                firstAdID = firstAdID == "" ? adID : firstAdID;

                if (String.IsNullOrEmpty(filter.LastNedeAdID))
                {
                    AdsDataGridView.Rows[filter.FilterIndex].Cells[14].Value = adID;
                    break;
                }
                else
                {
                    if (adID == filter.LastNedeAdID)
                        break;
                    else
                    {
                        //PlaySignal();
                        var name = ad.Descendants("a").ToList()[0].InnerText.ToString();

                        var price = ad.Descendants("strong").ToList()[0]
                            .InnerText.ToString().Replace(" ", "").Replace("&nbsp;", " ").Replace("&euro;", "€").Replace("\n", "").Replace("&#8364;", "€ ");

                        var address = ad.Descendants("a").ToList()[0].GetAttributeValue("href", "").ToString();

                        var htmlDocument = await getHtmlDocument(address);

                        var imgNodeList = htmlDocument.DocumentNode.Descendants("img")
                            .Where(node => node.GetAttributeValue("id", "")
                            .Contains("thumb_")).ToList();

                        List<Bitmap> imgList = new List<Bitmap>();
                        for (int x = 0; x < 3; x++)
                        {
                            string imgUrl;
                            if (imgNodeList.Count > x)
                                imgUrl = imgNodeList[x].GetAttributeValue("src", "").ToString();
                            else
                                imgUrl = "https://static-speurders.nl/static/img/nophoto_100.png";
                            imgUrl = FormatImgUrl(imgUrl);
                            var bmp = GetBmp(imgUrl);
                            imgList.Add(bmp);
                        }

                        NewestListdataGridView.Rows.Add(filter.FilterID, name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
                        NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
                    }
                }
            }
            AdsDataGridView.Rows[filter.FilterIndex].Cells[14].Value = firstAdID;
        }

        public async Task SearchSpeuAd(List<HtmlNode> adsList, FilterItem filter)
        {
            var firstAdID = "";
            if (filter.LastSpeuAdID != null)
            {
                var existAd = false;
                foreach (var ad in adsList)
                {
                    var adID = ad.Descendants("button")
                        .Where(node => node.GetAttributeValue("class", "")
                        .Equals("ripple-btn save-bookmark-button save-bookmark js-redesign mod-yellow"))
                        .ToList()[0].GetAttributeValue("data-ad-id", "").ToString();

                    if (adID == filter.LastSpeuAdID)
                    {
                        existAd = true;
                        break;
                    }
                }
                if (!existAd)
                {
                    AddLog("Nerastas skelbimas ", "Spla " + filter.LastSpeuAdID, Color.Red);
                    filter.LastSpeuAdID = "";
                }
            }
            foreach (var ad in adsList)
            {
                var topperCount = ad.Descendants("span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("label mod-uppercase mod-yellow")).Count();

                if (topperCount == 0)
                {
                    var adID = ad.Descendants("button")
                        .Where(node => node.GetAttributeValue("class", "")
                        .Equals("ripple-btn save-bookmark-button save-bookmark js-redesign mod-yellow"))
                        .ToList()[0].GetAttributeValue("data-ad-id", "").ToString();

                    firstAdID = firstAdID == "" ? adID : firstAdID;

                    if (String.IsNullOrEmpty(filter.LastSpeuAdID))
                    {
                        filter.LastSpeuAdID = adID;
                        break;
                    }
                    else
                    {
                        if (adID == filter.LastSpeuAdID)
                            break;
                        else
                        {
                            //PlaySignal();
                            var name = ad.Descendants("h2").ElementAt(1).InnerText.ToString();

                            var price = ad.Descendants("h2").ElementAt(0).InnerText.ToString();
                            price = price.Replace("&nbsp;", " ").Replace("&euro;", "€").Replace(" ", "").Replace("\n", "");

                            var address = "https://www.speurders.nl/" + ad.Descendants("a").ToList()[0].GetAttributeValue("href", "").ToString();

                            var htmlDocument = await getHtmlDocument(address);

                            var imgNodeList = htmlDocument.DocumentNode.Descendants("div")
                                .Where(node => node.GetAttributeValue("class", "")
                                .Equals("image-cover mod-medium-zoomed")).ToList();

                            List<Bitmap> imgList = new List<Bitmap>();
                            for (int x = 0; x < 3; x++)
                            {
                                string imgUrl;
                                if (imgNodeList.Count > x)
                                    imgUrl = imgNodeList[x].GetAttributeValue("style", "").ToString();
                                else
                                    imgUrl = "https://static-speurders.nl/static/img/nophoto_100.png";
                                imgUrl = FormatImgUrl(imgUrl);
                                var bmp = GetBmp(imgUrl);
                                imgList.Add(bmp);
                            }

                            NewestListdataGridView.Rows.Add(filter.FilterID, name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
                            NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
                        }
                    }
                }
            }
            AdsDataGridView.Rows[filter.FilterIndex].Cells[13].Value = firstAdID;
        }

        public async Task SearchMarkAd(List<HtmlNode> adsList, FilterItem filter)
        {
            var firstAdID = "";
            if (filter.LastMarkAdID != null)
            {
                var existAd = false;
                foreach (var ad in adsList)
                {
                    var adID = ad.GetAttributeValue("data-item-id", "").ToString();
                    if (adID == filter.LastMarkAdID)
                    {
                        existAd = true;
                        break;
                    }
                }
                if (!existAd)
                {
                    AddLog("Nerastas skelbimas ", "Mark " + filter.LastMarkAdID, Color.Red);
                    filter.LastMarkAdID = "";
                }
            }

            foreach (var ad in adsList)
            {
                var topString = ad.Descendants("span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("mp-listing-priority-product")).ToList()[0]
                    .InnerText.ToString();

                if (String.IsNullOrEmpty(topString))
                {
                    var adID = ad.GetAttributeValue("data-item-id", "").ToString();
                    firstAdID = String.IsNullOrEmpty(firstAdID) ? adID : firstAdID;

                    if (String.IsNullOrEmpty(filter.LastMarkAdID))
                    {
                        filter.LastMarkAdID = adID;
                        break;
                    }
                    else
                    {
                        if (adID == filter.LastMarkAdID)
                            break;
                        else
                        {
                            //PlaySignal();
                            var h2List = ad.Descendants("span")
                                .Where(node => node.GetAttributeValue("class", "")
                                .Equals("mp-listing-title")).ToList();

                            var name = h2List[0].InnerText.ToString();

                            var price = ad.Descendants("span")
                                .Where(node => node.GetAttributeValue("class", "")
                                .Equals("price-new")).ToList()[0]
                                .InnerText.ToString().Replace(" ", "").Replace("&nbsp;", " ").Replace("&euro;", "€").Replace("\n", "");

                            var address = ad.Descendants("a").ToList()[0]
                                .GetAttributeValue("href", "").ToString();

                            var htmlDocument = await getHtmlDocument(address);

                            var imgsUrls = htmlDocument.DocumentNode.Descendants("div")
                                .Where(node => node.GetAttributeValue("id", "")
                                .Equals("vip-carousel")).ToList()[0]
                                .GetAttributeValue("data-images-s", "").ToString();

                            char[] charSeparators = new char[] { ',' };
                            var imgUrls = imgsUrls.Split(new char[] { '&' }, 3, StringSplitOptions.None).ToList();

                            List<Bitmap> imgList = new List<Bitmap>();
                            for (int x = 0; x < 3; x++)
                            {
                                string imgUrl;
                                if (imgUrls.Count > x)
                                    imgUrl = "https:" + imgUrls[x];
                                else
                                    imgUrl = "https://static-speurders.nl/static/img/nophoto_100.png";

                                imgUrl = FormatImgUrl(imgUrl);
                                var bmp = GetBmp(imgUrl);
                                imgList.Add(bmp);
                            }

                            NewestListdataGridView.Rows.Add(filter.FilterID, name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
                            NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
                        }
                    }
                }
            }
            AdsDataGridView.Rows[filter.FilterIndex].Cells[12].Value = firstAdID;
        }

        private async Task<HtmlAgilityPack.HtmlDocument> getHtmlDocument(string url)
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);
            return htmlDocument;
        }

        //public async Task DoWorkNeder(List<HtmlNode> adsList, DataGridViewRow filter)
        //{
        //    var firstAdID = "";
        //    var lastAdID = "";
        //    var existAd = false;
        //    if (AdsDataGridView.Rows[filter.Index].Cells[14].Value != null)
        //    {
        //        lastAdID = AdsDataGridView.Rows[filter.Index].Cells[14].Value.ToString();
        //        foreach (var ad in adsList)
        //        {
        //            var adID = ad.GetAttributeValue("onclick", "").ToString();
        //            adID = adID.Substring(0,adID.LastIndexOf("search&") + 7);
        //            if (adID == lastAdID)
        //            {
        //                existAd = true;
        //                break;
        //            }
        //        }
        //        if (!existAd)
        //        {
        //            AddLog("Nerastas skelbimas ", "Nederland " + lastAdID, Color.Red);
        //            lastAdID = "";
        //        }
        //    }
        //    foreach (var ad in adsList)
        //    {
        //        var adID = ad.GetAttributeValue("onclick", "").ToString();
        //        adID = adID.Substring(0, adID.LastIndexOf("search&") + 7);

        //           firstAdID = firstAdID == "" ? adID : firstAdID;

        //            if (lastAdID == "")
        //            {
        //                AdsDataGridView.Rows[filter.Index].Cells[14].Value = adID;
        //                break;
        //            }
        //            else
        //            {
        //                var currSpeuName = adID;
        //                if (currSpeuName == lastAdID)
        //                    break;
        //                else
        //                {
        //                //PlaySignal();
        //                var h2List = ad.Descendants("a").ToList();
        //                var name = h2List[0].InnerText.ToString();

        //                var priceNode = ad.Descendants("strong").ToList();
        //                var price = priceNode[0].InnerText.ToString().Replace(" ", "").Replace("&nbsp;", " ").Replace("&euro;", "€").Replace("\n", "").Replace("&#8364;", "€ ");

        //                var nodesList = ad.Descendants("a").ToList();
        //                var address = nodesList[0].GetAttributeValue("href", "").ToString();

        //                var htmlDocument = await getHtmlDocument(address);

        //                var imgNodeList = htmlDocument.DocumentNode.Descendants("img")
        //                    .Where(node => node.GetAttributeValue("id", "")
        //                    .Contains("thumb_")).ToList();

        //                var imgUrl = "";
        //                var bmp = GetBmp("https://static-speurders.nl/static/img/nophoto_100.png");
        //                List<Bitmap> imgList = new List<Bitmap>();
        //                imgList.Add(bmp);
        //                imgList.Add(bmp);
        //                imgList.Add(bmp);

        //                foreach (var imgNode in imgNodeList.Select((x, i) => new { Value = x, Index = i }))
        //                {
        //                    imgUrl = imgNode.Value.GetAttributeValue("src", "").ToString();
        //                    imgUrl = FormatImgUrl(imgUrl);
        //                    bmp = GetBmp(imgUrl);
        //                    imgList.Insert(imgNode.Index, bmp);
        //                }

        //                NewestListdataGridView.Rows.Add(filter.Cells[0].Value.ToString(), name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
        //                NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
        //            }
        //            }
        //    }
        //    AdsDataGridView.Rows[filter.Index].Cells[14].Value = firstAdID;
        //}

        //public async Task DoWorkMark(List<HtmlNode> adsList, DataGridViewRow filter)
        //{
        //    var firstAdID = "";
        //    var lastAdID = "";
        //    var existAd = false;
        //    if (AdsDataGridView.Rows[filter.Index].Cells[12].Value != null)
        //    {
        //        lastAdID = AdsDataGridView.Rows[filter.Index].Cells[12].Value.ToString();
        //        foreach (var ad in adsList)
        //        {
        //            var adID = ad.GetAttributeValue("data-item-id", "").ToString();
        //            if (adID == lastAdID)
        //            {
        //                existAd = true;
        //                break;
        //            }
        //        }
        //        if (!existAd)
        //        {
        //            AddLog("Nerastas skelbimas ", "Mark " + lastAdID, Color.Red);
        //            lastAdID = "";
        //        }
        //    }
        //    foreach (var ad in adsList)
        //    {
        //        var top = ad.Descendants("span")
        //            .Where(node => node.GetAttributeValue("class", "")
        //            .Equals("mp-listing-priority-product")).ToList();
        //        var topString = top[0].InnerText.ToString();

        //        if (topString == "")
        //        {
        //            var adID = ad.GetAttributeValue("data-item-id", "").ToString();

        //            firstAdID = firstAdID == "" ? adID : firstAdID;

        //            if (lastAdID == "")
        //            {
        //                AdsDataGridView.Rows[filter.Index].Cells[12].Value = adID;
        //                break;
        //            }
        //            else
        //            {
        //                var currSpeuName = adID;
        //                if (currSpeuName == lastAdID)
        //                    break;
        //                else
        //                {
        //                    //PlaySignal();
        //                    var h2List = ad.Descendants("span")
        //                        .Where(node => node.GetAttributeValue("class", "")
        //                        .Equals("mp-listing-title")).ToList();

        //                    var name = h2List[0].InnerText.ToString();

        //                    var priceNode = ad.Descendants("span")
        //                        .Where(node => node.GetAttributeValue("class", "")
        //                        .Equals("price-new")).ToList();
        //                    var price = priceNode[0].InnerText.ToString().Replace(" ", "").Replace("&nbsp;", " ").Replace("&euro;", "€").Replace("\n", "");

        //                    var nodesList = ad.Descendants("a").ToList();
        //                    var address = nodesList[0].GetAttributeValue("href", "").ToString();

        //                    var htmlDocument = await getHtmlDocument(address);

        //                    var imgNodeList = htmlDocument.DocumentNode.Descendants("div")
        //                        .Where(node => node.GetAttributeValue("id", "")
        //                        .Equals("vip-carousel")).ToList();

        //                    var imgsUrls = imgNodeList[0].GetAttributeValue("data-images-s", "").ToString();

        //                    var imgUrl = "";
        //                    var bmp = GetBmp("https://static-speurders.nl/static/img/nophoto_100.png");
        //                    List<Bitmap> imgList = new List<Bitmap>();
        //                    imgList.Add(bmp);
        //                    imgList.Add(bmp);
        //                    imgList.Add(bmp);

        //                    char[] charSeparators = new char[] { ',' };
        //                    var imgUrls = imgsUrls.Split( new char[] {'&'} , 3, StringSplitOptions.None).ToList();

        //                    foreach (var url in imgUrls.Select((x, i) => new { Value = x, Index = i }))
        //                    {
        //                        imgUrl = "https:" + url.Value;
        //                        imgUrl = FormatImgUrl(imgUrl);
        //                        bmp = GetBmp(imgUrl);
        //                        imgList.Insert(url.Index, bmp);
        //                    }

        //                    NewestListdataGridView.Rows.Add(filter.Cells[0].Value.ToString(), name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
        //                    NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
        //                }
        //            }
        //        }
        //    }
        //    AdsDataGridView.Rows[filter.Index].Cells[12].Value = firstAdID;
        //}

        //public async Task DoWorkSpeu(List<HtmlNode> adsList, DataGridViewRow filter)
        //{
        //    var firstAdID = "";
        //    var lastAdID = "";
        //    var existAd = false;
        //    if (AdsDataGridView.Rows[filter.Index].Cells[13].Value != null)
        //    {
        //        lastAdID = AdsDataGridView.Rows[filter.Index].Cells[13].Value.ToString();
        //        foreach (var ad in adsList)
        //        {
        //            var h2List = ad.Descendants("button")
        //                .Where(node => node.GetAttributeValue("class", "")
        //                .Equals("ripple-btn save-bookmark-button save-bookmark js-redesign mod-yellow")).ToList();

        //            var adID = h2List[0].GetAttributeValue("data-ad-id", "").ToString();

        //            if (adID == lastAdID)
        //            {
        //                existAd = true;
        //                break;
        //            }
        //        }
        //        if (!existAd)
        //        {
        //            AddLog("Nerastas skelbimas ", "Spla " + lastAdID, Color.Red);
        //            lastAdID = "";
        //        }
        //    }
        //    foreach (var ad in adsList)
        //    {

        //        var topperCount = ad.Descendants("span")
        //            .Where(node => node.GetAttributeValue("class", "")
        //            .Equals("label mod-uppercase mod-yellow")).Count();

        //        if (topperCount == 0)
        //        {
        //            var h2List = ad.Descendants("button")
        //                .Where(node => node.GetAttributeValue("class","")
        //                .Equals("ripple-btn save-bookmark-button save-bookmark js-redesign mod-yellow")).ToList();

        //            var adID = h2List[0].GetAttributeValue("data-ad-id", "").ToString();

        //            firstAdID = firstAdID == "" ? adID : firstAdID;

        //            if (lastAdID == "")
        //            {
        //                AdsDataGridView.Rows[filter.Index].Cells[13].Value = adID;
        //                break;
        //            }
        //            else
        //            {
        //                var currSpeuName = adID;
        //                if (currSpeuName == lastAdID)
        //                    break;
        //                else
        //                {
        //                    //PlaySignal();
        //                    var name = ad.Descendants("h2").ElementAt(1).InnerText.ToString();

        //                    var price = ad.Descendants("h2").ElementAt(0).InnerText.ToString();
        //                    price = price.Replace("&nbsp;", " ").Replace("&euro;", "€").Replace(" ","").Replace("\n", "");

        //                    var nodesList = ad.Descendants("a").ToList();
        //                    var address = "https://www.speurders.nl/" + nodesList[0].GetAttributeValue("href", "").ToString();

        //                    var htmlDocument = await getHtmlDocument(address);

        //                    var imgNodeList = htmlDocument.DocumentNode.Descendants("div")
        //                        .Where(node => node.GetAttributeValue("class", "")
        //                        .Equals("image-cover mod-medium-zoomed")).ToList();

        //                    var imgUrl = "";
        //                    var bmp = GetBmp("https://static-speurders.nl/static/img/nophoto_100.png");
        //                    List<Bitmap> imgList = new List<Bitmap>();
        //                    imgList.Add(bmp);
        //                    imgList.Add(bmp);
        //                    imgList.Add(bmp);

        //                    foreach (var imgNode in imgNodeList.Select((x, i) => new { Value = x, Index = i }))
        //                    {
        //                        imgUrl = imgNode.Value.GetAttributeValue("style", "").ToString();
        //                        imgUrl = FormatImgUrl(imgUrl);
        //                        bmp = GetBmp(imgUrl);
        //                        imgList.Insert(imgNode.Index, bmp);
        //                    }

        //                    NewestListdataGridView.Rows.Add(filter.Cells[0].Value.ToString(), name, price, DateTime.Now, imgList[0], imgList[1], imgList[2], address);
        //                    NewestListdataGridView.Sort(NewestListdataGridView.Columns[3], ListSortDirection.Descending);
        //                }
        //            }
        //        }
        //    }
        //    AdsDataGridView.Rows[filter.Index].Cells[13].Value = firstAdID;
        //}

        private void PlaySignal()
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer("signals.wav");
            player.Play();
        }

        private string FormatImgUrl(string imgUrl)
        {
            return imgUrl.Replace("background-image: url('", "https:").Replace("')", "");
        }

        private Bitmap GetBmp(string imgUrl)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(imgUrl);
            myRequest.Method = "GET";
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            var bmp = new Bitmap(myResponse.GetResponseStream());
            bmp = (Bitmap)bmp.GetThumbnailImage(200, 150, null, IntPtr.Zero);
            myResponse.Close();
            return bmp;
        }

        private void NewestListdataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == NewestListdataGridView.Columns["DeleteAd"].Index)
            {
                NewestListdataGridView.Rows[e.RowIndex].Cells[3].Value = new DateTime();
                NewestListdataGridView.Rows[e.RowIndex].Visible = false;
            }
            else if (e.ColumnIndex == NewestListdataGridView.Columns["AutoUrl"].Index)
            {
                var url = NewestListdataGridView.Rows[e.RowIndex].Cells[7].Value.ToString();
                Process.Start(url);
            }
        }

        private void AdsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == AdsDataGridView.Columns["DeleteFilterBtn"].Index)
            {
                AdsDataGridView.Rows.RemoveAt(e.RowIndex);
            }
            else if (e.ColumnIndex == AdsDataGridView.Columns["Url"].Index)
            {
                var url = AdsDataGridView.Rows[e.RowIndex].Cells[8].Value.ToString();
                Process.Start(url);
            }
            else if (e.ColumnIndex == AdsDataGridView.Columns["Speurders"].Index)
            {
                var url = AdsDataGridView.Rows[e.RowIndex].Cells[9].Value.ToString();
                Process.Start(url);
            }
            else if (e.ColumnIndex == AdsDataGridView.Columns["Nederlandmobiel"].Index)
            {
                var url = AdsDataGridView.Rows[e.RowIndex].Cells[10].Value.ToString();
                Process.Start(url);
            }
        }

        private void StopCheckAds_Click(object sender, EventArgs e)
        {
            StopChecking = true;
            GetAdsbutton.Enabled = true;
        }

        public void AddLog(string text, string errorMessage, Color color)
        {
            int length = 0;
            LogTextBox.Invoke(new Action(() => length = LogTextBox.TextLength));
            string logMessage = text + errorMessage;
            LogTextBox.Invoke(new Action(() => LogTextBox.AppendText(text)));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionStart = length));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionLength = text.Length));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionColor = color));

            LogTextBox.Invoke(new Action(() => length = LogTextBox.TextLength));
            LogTextBox.Invoke(new Action(() => LogTextBox.AppendText(errorMessage + "\n")));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionStart = length));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionLength = errorMessage.Length));
            LogTextBox.Invoke(new Action(() => LogTextBox.SelectionColor = Color.Black));
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adspanel.Visible = false;
            addPanel.Visible = true;
            LogsPanel.Visible = false;
            if(markcheckedListBox.Items.Count == 0)
                loadAutoData();
        }

        private void loadAutoData()
        {
            addBrandsToCheckBox(markcheckedListBox, 1);
            addBrandsToCheckBox(speucheckedListBox, 2);
            addBrandsToCheckBox(nedcheckedListBox, 3);
            marknamecomboBox.Items.Clear();
            marknamecomboBox.ResetText();
            addModelsToCheckBox(checkedListBoxMarkModel, 1);
            addModelsToCheckBox(checkedListBoxSpeuModel, 2);
            addModelsToCheckBox(checkedListBoxNedModel, 3);
            comboBoxModel.Items.Clear();
            comboBoxModel.ResetText();
        }

        private void addModelsToCheckBox(CheckedListBox checkBox, int webId)
        {
            using (ScraperEntities context = new ScraperEntities())
            {
                var markModelsAll = context.CarModels
                    .Join(context.CarBrands, x => x.CarBrandId, c => c.CarBrandId,
                    (x, c) => new { CarModelName = x.CarModelName, CarBrandName = c.CarBrandName, c.WebPageId})
                    .GroupBy(c => new { c.CarModelName, c.CarBrandName })
                    .Where(x => x.Count() == 3)
                    .Select(x => x.Key)
                    .Select(x => x.CarModelName)
                    .ToList();

                var markModels = context.CarModels
                   .Join(context.CarBrands, x => x.CarBrandId, c => c.CarBrandId,
                   (x, c) => new  { CarFullName = c.CarBrandName + " " + x.CarModelName, CarBrandName = c.CarBrandName, CarModelName = x.CarModelName, CarModelId = x.CarModelId, WebPageId = c.WebPageId })
                   .Where(p => !markModelsAll
                   .Any(p2 => p2 == p.CarModelName) && p.WebPageId == webId)
                   .OrderBy(x => x.CarFullName)
                   .ToList();

                //checkBox.DataSource = null;
                //checkBox.DataSource = markModels;
                //checkBox.DisplayMember = "CarFullName";
                //checkBox.ValueMember = "CarModelId";
                //return markModels;
            }
        }

        //public async Task<string> BindFileGridAsync()
        //{
        //    this.radGridViewFiles.DataSource = await GetMyDatasourceAsync();
        //    return "I am finished";
        //}

        private void addBrandsToCheckBox(CheckedListBox checkBox, int webId)
        {
            using (ScraperEntities context = new ScraperEntities())
            {
                var markBrandsAll = context.CarBrands
                    .GroupBy(c => c.CarBrandName)
                    .Where(x => x.Count() == 3)
                    .Select(x => x.Key)
                    .ToList();

                 var markBrands = context.CarBrands
                    .Where(p => !markBrandsAll
                    .Any(p2 => p2 == p.CarBrandName) && p.WebPageId==webId)
                    .OrderBy(x => x.CarBrandName)
                    .ToList();

                checkBox.DataSource = null;
                checkBox.DataSource = markBrands;
                checkBox.DisplayMember = "CarBrandName";
                checkBox.ValueMember = "CarBrandId";
            }
        }

        private void skelbimaiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addPanel.Visible = false;
            adspanel.Visible = true;
            LogsPanel.Visible = false;
        }

        private void checkedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var checkBox = (CheckedListBox)sender;
            deleteFromComboBox(checkBox, e.Index);
            unchekItem(checkBox, e);
        }

        private void unchekItem(CheckedListBox checkBox, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                for (int ix = 0; ix < checkBox.Items.Count; ++ix)
                    if (e.Index != ix) checkBox.SetItemChecked(ix, false);
        }

        private void deleteFromComboBox(CheckedListBox checkBox, int index)
        {
            if (checkBox.CheckedItems.Count > 0)
            {
                var brand = (CarBrand)checkBox.Items[index];
                marknamecomboBox.Items.Remove(new { Text = brand.CarBrandName, Value = brand.CarBrandId });
            }
        }

        private void addToComboBox(CheckedListBox checkBox)
        {
            if (checkBox.CheckedItems.Count > 0)
            {
                var brand = (CarBrand)checkBox.CheckedItems[0];
                marknamecomboBox.DisplayMember = "Text";
                marknamecomboBox.ValueMember = "Value";
                marknamecomboBox.Items.Add(new { Text = brand.CarBrandName, Value = brand.CarBrandId });
            }
        }

        private void checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var checkBox = (CheckedListBox)sender;
            addToComboBox(checkBox);
        }

        private void joinbutton_Click(object sender, EventArgs e)
        {
            if(marknamecomboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Privaloma pasirinkti pavadinimą!");
                return;
            }
            updateBrandName(markcheckedListBox);
            updateBrandName(speucheckedListBox);
            updateBrandName(nedcheckedListBox);
        }

        private void updateBrandName(CheckedListBox checkBox)
        {
            using (ScraperEntities context = new ScraperEntities())
            {
                if (checkBox.CheckedItems.Count > 0)
                {
                    var brand = (CarBrand)checkBox.CheckedItems[0];
                    var record = context.CarBrands.Where(d => d.CarBrandId == brand.CarBrandId).First();
                    record.CarBrandName = marknamecomboBox.Text;
                    context.SaveChanges();
                    loadAutoData();
                }
            }
        }

        private void checkedListBoxMarkModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            var checkBox = (CheckedListBox)sender;
            addModelToComboBox(checkBox);
            checkIfNotSame();
        }

        private void checkIfNotSame()
        {
            var list = new List<string>();
            if (checkedListBoxMarkModel.CheckedItems.Count > 0)
                list.Add((string)getPropertyValue((object)checkedListBoxMarkModel.CheckedItems[0], "CarBrandName"));
            if (checkedListBoxSpeuModel.CheckedItems.Count > 0)
                list.Add((string)getPropertyValue((object)checkedListBoxSpeuModel.CheckedItems[0], "CarBrandName"));
            if (checkedListBoxNedModel.CheckedItems.Count > 0)
                list.Add((string)getPropertyValue((object)checkedListBoxNedModel.CheckedItems[0], "CarBrandName"));

            var same = list.Where(x => !string.IsNullOrEmpty(x))
                      .Distinct()
                      .Skip(1)
                      .Any();
            if (same)
                MessageBox.Show("Markių pavadinimai turi sutapti!");
            buttonJoinModel.Enabled = !same;
        }

        private void addModelToComboBox(CheckedListBox checkBox)
        {
            if (checkBox.CheckedItems.Count > 0)
            {
                var checkedItem = checkBox.CheckedItems[0];

                comboBoxModel.DisplayMember = "Text";
                comboBoxModel.ValueMember = "Value";
                comboBoxModel.Items.Add(new
                {
                    Text = getPropertyValue(checkedItem, "CarModelName"),
                    Value = getPropertyValue(checkedItem, "CarModelId")
                });
            }
        }

        public object getPropertyValue(object obj, string objName)
        {
            return obj.GetType().GetProperty(objName).GetValue(obj, null);
        }

        private void checkedListBoxMarkModel_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var checkBox = (CheckedListBox)sender;
            deleteModelFromComboBox(checkBox, e.Index);
            unchekItem(checkBox, e);
        }

        private void deleteModelFromComboBox(CheckedListBox checkBox, int index)
        {
            if (checkBox.CheckedItems.Count > 0)
            {
                var item = checkBox.Items[index];
                comboBoxModel.Items.Remove(new
                {
                    Text = getPropertyValue(item, "CarModelName"),
                    Value = getPropertyValue(item, "CarModelId")
                });
            }
        }

        private void buttonJoinModel_Click(object sender, EventArgs e)
        {
            if (comboBoxModel.SelectedIndex == -1)
            {
                MessageBox.Show("Privaloma pasirinkti pavadinimą!");
                return;
            }

            updateModelName(checkedListBoxMarkModel);
            updateModelName(checkedListBoxSpeuModel);
            updateModelName(checkedListBoxNedModel);
        }

        private void updateModelName(CheckedListBox checkBox)
        {
            using (ScraperEntities context = new ScraperEntities())
            {
                if (checkBox.CheckedItems.Count > 0)
                {
                    var checkedItem = checkBox.CheckedItems[0];
                    var record = context.CarModels.Where(d => d.CarModelId == (int)getPropertyValue(checkedItem, "CarModelId")).First();
                    record.CarModelName = comboBoxModel.Text;
                    context.SaveChanges();
                    loadAutoData();
                }
            }
        }

        private void loadDataToFilters()
        {
            using (ScraperEntities context = new ScraperEntities())
            {
                var brands = context.CarBrands
                    .GroupBy(c => c.CarBrandName)
                    .Select(x => new { value = x.Key, count = x.Count() })
                    .OrderBy(x => x.value)
                    .OrderByDescending(x => x.count)
                    .ToList();

                MarkComboBox.DataSource = brands;
            }

            fuelcomboBox.Items.Add("");
            fuelcomboBox.Items.Add("Benzine");
            fuelcomboBox.Items.Add("Diesel");
            fuelcomboBox.Items.Add("LPG");
        }

        private void MarkComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModelComboBox.Enabled = true;
            ModelComboBox.SelectedIndex = -1;

            using (ScraperEntities context = new ScraperEntities())
            {
                var selectedBrand = MarkComboBox.Text;
                var brandsIds = context.CarBrands
                    .Where(x => x.CarBrandName == selectedBrand)
                    .Select(x => x.CarBrandId).ToList();

                var models = context.CarModels
                    .Where(x => brandsIds.Any(c => c == x.CarBrandId))
                    .GroupBy(c => c.CarModelName)
                    .Select(x => new { value = x.Key, count = x.Count() })
                    .OrderBy(x => x.value)
                    .OrderByDescending(x => x.count)
                    .Select(x => x.value)
                    .ToList();

                models.Insert(0, "");

                ModelComboBox.DataSource = models;
                ModelComboBox.SelectedIndex = -1;
            }
        }

        private void YearsFromTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void AdsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 8 && AdsDataGridView.Rows[e.RowIndex].Cells[8].Value != null)
            {
                AdsDataGridView.Rows[e.RowIndex].Cells[8].ToolTipText = AdsDataGridView.Rows[e.RowIndex].Cells[8].Value.ToString();
                e.Value = "marktplaats.nl";
            }
            if (e.ColumnIndex == 9 && AdsDataGridView.Rows[e.RowIndex].Cells[9].Value != null)
            {
                AdsDataGridView.Rows[e.RowIndex].Cells[9].ToolTipText = AdsDataGridView.Rows[e.RowIndex].Cells[9].Value.ToString();
                e.Value = "speurders.nl";
            }
            if (e.ColumnIndex == 10 && AdsDataGridView.Rows[e.RowIndex].Cells[10].Value != null)
            {
                AdsDataGridView.Rows[e.RowIndex].Cells[10].ToolTipText = AdsDataGridView.Rows[e.RowIndex].Cells[10].Value.ToString();
                e.Value = "nederlandmobiel.nl";
            }
        }

        private void NewestListdataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 7)
            {
                NewestListdataGridView.Rows[e.RowIndex].Cells[7].ToolTipText = NewestListdataGridView.Rows[e.RowIndex].Cells[7].Value.ToString();
                if (NewestListdataGridView.Rows[e.RowIndex].Cells[7].Value.ToString().StartsWith("https://www.marktplaats.nl"))
                    e.Value = "marktplaats.nl";
                else if (NewestListdataGridView.Rows[e.RowIndex].Cells[7].Value.ToString().StartsWith("https://www.speurders.nl"))
                    e.Value = "speurders.nl";
                else if (NewestListdataGridView.Rows[e.RowIndex].Cells[7].Value.ToString().StartsWith("https://www.nederlandmobiel.nl"))
                    e.Value = "nederlandmobiel.nl";
            }
        }

        private void logsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addPanel.Visible = false;
            adspanel.Visible = false;
            LogsPanel.Visible = true;
        }
    }
}