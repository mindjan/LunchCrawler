using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LunchCrawler.Models;
using LunchCrawler.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace LunchCrawler
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly IWebDriver browser = new ChromeDriver("C:\\chromeDriver\\");

        //        private IWebDriver browser;
        //        FirefoxProfile profile = new FirefoxProfile();
        //        profile.setPreference("javascript.enabled", false);
        //        IWebDriver browser = new FirefoxDriver(profile);
        //        private static readonly IWebDriver browser = new ChromeDriver("C:\\chromeDriver\\");
        //        private static readonly IWebDriver browser = new ChromeDriver("C:\\chromeDriver\\");

        static void Main(string[] args)
        {
            var repository = new MongoRepositoryBase<Restaurant>("mongodb://localhost/LunchBox", "RestaurantsRaw");

            var builder = Builders<Restaurant>.Filter;
            var filter = builder.Exists("id");
            var restourants = repository.GetMany(filter);

            try
            {
                if (restourants.Count == 0)
                {
                    Search("restoranas");
                    Search("kavine");
                    Search("uzeiga");
                    Search("valgykla");
                    Search("picerija");
                    Search("baras");
                    Search("pubas");
                    Search("republicbaras");
                    Search("šašlykinė");
                    Search("užeiga");
                    Search("Coffee");
                    Search("Sotus.vilkas.kaunas");
                    Search("restourant");
                }
                else
                {
                    var restourantTocheck = restourants.First();

                    if (restourants.Count == 0 || restourantTocheck.CreatedAt < DateTime.Now.AddHours(-12))
                    {
                        Search("restoranas");
                        Search("restourant");
                        Search("kavine");
                        Search("uzeiga");
                        Search("valgykla");
                        Search("picerija");
                        Search("baras");
                        Search("pubas");
                        Search("republicbaras");
                        Search("šašlykinė");
                        Search("užeiga");
                        Search("Coffee");
                        Search("Sotus.vilkas.kaunas");
                    }
                }
            }
            catch (Exception ex)
            {

            }

            restourants = repository.GetMany(filter);

            var restourantsToParse = new List<Restaurant>();
            foreach (Restaurant restourant in restourants)
            {
                try
                {
                    if (restourant.location.country.ToLower().Equals("lithuania"))
                        restourantsToParse.Add(restourant);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }

            int count = 0;

//            Parallel.ForEach(restourantsToParse, new ParallelOptions { MaxDegreeOfParallelism = 50 }, (restourant) =>
//            {
//                try
//                {
//                    GetLunchPost(restourant);
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine(e);
//                }
//                
//                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!DONE POPULATING !!!!!!!!!!!!!!");
//            });

//
            foreach (var restourant in restourantsToParse)
            {
                try
                {
                    GetLunchPost(restourant);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                count = count + 1;
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!DONE POPULATING !!!!!!!!!!!!!!  COUNT " + count + " From " + restourantsToParse.Count);
            }

            Console.WriteLine("Done");

            browser.Close();
            browser.Dispose();

            Environment.Exit(0);
        }

        static void Search(string query)
        {
            var tokken = "EAADuFSmwLuEBACdHJLCTdZCepNfUk0MPkpOOZAkAReyjeLa9vpWjBZA7fVUMQ4S66AQfW3v9zZBkb2DM89czFBqJZAJXXhS0168opiOjJEJdZByi9EkZAeZBsoDKPTT2sYTroK34WZASFUfgUeRJcXYZBZC4xEHY5zXZCNs9PWAkvAsI28f6egJveTkAf5QcBt4V4ZC8kzQP77iFjRQZDZD";

            var repository = new MongoRepositoryBase<Restaurant>("mongodb://localhost/LunchBox", "RestaurantsRaw");
            var path = "https://graph.facebook.com/v3.1/search?fields=about,app_links,checkins,cover,description,engagement,hours,id,is_permanently_closed,is_verified,link,location,name,overall_star_rating,parking,payment_options,phone,price_range,rating_count,single_line_address,website,picture{height,url,width},photos&q=" + query + "&type=place&limit=999999&access_token=" + tokken;
            bool searchNext = true;
            while (searchNext)
            {
                var result = GetProductAsync(path).Result;


                foreach (var restaurant in result.data)
                {
                    try
                    {
                        try
                        {
                            repository.UpdateOne(restaurant);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            restaurant.location.PointLocation = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                                new GeoJson2DGeographicCoordinates(restaurant.location.longitude,
                                    restaurant.location.latitude));
                            repository.Insert(restaurant);
                        }


                        Console.WriteLine("INSERTED: " + restaurant.name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("FAILED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!: " + restaurant.name);
                        Console.WriteLine(ex.Message);
                    }
                }

                try
                {
                    path = result.paging.next;
                    Console.WriteLine("Search added for " + path);

                    if (result.paging.next.Length > 0)
                    {

                    }
                    else
                    {
                        searchNext = false;
                    }
                }
                catch (Exception e)
                {
                    searchNext = false;
                }

            }

            Console.WriteLine("Done ");
        }

        static async Task<RootObject> GetProductAsync(string path)
        {
            var resultStream = await client.GetStringAsync(path);

            var result = JsonConvert.DeserializeObject<RootObject>(resultStream);

            return result;
        }

        static void GetLunchPost(Restaurant restaurant)
        {
           
            var repository = new MongoRepositoryBase<LunchDeal>("mongodb://localhost/LunchBox", "LunchDeals");

//            IWebDriver browser = new ChromeDriver("C:\\chromeDriver\\");
            try
            {
                

                //Firefox's proxy driver executable is in a folder already
                //  on the host system's PATH environment variable.
                var link = restaurant.link.Split(new[] { ".com" }, StringSplitOptions.None)[1];
                link = "https://m.facebook.com" + link + "posts/";
                browser.Navigate().GoToUrl(link);

                var posts = browser.FindElements(By.CssSelector("#pages_msite_body_contents .story_body_container"));

                foreach (var post in posts)
                {
                    try
                    {
                        var moreButton = post.FindElement(By.LinkText("More"));
                        moreButton.Click();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    IWebElement time = null;
                    IWebElement withoutMoreButton = null;
                    IWebElement imagelocation = null;
                    IWebElement image = null;

                    try
                    {
                        time = post.FindElement(By.CssSelector("[data-sigil=\"m-feed-voice-subtitle\"] abbr"));
                        withoutMoreButton = post.FindElement(By.CssSelector("[data-ad-preview=\"message\"] span"));
                        imagelocation = post.FindElement(By.CssSelector("i.img"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    ReadOnlyCollection<IWebElement> withMoreButtonVisible = null;
                    ReadOnlyCollection<IWebElement> withMoreButtonInvisible = null;
                    ReadOnlyCollection<IWebElement> dealImage = null;
                    try
                    {
                        withMoreButtonVisible = post.FindElements(By.CssSelector(
                            "#pages_msite_body_contents .story_body_container [data-ad-preview=\"message\"] span[data-sigil=\"expose\"] > p"));
//                        withMoreButtonInvisible = post.FindElements(By.CssSelector(
//                            "#pages_msite_body_contents .story_body_container [data-ad-preview=\"message\"] span[data-sigil=\"expose\"] div.text_exposed_show p"));

                        dealImage = post.FindElements(By.CssSelector(".userContentWrapper .uiScaledImageContainer img"));
                    }
                    catch (Exception ex)
                    {

                    }

                   

                    try
                    {
                        if (Regex.IsMatch(time.Text, @"^\d+"))
                        {
                            if (withoutMoreButton.Text.ToLower().Contains("pietus") ||
                                withoutMoreButton.Text.ToLower().Contains("pietūs") ||
                                withoutMoreButton.Text.ToLower().Contains("pietū") ||
                                withoutMoreButton.Text.ToLower().Contains("dienos") ||
                                withoutMoreButton.Text.ToLower().Contains("pietu") ||
                                withoutMoreButton.Text.ToLower().Contains("pietums") ||
                                withoutMoreButton.Text.ToLower().Contains("pasiulymas") ||
                                withoutMoreButton.Text.ToLower().Contains("pasiūlymas") ||
                                withoutMoreButton.Text.ToLower().Contains("sriuba") ||
                                withoutMoreButton.Text.ToLower().Contains("piet"))
                            {
                                string text = withoutMoreButton.Text;

                                if (withMoreButtonVisible != null)
                                {
                                    foreach (var p in withMoreButtonVisible)
                                    {
                                        text = text + p.Text + Environment.NewLine;
                                    }
                                }

                                var deal = new LunchDeal();
                                deal.Description = text;
                                
                                deal.RestaurantId = restaurant.id;
                                deal.id = restaurant.id;
                                deal.time = time.Text;
                                deal.Restaurant = restaurant;

                                try
                                {
                                    post.Click();
                                    image = browser.FindElement(By.CssSelector("div[data-full-size-href]"));
                                    deal.ImageUrl = image.GetAttribute("data-full-size-href");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }

                                try
                                {
                                    repository.UpdateOne(deal);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    repository.Insert(deal);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                }
            }
            catch (Exception ex)
            {

            }

            //            browser.Close();
            //            browser.Dispose();

            Console.WriteLine("Done " + restaurant.name);
//            browser.Close();
//            browser.Dispose();
        }
    }

}