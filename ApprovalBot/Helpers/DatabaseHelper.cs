using ApprovalBot.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace ApprovalBot.Helpers
{
    public static class DatabaseHelper
    {
        private static readonly string databaseUri = ConfigurationManager.AppSettings["DatabaseUri"];
        private static readonly string databaseKey = ConfigurationManager.AppSettings["DatabaseKey"];
        private static readonly string databaseName = "ApprovalBotDB";
        private static readonly string collectionName = "Approvals";
        private static readonly string loggingCollectionName = "GraphRequests";

        private static DocumentClient client = null;

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(databaseUri), databaseKey);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = databaseName });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseName),
                        new DocumentCollection { Id = collectionName },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }

            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, loggingCollectionName));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseName),
                        new DocumentCollection { Id = loggingCollectionName },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<Approval>> GetApprovalsAsync(Expression<Func<Approval, bool>> predicate)
        {
            IDocumentQuery<Approval> query = client.CreateDocumentQuery<Approval>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName))
                .Where(predicate)
                .AsDocumentQuery();

            List<Approval> results = new List<Approval>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<Approval>());
            }

            return results;
        }

        public static async Task<Approval> GetApprovalAsync(string id)
        {
            try
            {
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, id));
                return (Approval)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (HttpStatusCode.NotFound == e.StatusCode)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<Approval> CreateApprovalAsync(Approval approval)
        {
            Document document = await client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                approval);

            return (Approval)(dynamic)document;
        }

        public static async Task<Approval> UpdateApprovalAsync(string id, Approval approval)
        {
            Document document = await client.ReplaceDocumentAsync(
                UriFactory.CreateDocumentUri(databaseName, collectionName, id), approval);

            return (Approval)(dynamic)document;
        }

        public static async Task DeleteApprovalAsync(string id)
        {
            try
            {
                await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, id));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public static async Task DeleteAllUserApprovals(string userId)
        {
            var userApprovals = await GetApprovalsAsync(a => a.Requestor == userId);

            foreach (var approval in userApprovals)
            {
                await DeleteApprovalAsync(approval.Id);
            }
        }

        public static async Task<Document> AddGraphLog(GraphLogEntry entry)
        {
            Document document = await client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, loggingCollectionName),
                entry);

            return document;
        }
    }
}