using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enterspeed.Delivery.Sdk.Api.Models;
using Enterspeed.Delivery.Sdk.Api.Services;
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

        public async Task<PageResponse> GetPageResponsesAsync(EnterspeedResponse enterspeedResponse)
        {
            var pageResponse = new PageResponse
            {
                DeliveryApiResponse = await GetByUrlAsync(enterspeedResponse?.Views?.Navigation?.Self?.View?.Url)
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
            var pageResponses = new List<PageResponse>();
            foreach (var child in children)
            {
                var response = await GetByUrlAsync(child?.View?.Self?.View?.Url);
                if (response.Response == null) continue;

                var pageResponse = new PageResponse
                {
                    DeliveryApiResponse = response
                };

                if (child?.View?.Children != null && child.View.Children.Any())
                {
                    var mappedResponses = await MapResponseAsync(child.View.Children);
                    pageResponse.Children.AddRange(mappedResponses);
                }

                pageResponses.Add(pageResponse);
            }

            return pageResponses;
        }
    }
}