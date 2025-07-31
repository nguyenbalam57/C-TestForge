using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Mapping between a user-defined type and its base type
    /// </summary>
    public class TypedefMapping
    {
        /// <summary>
        /// User-defined type name
        /// </summary>
        public string UserType { get; set; }

        /// <summary>
        /// Base type name
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// Minimum value for range constraint
        /// </summary>
        public string MinValue { get; set; }

        /// <summary>
        /// Maximum value for range constraint
        /// </summary>
        public string MaxValue { get; set; }

        /// <summary>
        /// Size in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Source of the typedef (predefined, detected, etc.)
        /// </summary>
        public string Source { get; set; } = "Predefined";
    }
}
