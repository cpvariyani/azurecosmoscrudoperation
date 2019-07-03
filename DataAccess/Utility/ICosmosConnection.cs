using Microsoft.Azure.Documents.Client;
using System.Threading.Tasks;

namespace Cosmos_CRUD.DataAccess.Utility
{
    /// <summary>
    /// Provides boilerplate connection, get, and upsert for classes that need to access
    /// Cosmos DB in an injectable format.
    /// </summary>
    /// <remarks>
    /// This is an attempt to DRY up our DataAdapters. This
    /// should cover all of the stuff we'd do if we're using
    /// Cosmos no matter the adapter.
    /// </remarks>
    public interface ICosmosConnection
    {
        /// <summary>
        /// Should be called from the deriving class in its constructor AFTER _collectionId
        /// has been set. `base.Initialize().GetAwaiter().GetResult();`
        /// </summary>
        /// <returns>Returns a task so that it's awaitable.</returns>
        Task<DocumentClient> InitializeAsync(string collectionId);
    }
}
