using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Evently.Api.Models.Mongo
{
    public class OrderFormResponse
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("orderId")]
        public Guid OrderId { get; set; }

        [BsonElement("eventId")]
        public Guid EventId { get; set; }

        [BsonElement("userId")]
        public Guid UserId { get; set; }

        [BsonElement("answers")]
        public Dictionary<string, string> Answers { get; set; } = new();

        [BsonElement("submittedAt")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
