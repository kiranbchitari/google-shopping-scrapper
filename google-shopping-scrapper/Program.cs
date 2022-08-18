using google_shopping_scrapper.Model;
using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace google_shopping_scrapper
{
    class Program
    {
        static List<Item> products = new List<Item>();
        public static async Task Main(string[] args)
        {
            string url = "https://www.google.com/search?q=printers&tbm=shop";
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe" // Chrome.exe local Path 
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync(url, WaitUntilNavigation.Load);
            string content = await page.GetContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            List<PageInfo> pageInfos = new List<PageInfo>();
            pageInfos = GetPageInfo(doc, url, pageInfos);
            foreach (var pageInfo in pageInfos)
            {
                await page.GoToAsync(pageInfo.Url, WaitUntilNavigation.Load);
                string content1 = await page.GetContentAsync();
                var parentDoc = new HtmlDocument();
                parentDoc.LoadHtml(content1);
                foreach (var item in parentDoc.DocumentNode.SelectNodes("(//a[@class='Lq5OHe eaGTj translate-content'])"))
                {
                    Item product = new Item();
                    string link = item.OuterHtml;
                    var doc2 = new HtmlDocument();
                    doc2.LoadHtml(link);
                    var anchor = doc2.DocumentNode.SelectSingleNode("//a");
                    if (anchor != null)
                    {
                        string href = anchor.Attributes["href"].Value;
                        if (href.Contains("shopping/product"))
                        {
                            product.ProductUrl = "https://www.google.com" + href;
                            await page.GoToAsync(product.ProductUrl, WaitUntilNavigation.Load);
                            string morecontent = await page.GetContentAsync();
                            var doc3 = new HtmlDocument();
                            doc3.LoadHtml(morecontent);
                            try
                            {
                                var name = doc3.DocumentNode.SelectNodes("(//span[@class='BvQan sh-t__title-pdp sh-t__title translate-content'])").FirstOrDefault();
                                product.name = name.InnerHtml.ToString();
                                var imageUrl = doc3.DocumentNode.Descendants("img")
                                               .Select(e => e.GetAttributeValue("src", null))
                                               .Where(s => !String.IsNullOrEmpty(s) && s.Contains("com/shopping?")).ToList().FirstOrDefault();
                                product.ImageUrl = imageUrl;
                                try
                                {
                                    var des = doc3.DocumentNode.SelectNodes("(//span[@class='sh-ds__full-txt translate-content'])").FirstOrDefault();
                                    product.description = des.InnerHtml.ToString();
                                }
                                catch (Exception)
                                {
                                    var des = doc3.DocumentNode.SelectNodes("(//span[@class='sh-ds__trunc-txt translate-content'])").FirstOrDefault();
                                    product.description = des.InnerHtml.ToString();
                                }
                                products.Add(product);
                            }
                            catch (Exception) { continue; }

                        }
                    }
                }
            }

            var allproducts = products;
        }
        private static List<PageInfo> GetPageInfo(HtmlDocument doc, string url, List<PageInfo> pageInfos)
        {
            int i = 1;
            pageInfos.Add(new PageInfo
            {
                PageNumber = 1,
                Url = url
            }
            );
            foreach (var item in doc.DocumentNode.SelectNodes("//a[contains(@class, 'fl')]"))
            {
                PageInfo page = new PageInfo();
                var anchor = item.OuterHtml.ToString();

                var hrefLink = XElement.Parse("<p>" + anchor + "</p>")
                       .Descendants("a")
                       .Select(x => x.Attribute("href").Value)
                       .FirstOrDefault();

                if (hrefLink != null)
                {
                    page.Url = "https://www.google.com" + hrefLink;
                    page.PageNumber = ++i;
                    pageInfos.Add(page);
                }
            }

            return pageInfos;
        }
    }
}
