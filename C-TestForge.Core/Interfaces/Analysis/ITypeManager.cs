using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Manages type definitions and their constraints
    /// </summary>
    public interface ITypeManager
    {
        /// <summary>
        /// Initializes the type manager
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Adds a typedef mapping
        /// </summary>
        void AddTypedef(string userType, string baseType, string source = "Runtime");

        /// <summary>
        /// Tries to resolve a user type to its base type
        /// </summary>
        bool TryResolveType(string typeName, out string baseType);

        /// <summary>
        /// Gets constraint for a type
        /// </summary>
        VariableConstraint GetConstraintForType(string typeName, string variableName);

        /// <summary>
        /// Analyzes header files to detect typedefs
        /// </summary>
        Task AnalyzeHeaderFilesAsync(IEnumerable<string> headerPaths);

        /// <summary>
        /// Saves the current typedef configuration
        /// </summary>
        Task SaveTypedefConfigAsync();
    }

}
