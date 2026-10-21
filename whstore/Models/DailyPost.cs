using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace whstore.Models
{
    public class DailyPost
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}