using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Evently.Api.Models.Mongo
{
    public class EventForm
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("eventId")]
        public Guid EventId { get; set; }

        [BsonElement("fields")]
        public List<FormField> Fields { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FormField
    {
        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty; // text, number, email, select, checkbox

        [BsonElement("required")]
        public bool Required { get; set; }

        [BsonElement("options")]
        public List<string>? Options { get; set; }
    }
}
