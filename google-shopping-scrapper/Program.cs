using CsvHelper;
using google_shopping_scrapper.Model;
using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace google_shopping_scrapper
{
    class Program
    {
        static string CategoryName;
        public static async Task Main()
        {
            Console.WriteLine("Product Scrapping started at {0}", DateTime.Now);
            Console.WriteLine("Please Enter Category Name");
            CategoryName = Console.ReadLine().Trim();
            Console.WriteLine("=======================================================================================================");
            Console.WriteLine("Please wait.....");
            List<Item> products = new List<Item>();
            try
            {
                var url = Uri.EscapeUriString($"https://www.google.com/search?q=" + $"{HttpUtility.UrlEncode(CategoryName)}&tbm=shop");
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false
                });
                var page = await browser.NewPageAsync();
                Thread.Sleep(1000);
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
                    try
                    {
                        var result = parentDoc.DocumentNode.SelectNodes("(//a[@class='Lq5OHe eaGTj translate-content'])");
                        if (result != null)
                        {
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
                                        Thread.Sleep(1000);
                                        string morecontent = await page.GetContentAsync();
                                        var doc3 = new HtmlDocument();
                                        doc3.LoadHtml(morecontent);
                                        try
                                        {
                                            var name = doc3.DocumentNode.SelectNodes("(//span[@class='BvQan sh-t__title-pdp sh-t__title translate-content'])").FirstOrDefault();
                                            product.Name = name.InnerHtml.ToString();
                                            var imageUrl = doc3.DocumentNode.Descendants("img")
                                                                .Select(e => e.GetAttributeValue("src", null))
                                                                .Where(s => !String.IsNullOrEmpty(s) && s.Contains("com/shopping?")).ToList().FirstOrDefault();
                                            product.ImageUrl = imageUrl;

                                            try
                                            {
                                                var des = doc3.DocumentNode.SelectNodes("(//span[@class='sh-ds__full-txt translate-content'])").FirstOrDefault();
                                                product.Description = des.InnerHtml.ToString();
                                            }
                                            catch (Exception)
                                            {
                                                var des = doc3.DocumentNode.SelectNodes("(//span[@class='sh-ds__trunc-txt translate-content'])").FirstOrDefault();
                                                product.Description = des.InnerHtml.ToString();
                                            }
                                            Console.WriteLine($"Product Added: {products.Count() + 1}");
                                            products.Add(product);
                                        }
                                        catch (Exception)
                                        {
                                            continue;
                                        }

                                    }
                                }
                            }
                        }
                    }

                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error has occcured for category: ");
                Console.WriteLine($"CSV file has been created!! ");
                CreateCsvFile(products);
            }
            Console.WriteLine($"CSV file has been created!! ");
            CreateCsvFile(products);
        }

        private static void CreateCsvFile(List<Item> products)
        {
            string csvPath = @"C:/Temp/";
            if (!Directory.Exists(csvPath))
                Directory.CreateDirectory(csvPath);
            using (var writer = new StreamWriter(csvPath + CategoryName + " ProductsData.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(products);
            }
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
            var data = doc.DocumentNode.SelectNodes("//a[contains(@class, 'fl')]");
            if (data != null)
            {
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
            }
            return pageInfos;
        }
    }
}
