using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.UI
{
    /// <summary>
    /// Panel layout information
    /// </summary>
    public class PanelLayout : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the panel
        /// </summary>
        public PanelType Type { get; set; }

        /// <summary>
        /// Custom title for the panel
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Whether the panel is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether the panel is collapsed
        /// </summary>
        public bool IsCollapsed { get; set; } = false;

        /// <summary>
        /// Relative width of the panel (0-1)
        /// </summary>
        public double Width { get; set; } = 0.25;

        /// <summary>
        /// Relative height of the panel (0-1)
        /// </summary>
        public double Height { get; set; } = 0.25;

        /// <summary>
        /// X position of the panel (0-1)
        /// </summary>
        public double X { get; set; } = 0;

        /// <summary>
        /// Y position of the panel (0-1)
        /// </summary>
        public double Y { get; set; } = 0;

        /// <summary>
        /// Dock position of the panel
        /// </summary>
        public string DockPosition { get; set; } = "Left";

        /// <summary>
        /// Panel content ID (for custom panels)
        /// </summary>
        public string ContentId { get; set; }

        /// <summary>
        /// Creates a clone of the panel layout
        /// </summary>
        public PanelLayout Clone()
        {
            return new PanelLayout
            {
                Id = Id,
                Type = Type,
                Title = Title,
                IsVisible = IsVisible,
                IsCollapsed = IsCollapsed,
                Width = Width,
                Height = Height,
                X = X,
                Y = Y,
                DockPosition = DockPosition,
                ContentId = ContentId
            };
        }
    }
}
