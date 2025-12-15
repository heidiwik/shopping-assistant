using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingAssistant
{
    public class ShoppingAssistant
    {
        private readonly ILogger<ShoppingAssistant> _logger;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly IConfiguration _config;
        private readonly string logicAppUrl;

        public ShoppingAssistant(ILogger<ShoppingAssistant> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            logicAppUrl = _config?["LogicAppUrl"] ?? throw new ArgumentNullException(nameof(logicAppUrl), "LogicAppUrl cannot be null");
        }

        [Function("ShoppingAssistant")]
        public async Task Run([TimerTrigger("0 0 7 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"ShoppingAssistant Timer trigger function executed at: {DateTime.Now}");

 
        }

        private static async Task CheckUrlList(IConfiguration _config, ILogger _logger)
        {
            string? urlList = _config?["UrlList"] ?? throw new ArgumentNullException(nameof(urlList), "UrlList cannot be null");
            List<string> urls = new List<string>(urlList.Split(','));

            var results = new List<object>();
            var availableUrls = new List<string>();

            foreach (var url in urls)
            {
                string htmlContent = await FetchHtmlAsync(url);
                if (string.IsNullOrEmpty(htmlContent))
                {
                    _logger.LogError($"Failed to fetch HTML from {url}.");
                    results.Add(new { url, isSoldOut = true, message = "Failed to fetch HTML." });
                    continue;
                }

                bool isSoldOut = CheckIfProductIsSoldOut(htmlContent);

                results.Add(new
                {
                    url,
                    isSoldOut,
                    message = isSoldOut ? "Product is sold out." : "Product is available."
                });

                if (!isSoldOut)
                {
                    availableUrls.Add(url);
                }
            }

            if (availableUrls.Count > 0)
            {
                await CallLogicApp(availableUrls);
            }
        }

        private static async Task<string> FetchHtmlAsync(string url)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                return await httpClient.GetStringAsync(url);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool CheckIfProductIsSoldOut(string html)
        {
            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var addToCartDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'add_to_cart_holder')]");

                if (addToCartDiv != null)
                {
                    var addToCartButton = addToCartDiv.SelectSingleNode(".//button[contains(@class, 'single_add_to_cart_button')]");

                    if (addToCartButton != null)
                    {
                        bool hasSoldOutClass = addToCartButton.Attributes["class"]?.Value.Contains("sold-out") ?? false;
                        bool isDisabled = addToCartButton.Attributes["disabled"] != null;

                        return hasSoldOutClass || isDisabled;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing HTML: {ex.Message}");
            }
            return false;
        }

        private static async Task CallLogicApp(List<string> availableUrls)
        {
            var countries = new HashSet<string>();
            foreach (var url in availableUrls)
            {
                var uri = new Uri(url);
                var segments = uri.Segments;
                if (segments.Length > 1)
                {
                    var countrySegment = segments[1].Trim('/');
                    countries.Add(countrySegment);
                }
            }

            var payload = new
            {
                countries = countries.ToList(),
                message = "Products are available."
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            _logger.LogInformation($"Calling Logic App with payload: {JsonConvert.SerializeObject(payload)}");

            var response = await httpClient.PostAsync(logicAppUrl, content);
        }
    }
}
