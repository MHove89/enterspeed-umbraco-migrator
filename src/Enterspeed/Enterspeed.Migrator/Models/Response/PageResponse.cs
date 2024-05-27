using System.Collections.Generic;

namespace Enterspeed.Migrator.Models.Response
{
    public class PageResponse
    {
        public PageResponse()
        {
            Data = new Dictionary<string, object>();
            Children = new List<PageResponse>();
        }

        public Dictionary<string, object> Data { get; set; }
        public List<PageResponse> Children { get; set; }
    }
}