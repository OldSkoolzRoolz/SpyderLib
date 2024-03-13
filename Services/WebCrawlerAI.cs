using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Polly;

namespace KC.Apps.SpyderLib
{
    public class WebCrawlerAI
    {
        private string startingAddress;
        private string startingHost;
        private HashSet<string> internalLinks;
        private HashSet<string> externalLinks;
        private HttpClient httpClient;

        public WebCrawlerAI(string startingAddress)
        {
            this.startingAddress = startingAddress;
            this.startingHost = new Uri(startingAddress).Host;
            this.internalLinks = new HashSet<string>();
            this.externalLinks = new HashSet<string>();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                CheckCertificateRevocationList = true,
                Credentials = null,
                DefaultProxyCredentials = null,
                MaxAutomaticRedirections = 5,
                MaxConnectionsPerServer = 5,
                UseCookies = false,
                UseDefaultCredentials = false,
                UseProxy = false
            };

            this.httpClient = new HttpClient(handler);

            // Apply resilience policies
            var retryPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var fallbackPolicy = Policy<string>.Handle<Exception>()
                .FallbackAsync("Fallback content");

            this.httpClient = this.httpClient
                .WrapAsync(retryPolicy)
                .WrapAsync(fallbackPolicy);
        }

        public async Task Crawl(int depth = 4)
        {
            this.internalLinks.Add(this.startingAddress);
            await this.ScanLinks(this.startingAddress, depth);
        }

        
        
        
        private async Task ScanLinks(string url, int depth)
        {

            if (depth == 0)
                {
                    return;
                }

            try
                {
                    // Use httpClient to make requests and handle retries
                    var response = await this.httpClient.GetAsync(url);
                    var htmlContent = await response.Content.ReadAsStringAsync();
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlContent);

                    // Process the HTML document and extract links
                    // ...

                    var links = document.DocumentNode.SelectNodes("//a[@href]");
                    if (links != null)
                        {
                            foreach (var link in links)
                                {
                                    var href = link.GetAttributeValue("href", "");
                                    if (!string.IsNullOrWhiteSpace(href))
                                        {
                                            var absoluteUrl = new Uri(new Uri(url), href).AbsoluteUri;
                                            var parsedUrl = new Uri(absoluteUrl);
                                            if (parsedUrl.Host == this.startingHost)
                                                {
                                                    this.internalLinks.Add(absoluteUrl);
                                                    this.ScanLinks(absoluteUrl, depth - 1);
                                                }
                                            else
                                                {
                                                    this.externalLinks.Add(absoluteUrl);
                                                }
                                        }
                                }
                        }

                }
            catch (WebRequestException e)
                {
                    Console.WriteLine(e);
                    throw;
                }





            // Recursive call to scan internal links
            // ...
        }

        
        
        
        
        
        
        
        public void SaveLinksToFile(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Internal Links:");
                foreach (var link in this.internalLinks)
                {
                    writer.WriteLine(link);
                }

                writer.WriteLine();
                writer.WriteLine("External Links:");
                foreach (var link in this.externalLinks)
                {
                    writer.WriteLine(link);
                }
            }
        }






        public void MultiThreadedScan(int numThreads)
            {
                var tasks = new List<Task>();
                foreach (var link in this.internalLinks)
                    {
                        tasks.Add(Task.Run(() => this.ScanLinks(link, 3)));
                    }

                Task.WaitAll(tasks.ToArray());
            }
        
        
        
        
    }
}