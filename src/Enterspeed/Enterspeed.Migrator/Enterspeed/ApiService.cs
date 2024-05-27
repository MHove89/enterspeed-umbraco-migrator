﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enterspeed.Delivery.Sdk.Api.Models;
using Enterspeed.Delivery.Sdk.Api.Services;
using Enterspeed.Delivery.Sdk.Domain.Models;
using Enterspeed.Migrator.Enterspeed.Contracts;
using Enterspeed.Migrator.Models.Response;
using Enterspeed.Migrator.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterspeed.Migrator.Enterspeed
{
    internal class ApiService : IApiService
    {
        private readonly IEnterspeedDeliveryService _enterspeedDeliveryService;
        private readonly EnterspeedConfiguration _enterspeedConfiguration;
        private readonly ILogger<ApiService> _logger;

        public ApiService(IEnterspeedDeliveryService enterspeedDeliveryService,
            IOptions<EnterspeedConfiguration> enterspeedConfiguration,
            ILogger<ApiService> logger)
        {
            _enterspeedDeliveryService = enterspeedDeliveryService;
            _enterspeedConfiguration = enterspeedConfiguration?.Value;
            _logger = logger;
        }

        public async Task<EnterspeedResponse> GetNavigationAsync()
        {
            var data = await _enterspeedDeliveryService.Fetch(_enterspeedConfiguration.ApiKey,
                (s) => s.WithHandle(_enterspeedConfiguration.NavigationHandle));

            var json = JsonSerializer.Serialize(data.Response);
            return JsonSerializer.Deserialize<EnterspeedResponse>(json);
        }

        public async Task<DeliveryApiResponse> GetByUrlAsync(string url)
        {
            _logger.LogInformation("Calling page url = ", url);
            return await _enterspeedDeliveryService.Fetch(_enterspeedConfiguration.ApiKey, (s) => s.WithUrl(url));
        }
        public async Task<DeliveryApiResponse> GetByHandlesAsync(List<string> handles)
        {
            return await _enterspeedDeliveryService.FetchMany(_enterspeedConfiguration.ApiKey, new GetByIdsOrHandle { Handles = handles });
        }

        public async Task<PageResponse> GetPageResponsesAsync(EnterspeedResponse enterspeedResponse)
        {
            var pageResponse = new PageResponse
            {
                Data = (await GetByUrlAsync(enterspeedResponse?.Views?.Navigation?.Self?.Url)).Response.Route
            };

            if (enterspeedResponse.Views.Navigation?.Children is not null)
            {
                var children = await MapResponseAsync(enterspeedResponse.Views.Navigation?.Children);
                pageResponse.Children.AddRange(children);
            }

            return pageResponse;
        }

        private async Task<List<PageResponse>> MapResponseAsync(List<Child> children)
        {
            var childrenResponse = await GetByHandlesAsync(children.Select(x => x.Self?.Url).ToList());

            var pageResponses = new List<PageResponse>();
            foreach (var child in children)
            {
                if (!childrenResponse.Response.Views.TryGetValue(child?.Self?.Url, out var response) || response is not Dictionary<string, object> data)
                {
                    continue;
                }

                var pageResponse = new PageResponse
                {
                    Data = data
                };

                if (child?.Children != null && child.Children.Any())
                {
                    var mappedResponses = await MapResponseAsync(child.Children);
                    pageResponse.Children.AddRange(mappedResponses);
                }

                pageResponses.Add(pageResponse);
            }
            return pageResponses;
        }
    }
}