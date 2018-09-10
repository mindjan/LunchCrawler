using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LunchCrawler.Models
{
    public class MongoModelBase
    {
        public string id { get; set; }
        public DateTime CreatedAt => DateTime.Now;
    }
}
