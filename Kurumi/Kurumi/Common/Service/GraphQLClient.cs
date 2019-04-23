using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Common.Service
{
    public class GraphQLClient : QueryResult
    {
        private readonly HttpClient Client;
        private readonly string Url;
        public GraphQLClient(string Url)
        {
            this.Url = Url;
            this.Client = new HttpClient();
        }

        public async Task<QueryResult> Get(string Query, object Variables)
        {
            //Create string from input
            string StringContent = JsonConvert.SerializeObject(new Query() { QueryString = Query, Variables = Variables });

            //Post request
            HttpContent Content = new StringContent(StringContent, Encoding.UTF8, "application/json");
            var Response = await Client.PostAsync(Url, Content);

            //Get received data
            var DataReceived = await Response.Content.ReadAsStringAsync();
            this.Create(DataReceived);
            return this; //Return base
        }

        private class Query
        {
            [JsonProperty("query")]
            public string QueryString { get; set; }
            [JsonProperty("variables")]
            public object Variables { get; set; }
        }
    }

    public class QueryResult
    {
        private string ReceivedData;
        private JObject JData;

        protected QueryResult() { }

        protected void Create(string Content)
        {
            ReceivedData = Content;
            JData = JObject.Parse(Content);
        }

        public T Get<T>(string Key) => JsonConvert.DeserializeObject<T>(JData["data"][Key].ToString());
    }
}