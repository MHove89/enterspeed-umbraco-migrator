using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Migrator.DocumentTypes.Components.Builders;

namespace Umbraco.Migrator.DocumentTypes.Components
{
    public class ComponentBuilderHandler : IComponentBuilderHandler
    {
        private readonly IEnumerable<IComponentBuilder> _componentBuilders;
        private readonly ILogger<ComponentBuilderHandler> _logger;

        public ComponentBuilderHandler(
            IEnumerable<IComponentBuilder> componentBuilders,
            ILogger<ComponentBuilderHandler> logger)
        {
            _componentBuilders = componentBuilders;
            _logger = logger;
        }

        public void BuildComponent(string alias, int parentId)
        {
            var componentBuilder = _componentBuilders.FirstOrDefault(p => p.CanBuild(alias));
            if (componentBuilder != null)
            {
                if (componentBuilder.ComponentExists(alias)) return;
                componentBuilder.Populate(parentId).Build();
            }
            else
            {
                _logger.LogError("No component property converter found for " + alias);
            }
        }
    }
}