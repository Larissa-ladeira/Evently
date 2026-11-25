using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Evently.Api.Data.Mongo
{
    public class EventlyMongoContext
    {
        private readonly IMongoDatabase _database;

        public EventlyMongoContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoConnection") ?? "mongodb://localhost:27017";
            var databaseName = configuration["Mongo:DatabaseName"] ?? "Evently";
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Models.Mongo.EventForm> EventForms => _database.GetCollection<Models.Mongo.EventForm>("EventForms");
        public IMongoCollection<Models.Mongo.CheckInLog> CheckInLogs => _database.GetCollection<Models.Mongo.CheckInLog>("CheckInLogs");
        public IMongoCollection<Models.Mongo.OrderFormResponse> OrderFormResponses => _database.GetCollection<Models.Mongo.OrderFormResponse>("OrderFormResponses");
    }
}
