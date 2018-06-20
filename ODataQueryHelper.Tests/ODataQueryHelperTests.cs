using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using ODataQueryHelper.Core;
using ODataQueryRunner.MongoDB;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ODataQueryHelper.Tests
{
    public class MongoDBQueryRunnerTests
    {
        [Fact]
        public void FilterTest()
        {
            var expression = "$filter=Age eq 10";
            var docQuery = new ODataQueryParser<Employee>();
            var query = docQuery.TryParse(expression);

            Assert.NotNull(query.Filter.FilterExpression);
        }


        [Fact]
        public async Task ComplexFilterTest()
        {
            var expression = "$filter=(CompanyGroup eq 'Synechron' and IsDeleted eq false) or NumberOfEmployees ge 1";
            //var expression = "startswith(Name,'Syne') eq true";
            var runner = new MongoDBQueryRunner<Company>();
            var docQuery = new ODataQueryParser<Company>();
            var query = docQuery.TryParse(expression);
            runner.Create(query);
            var collection = GetCollection<Company>("company");
            var list = await runner.QueryAsync(collection);
            Assert.True(list.Count > 0);
        }

        [Fact]
        public async Task ContainsFilterTest()
        {
            var expression = "$filter=contains(Name,'Test')&$orderby=name desc";
            //var expression = "$filter=substringof('Test', Name)";
            //var expression = "startswith(Name,'Syne') eq true";
            var runner = new MongoDBQueryRunner<MessagingAccount>();
            var docQuery = new ODataQueryParser<MessagingAccount>();
            var query = docQuery.TryParse(expression);
            runner.Create(query);
            var collection = GetCollection<MessagingAccount>("messagingaccount");
            var list = await runner.QueryAsync(collection);
            Assert.True(list.Count > 0);
        }


        private IMongoCollection<T> GetCollection<T>(string name)
        {
            var mongoUrl = new MongoUrl("mongodb://localhost:27017");
            MongoClientSettings mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
            mongoClientSettings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(e =>
                    {
                        Debug.WriteLine($"{e.DatabaseNamespace} - {e.CommandName} - {e.Command.ToJson()}");
                    });
                };
            MongoClient client = new MongoClient(mongoClientSettings);
            var convention = new ConventionPack();
            convention.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("IgnoreExtraElementConvention", convention, t => true);
            var collection = client.GetDatabase("quartz-dev").GetCollection<T>(name);


            return collection;
        }
    }
}
