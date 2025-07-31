using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Configuration for typedefs
    /// </summary>
    public class TypedefConfig
    {
        /// <summary>
        /// Predefined typedef mappings
        /// </summary>
        public List<TypedefMapping> TypedefMappings { get; set; } = new List<TypedefMapping>();

        /// <summary>
        /// Typedefs detected from header files
        /// </summary>
        public List<TypedefMapping> DetectedTypedefs { get; set; } = new List<TypedefMapping>();
    }
}
