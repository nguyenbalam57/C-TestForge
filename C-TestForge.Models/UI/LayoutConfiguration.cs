using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.UI
{
    /// <summary>
    /// Layout configuration for the application
    /// </summary>
    public class LayoutConfiguration : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the layout
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the layout
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// List of panel layouts
        /// </summary>
        public List<PanelLayout> Panels { get; set; } = new List<PanelLayout>();

        /// <summary>
        /// Whether this is the default layout
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Last modified time of the layout
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Creates a clone of the layout configuration
        /// </summary>
        public LayoutConfiguration Clone()
        {
            return new LayoutConfiguration
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Panels = Panels?.Select(p => p.Clone()).ToList() ?? new List<PanelLayout>(),
                IsDefault = IsDefault,
                LastModified = LastModified
            };
        }
    }
}
