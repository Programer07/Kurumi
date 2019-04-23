using Kurumi.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace Kurumi.Services.Random
{
    public class KurumiRandom
    {
        private static int Index = 1;
        private static int[] Buffer;
        private static int MaxMax;
        public static DateTime LastRequest = new DateTime(1970, 1, 1);

        public static int RequestsLeft { get; private set; } = 0;
        public static int BitsLeft { get; private set; } = 0;

        

        private System.Random rand;
        private readonly HttpClient http;
        private readonly int BufferSize = 10;

        public KurumiRandom() => http = new HttpClient
        {
            Timeout = new TimeSpan(0, 0, seconds: 10)
        };

        public int Next([Optional]int MinValue, int MaxValue)
        {
            //Check if the api key is set
            if (Config.RandomOrgApiKey == null)
                goto GenerateDefault;

            //Check if possible
            if (MinValue > MaxValue)
                throw new ArgumentOutOfRangeException("MinValue", "'MinValue' cannot be greater than 'MaxValue'");
            //2 values are equal, return min
            if (MaxValue - 1 <= MinValue)
                return MinValue;

            //Check if buffer has a usable value
            int? num = GetNextFromBuffer();
            if (num != null)
                //1, Check if the value is less then MaxValue
                //2, Check if the value is larget then MinValue
                //3, Check if the MaxValue is less or equal to the required MaxMax
                if (num.Value < MaxValue && num.Value >= MinValue && MaxMax >= MaxValue)
                    return num.Value;

            if ((RequestsLeft > 0 && BitsLeft > 30) || LastRequest.Day < DateTime.Now.Day)
            {
                //Set the last request to now
                LastRequest = DateTime.Now;

                //Haven't reached request limit
                try
                {
                    //Create content
                    using (var content = new StringContent(
                        JsonConvert.SerializeObject(new RequestContent("generateIntegers", BufferSize, MinValue, MaxValue)).Replace("Params", "params"), Encoding.UTF8, "application/json"))
                    {
                        //Set headers
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/json-rpc");
                        //LINK START
                        HttpResponseMessage res = http.PostAsync($"https://api.random.org/json-rpc/1/invoke", content).Result;
                        //Get response
                        ResponseContent restCont = JsonConvert.DeserializeObject<ResponseContent>(res.Content.ReadAsStringAsync().Result);
                        //Get random values
                        Buffer = JsonConvert.DeserializeObject<RandomContent>(restCont.result["random"].ToString()).data;
                        //Set the remaining bits and requests
                        BitsLeft = int.Parse(restCont.result["bitsLeft"].ToString());
                        RequestsLeft = int.Parse(restCont.result["requestsLeft"].ToString());

                        //Set the MaxMax to +20% and the buffer index "pointer"
                        MaxMax = (int)(MaxValue * 1.2);
                        Index = 1;
                        //Return the first number
                        return Buffer[0];
                    }
                }
                catch (Exception) { } //random.org offline / disconnected from internet while a command was running / server error
            }
        GenerateDefault:
            //Out of requests, failed or disabled, generate number with the default rng
            rand = new System.Random();
            return rand.Next(MinValue, MaxValue);
        }

        private int? GetNextFromBuffer()
        {
            //Check if the buffer is null or the "pointer" reached the end.
            if (Buffer == null || Index == Buffer.Length)
                return null;
            //Get data
            int Data = Buffer[Index];
            //Increment "pointer"
            Index++;

            return Data;
        }
    }
    public class RequestContent
    {
        public string jsonrpc = "2.0";
        public string method { get; private set; }
        public Dictionary<string, object> Params { get; private set; } = new Dictionary<string, object>() { { "apiKey", Config.RandomOrgApiKey }, { "replacement", true } };
        public int id { get; private set; } = 3030;

        public RequestContent(string Method, [Optional]int? N, [Optional]int? MinValue, [Optional]int? MaxValue)
        {
            if (N != null)
                Params.Add("n", N);
            if (MinValue != null && MaxValue != null)
            {
                Params.Add("min", (MinValue.Value));
                Params.Add("max", (MaxValue.Value - 1));
            }
            method = Method;
        }
    }
    public class ResponseContent
    {
        public Dictionary<string, object> result { get; set; }
    }
    public class RandomContent
    {
        public int[] data { get; set; }
    }
}