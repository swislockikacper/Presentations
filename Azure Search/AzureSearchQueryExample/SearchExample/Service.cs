using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Collections.Generic;
using System.Configuration;

namespace SearchExample
{
    class Service
    {
        private string serviceName = ConfigurationManager.AppSettings["ServiceName"];
        private string apiKey = ConfigurationManager.AppSettings["ApiKey"];
        private const string indexName = "index";

        private readonly SearchServiceClient searchClient;
        private readonly ISearchIndexClient indexClient;

        public Service()
        {
            searchClient = new SearchServiceClient(serviceName, new SearchCredentials(apiKey));
            indexClient = searchClient.Indexes.GetClient(indexName);
        }

        private DocumentSearchResult Search(string query)
        {
            var searchParameters = new SearchParameters()
            {
                IncludeTotalResultCount = true,
                ScoringProfile = "scoring-profile",
                QueryType = QueryType.Full
            };

            return indexClient.Documents.Search(query, searchParameters);
        }

        private IEnumerable<Article> DeserializeResults(DocumentSearchResult response)
        {
            var results = new List<Article>();

            foreach (var result in response.Results)
            {
                results.Add(new Article
                {
                    Id = result.Document["Id"].ToString(),
                    Title = result.Document["Title"].ToString(),
                    Content = result.Document["Content"].ToString()
                });
            }
            return results;
        }

        public void GetResults(string query)
        {
            var response = Search(query);

            var results = DeserializeResults(response);

            foreach (var result in results)
            {
                System.Console.WriteLine(result.Title.ToUpper());
                System.Console.WriteLine();
                System.Console.WriteLine(result.Content);
            }
        }
    }

    class Article
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Content { get; set; }
    }
}
