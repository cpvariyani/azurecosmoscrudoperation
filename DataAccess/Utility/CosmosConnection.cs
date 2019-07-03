using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

namespace Cosmos_CRUD.DataAccess.Utility
{
    /// <inheritdoc />
    public class CosmosConnection : ICosmosConnection
    {
        /// <summary>
        /// This readonly field is read from the Cosmos:DatabaseId value in appsettings.json.
        /// An example might be "MicroEdgeCommon". You may need to use it in a derived class
        /// to create a collection's `Uri` with `UriFactory`.
        /// </summary>
        protected string DatabaseId { get; }

        /// <summary>
        /// This is the name of the Cosmos collection that will be accessed in this data adapter.
        /// Examples might include "Grants" or "UserPreferences". You WILL need to set this in
        /// your deriving class BEFORE calling Initialize.
        /// </summary>
        protected string CollectionId { get; set; }

        ///// <summary>
        ///// Run of the mill logger.
        ///// </summary>
        //protected ILogger<ICosmosConnection> Logger { get; set; }

        /// <summary>
        /// This is the document client used to access Cosmos. Initialized here in `Initialize`.
        /// Will be used in derived classes to access the Cosmos DB.
        /// </summary>
        private DocumentClient _client;
        private readonly string _endpointUrl;
        private readonly string _authKey;
        private readonly Random _random = new Random();
        private const string _partitionKey = "environmentId";
        private readonly ILogger<ICosmosConnection> _logger;
        /// <summary>
        /// Config-based constructor.
        /// </summary>
        /// <param name="config">Config file is used to access DatabaseId, AccountURL, and AuthKey</param>
        /// /// <param name="logger">Logger for logging</param>
        public CosmosConnection(IConfiguration config, ILogger<ICosmosConnection> logger)
        {
            DatabaseId = config.GetValue<string>("Cosmos:DatabaseId");
            _endpointUrl = config.GetValue<string>("Cosmos:AccountURL");
            _authKey = config.GetValue<string>("Cosmos:AuthKey");
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DocumentClient> InitializeAsync(string collectionId)
        {
            CollectionId = collectionId;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var connectionPolicy = new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Gateway,
                ConnectionProtocol = Protocol.Https
            };

            if (_client == null)
                _client = new DocumentClient(
                    new Uri(_endpointUrl), _authKey, connectionPolicy);

            await VerifyDatabaseCreated();
            await VerifyCollectionCreated();
            return _client;
        }

        /// <summary>
        /// Verify if database is already created or it need to be created.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> VerifyDatabaseCreated()
        {

            var database = await _client.CreateDatabaseIfNotExistsAsync(
                new Database
                {
                    Id = DatabaseId
                }
                );

            if (database.StatusCode == HttpStatusCode.Created)
            {
                _logger.LogInformation($"Created DocumentDB database: {DatabaseId}");
                return true;
            }
            else if (database.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"DocumentDB database already exists: {DatabaseId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Verify if collection is already created or we need to create the collection
        /// </summary>
        /// <returns></returns>
        private async Task<bool> VerifyCollectionCreated()
        {
            if (string.IsNullOrEmpty(CollectionId))
            {
                throw new Exception("No collection id was set before accessing the CosmosConnection's Initialize method");
            }

            var databaseUri = UriFactory.CreateDatabaseUri(DatabaseId);
            var collection = await _client.CreateDocumentCollectionIfNotExistsAsync(
                databaseUri, new DocumentCollection
                {
                    Id = CollectionId,
                    PartitionKey = new PartitionKeyDefinition
                    {
                        Paths = new Collection<string> { $"/{_partitionKey}" }
                    }
                });

            if (collection.StatusCode == HttpStatusCode.Created)
            {
                _logger.LogInformation($"Created DocumentDB collection: {CollectionId}");
                return true;
            }
            else if (collection.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"DocumentDB collection already exists: {CollectionId}");
                return true;
            }

            return false;

        }

    }
}
