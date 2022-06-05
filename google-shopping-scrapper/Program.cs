using google_shopping_scrapper.Model;
using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace google_shopping_scrapper
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions {
                Headless = false,
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe" // Chrome.exe local Path 
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://www.google.com/search?q=printers&tbm=shop", WaitUntilNavigation.Load);
            string content = await page.GetContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            List<Item> products = new List<Item>();
            foreach (var item in doc.DocumentNode.SelectNodes("(//a[@class='Lq5OHe eaGTj translate-content'])"))
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

                        var name = doc3.DocumentNode.SelectNodes("(//span[@class='BvQan sh-t__title-pdp sh-t__title translate-content'])").FirstOrDefault();
                        product.name = name.InnerHtml.ToString();
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
                }
            }
            var allproducts= products;
        }
    }
}
