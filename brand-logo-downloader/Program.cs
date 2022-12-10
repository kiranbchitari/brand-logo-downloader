using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Image = System.Drawing.Image;

namespace brand_logo_downloader
{
    public class Program
    {
        public static async Task Main()
        {
            int count = 0;
            DataTable dtData = ConvertCsvToDataTable(@"..\..\brandfile.csv");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
            foreach (DataRow item in dtData.Rows)
            {
                count++;
                int Id = Convert.ToInt32(item[0]);
                string Url = Convert.ToString(item[1]);
                Url = Url.Replace("https://www.", "").Replace("http://www.", "").Replace("https://", "").Replace("http://", "");
                int index = Url.IndexOf('/');
                if (index > 0)
                {
                    Url = Url.Remove(index);
                }
                Uri myUri = new Uri("https://brandfetch.com/" + Url);
                string url = myUri.AbsoluteUri;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(url + " " + count);
                var page = await browser.NewPageAsync();
                try
                {
                    await page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error for " + url);
                }
                string content = await page.GetContentAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                try
                {
                    var images = doc.DocumentNode.Descendants("img")
                                           .Select(e => e.GetAttributeValue("src", null))
                                           .Where(s => !String.IsNullOrEmpty(s) && s.Contains("https://asset.brandfetch.io/")).ToList();

                    if (images != null)
                    {
                        foreach (var image in images)
                        {
                            SaveImage(image, Id.ToString(), ImageFormat.Png);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                await page.CloseAsync();
            }
        }
        public static DataTable ConvertCsvToDataTable(string filePath)
        {
            string[] rows = File.ReadAllLines(filePath);

            DataTable dtData = new DataTable();
            string[] rowValues = null;
            DataRow dr = dtData.NewRow();

            if (rows.Length > 0)
            {
                foreach (string columnName in rows[0].Split(','))
                    dtData.Columns.Add(columnName);
            }
            for (int row = 1; row < rows.Length; row++)
            {
                rowValues = rows[row].Split(',');
                dr = dtData.NewRow();
                dr.ItemArray = rowValues;
                dtData.Rows.Add(dr);
            }

            return dtData;
        }
        public static void SaveImage(string imageUrl, string filename, ImageFormat format)
        {
            try
            {
                string imagePath = @"..\..\Images\";
                if (!Directory.Exists(imagePath))
                    Directory.CreateDirectory(imagePath);
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " + " (compatible; MSIE 6.0; Windows    NT 5.1; " + ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                    byte[] data = webClient.DownloadData(imageUrl);

                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        using (var image = Image.FromStream(mem))
                        {
                            if (image.Height == image.Width)
                            {
                                if (image.Height > 150)
                                {
                                    var img = resizeImage(image, new Size(400, 400));
                                    img.Save(imagePath + filename + ".png", format);

                                }

                            }
                        }
                    }

                }
            }
            catch (Exception)
            {
            }
        }
        private static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (System.Drawing.Image)b;
        }
    }
}
