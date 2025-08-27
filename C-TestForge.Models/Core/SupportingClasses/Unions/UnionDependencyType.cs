using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Unions
{
    /// <summary>
    /// Types of union dependencies
    /// </summary>
    public enum UnionDependencyType
    {
        /// <summary>
        /// Direct dependency (union contains another union as member)
        /// </summary>
        Direct,

        /// <summary>
        /// Pointer dependency (union contains pointer to another union)
        /// </summary>
        Pointer,

        /// <summary>
        /// Array dependency (union contains array of another union)
        /// </summary>
        Array,

        /// <summary>
        /// Indirect dependency (through function parameters or return types)
        /// </summary>
        Indirect,

        /// <summary>
        /// Circular dependency
        /// </summary>
        Circular
    }
}
