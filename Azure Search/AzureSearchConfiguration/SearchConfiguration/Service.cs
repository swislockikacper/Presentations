using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;

namespace SearchConfiguration
{
    class Service
    {
        private const string contentDataSourceName = "content-source";
        private const string filesDataSourceName = "files-source";
        private const string contentTable = "Article";
        private const string indexName = "index";
        private const string scoringProfileName = "scoring-profile";
        private const string suggesterName = "suggester";

        private const string contentIndexerName = "content-indexer";
        private const string filesIndexerName = "files-indexer";

        private string dbConnectionString = ConfigurationManager.AppSettings["DbConnectionString"];
        private string blobConnectionString = ConfigurationManager.AppSettings["BlobConnectionString"];
        private string serviceName = ConfigurationManager.AppSettings["ServiceName"];
        private string apiKey = ConfigurationManager.AppSettings["ApiKey"];

        private readonly SearchServiceClient searchClient;

        private bool serviceWorks = true;

        public Service()
        {
            searchClient = new SearchServiceClient(serviceName, new SearchCredentials(apiKey));
        }

        public bool CreateService()
        {
            Console.WriteLine("Creating files search...");
            Console.WriteLine();
            CreateContentDataSource();

            if (!serviceWorks)
                return false;

            Console.WriteLine();
            CreateBlobDataSource();

            if (!serviceWorks)
                return false;

            Console.WriteLine();
            CreateIndex();

            if (!serviceWorks)
                return false;

            Console.WriteLine();
            CreateBlobIndexer();

            if (!serviceWorks)
                return false;

            Console.WriteLine();
            CreateContentIndexer();

            Console.WriteLine();

            return serviceWorks;
        }

        private void CreateContentDataSource()
        {
            Console.WriteLine("Creating data source...");

            var dataSource = DataSource.AzureSql(
                name: contentDataSourceName,
                sqlConnectionString: dbConnectionString,
                tableOrViewName: contentTable);

            try
            {
                searchClient.DataSources.CreateOrUpdate(dataSource);
                Console.WriteLine("Done");
                serviceWorks = true;
            }
            catch
            {
                Console.WriteLine("Something went wrong");
                serviceWorks = false;
            }
        }

        private void CreateIndex()
        {
            Console.WriteLine("Creating index...");
            var indexExists = searchClient.Indexes.Exists(indexName);

            if (indexExists)
                searchClient.Indexes.Delete(indexName);

            var index = new Index(
                name: indexName,
                fields: FieldBuilder.BuildForType<ArticleIndex>(),
                scoringProfiles: CreateScoringProfile(),
                suggesters: CreateSuggester()
                );

            try
            {
                searchClient.Indexes.Create(index);
                Console.WriteLine("Done");
                serviceWorks = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                serviceWorks = false;
            }
        }

        private List<Suggester> CreateSuggester() =>
            new List<Suggester> { new Suggester(suggesterName, new[] { "Title" }) };


        private List<ScoringProfile> CreateScoringProfile()
        {
            var weights = new Dictionary<string, double>
            {
                { "Title", 6 }, { "Content", 3 }
            };

            return new List<ScoringProfile>
            {
                new ScoringProfile(scoringProfileName, new TextWeights(weights))
            };
        }

        private void CreateContentIndexer()
        {
            Console.WriteLine("Creating indexer...");

            var indexerExists = searchClient.Indexers.Exists(contentIndexerName);

            if (indexerExists)
                searchClient.Indexers.Delete(contentIndexerName);

            var indexer = new Indexer(
                name: contentIndexerName,
                dataSourceName: contentDataSourceName,
                targetIndexName: indexName,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));

            try
            {
                searchClient.Indexers.Create(indexer);
                searchClient.Indexers.Run(contentIndexerName);
                System.Console.WriteLine("Done");
                serviceWorks = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                serviceWorks = false;
            }
        }

        private void CreateBlobDataSource()
        {
            Console.WriteLine("Creating data source (blob)...");

            var dataSource = DataSource.AzureBlobStorage(
                name: filesDataSourceName,
                storageConnectionString: blobConnectionString,
                containerName: $"articles"
             );

            try
            {
                searchClient.DataSources.CreateOrUpdate(dataSource);
                Console.WriteLine("Done");
                serviceWorks = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                serviceWorks = false;
            }
        }

        private void CreateBlobIndexer()
        {
            Console.WriteLine("Creating indexer (blob)...");

            var indexerExists = searchClient.Indexers.Exists(filesIndexerName);

            if (indexerExists)
                searchClient.Indexers.Delete(filesIndexerName);

            var indexer = new Indexer(
                name: filesIndexerName,
                dataSourceName: filesDataSourceName,
                targetIndexName: indexName,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)),
                fieldMappings: AddFieldsToMappingInBlob()
                );

            try
            {
                searchClient.Indexers.Create(indexer);
                searchClient.Indexers.Run(filesIndexerName);
                Console.WriteLine("Done");
                serviceWorks = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                serviceWorks = false;
            }
        }

        private List<FieldMapping> AddFieldsToMappingInBlob()
            => new List<FieldMapping> { new FieldMapping("title", "Title"), new FieldMapping("content", "Content") };
    }

    class ArticleIndex
    {
        [Key]
        [IsRetrievable(true)]
        public string Id { get; set; }

        [IsFilterable, IsSearchable, IsRetrievable(true)]
        public string Title { get; set; }

        [IsFilterable, IsSearchable, IsRetrievable(true)]
        public string Content { get; set; }
    }
}
