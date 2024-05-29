using System.Threading.Tasks;
using Enterspeed.Migrator.Models.Response;

namespace Enterspeed.Migrator.Enterspeed.Contracts
{
    public interface IApiService
    {
        Task<EnterspeedResponse> GetNavigationAsync();

        /// <summary>
        /// Iterates through all the pages and maps to a delivery api deliveryApiResponse object.
        /// </summary>
        /// <param name="page"></param>
        /// <returns>List of DeliveryApiResponse</returns>
        Task<PageResponse> GetPageResponsesAsync(Item page);
    }
}