﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using CryptoGramBot.Configuration;
using CryptoGramBot.Models;

namespace CryptoGramBot.Services
{
    public class CoinigyApiService : IDisposable
    {
        private readonly Dictionary<int, CoinigyAccount> _coinigyAccounts = new Dictionary<int, CoinigyAccount>();
        private readonly CoinigyConfig _config;
        private readonly ILogger<CoinigyApiService> _log;

        public CoinigyApiService(CoinigyConfig config, ILogger<CoinigyApiService> log)
        {
            _config = config;
            _log = log;
        }

        public void Dispose()
        {
        }

        public async Task<Dictionary<int, CoinigyAccount>> GetAccounts()
        {
            _log.LogInformation($"Getting account list from Coinigy");
            if (_coinigyAccounts.Count == 0)
            {
                var jObject = await CommonApiQuery("accounts", "");
                var token = jObject["data"];

                int count = 1;
                foreach (var t in token)
                {
                    var account = new CoinigyAccount
                    {
                        AuthId = t["auth_id"].ToString(),
                        Name = t["auth_nickname"].ToString()
                    };

                    _coinigyAccounts[count] = account;
                    count++;
                }
            }
            return _coinigyAccounts;
        }

        public string GetAuthIdFor(string name)
        {
            _log.LogInformation($"Getting authId for {name}");
            var coinigyAuthId = _coinigyAccounts.Values.FirstOrDefault(x => x.Name == name);
            return coinigyAuthId.AuthId;
        }

        public async Task<decimal> GetBtcBalance(string authId)
        {
            _log.LogInformation($"Getting BTC balance for {authId}");
            var jObject = await CommonApiQuery("refreshBalance", "{  \"auth_id\":" + authId + "}");

            if (jObject != null)
            {
                var btcBalance = Helpers.Helpers.BalanceForAuthId(jObject);
                return Math.Round(btcBalance, 3);
            }
            return 0;
        }

        public async Task<decimal> GetBtcBalance()
        {
            _log.LogInformation($"Getting total BTC balance");
            var jObject = await CommonApiQuery("balances", "{  \"show_nils\": 0,  \"auth_ids\": \"\"}");
            var btcBalance = Helpers.Helpers.TotalBtcBalance(jObject);
            return Math.Round(btcBalance, 3);
        }

        public async Task<decimal> GetTicker(string ticker)
        {
            _log.LogInformation($"Getting ticker data for {ticker}");
            var jObject = await CommonApiQuery("ticker", "{  \"exchange_code\": \"GDAX\",  \"exchange_market\": \"" + ticker + "\"}");
            var bid = Helpers.Helpers.GetLastBid(jObject);
            return bid;
        }

        private async Task<JObject> CommonApiQuery(string apiCall, string stringContent)
        {
            var baseAddress = new Uri(_config.Endpoint);

            using (var httpClient = new HttpClient { BaseAddress = baseAddress })
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _config.Key);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-secret", _config.Secret);

                using (var content = new StringContent(stringContent, Encoding.Default, "application/json"))
                {
                    _log.LogInformation($"Querying Coinigy API: {baseAddress}/{apiCall} and content is {stringContent}");
                    using (var response = await httpClient.PostAsync(apiCall, content))
                    {
                        try
                        {
                            var responseData = await response.Content.ReadAsStringAsync();
                            return JObject.Parse(responseData);
                        }
                        catch (Exception exception)
                        {
                            var ex = exception.Message;
                            _log.LogError(ex, "Exception when parsing response from Coinigy");
                            // Coinigy sometimes returns an odd object here when trying refresh balance
                            return null;
                        }
                    }
                }
            }
        }
    }
}