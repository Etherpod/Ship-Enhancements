using System;
using System.Net.Http;
using System.Net.Http.Headers;
//using Dalamud.Plugin.Services;

namespace ShipEnhancements;

public class HttpClientGenerator : IDisposable
{
    private readonly string _baseUrlSupplier;
    private readonly Action<HttpClient> _clientConfigurer;

    private static readonly MediaTypeWithQualityHeaderValue MediaTypeJson =
        MediaTypeWithQualityHeaderValue.Parse("application/json");

    private static readonly ProductInfoHeaderValue UserAgent = new ProductInfoHeaderValue(
        "ShipEnhancements",
        "1.2.0"
    );

    private HttpClient _client = new();

    public HttpClient Client
    {
        get
        {
            var latestUrl = new Uri(_baseUrlSupplier);
            if (_client.BaseAddress != latestUrl)
            {
                InitializeNewClient();
            }

            return _client;
        }
    }

    public HttpClientGenerator(/*IPluginLog log, */ string baseUrlSupplier, Action<HttpClient> clientConfigurer)
    {
        //_log = log;
        _baseUrlSupplier = baseUrlSupplier;
        _clientConfigurer = clientConfigurer;

        InitializeNewClient();
    }

    public void Dispose()
    {
        _client.Dispose();

        GC.SuppressFinalize(this);
    }

    private void InitializeNewClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseUrlSupplier);
        //_log.Debug("generating a new http client for base address: {0:l}", client.BaseAddress.ToString());
        client.DefaultRequestHeaders.UserAgent.Add(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(MediaTypeJson);
        _clientConfigurer(client);

        _client.Dispose();
        _client = client;
    }
}