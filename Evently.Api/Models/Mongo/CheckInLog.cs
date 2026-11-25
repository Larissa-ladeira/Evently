using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Evently.Api.Models.Mongo
{
    public class CheckInLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("eventId")]
        public Guid EventId { get; set; }

        [BsonElement("ticketId")]
        public Guid TicketId { get; set; }

        [BsonElement("qrCode")]
        public string QrCode { get; set; } = string.Empty;

        [BsonElement("checkedInBy")]
        public Guid CheckedInBy { get; set; }

        [BsonElement("checkedInAt")]
        public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

        [BsonElement("deviceInfo")]
        public string? DeviceInfo { get; set; }
    }
}
