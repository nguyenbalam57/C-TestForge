using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Projects
{
    /// <summary>
    /// Interface for source file service
    /// </summary>
    public interface ISourceFileService
    {
        /// <summary>
        /// Process type replacements in a source file
        /// </summary>
        /// <param name="sourceFile">Source file to process</param>
        /// <returns>List of type replacements</returns>
        List<TypeReplacement> ProcessTypeReplacements(SourceFile sourceFile);

        /// <summary>
        /// Replace types in content based on type mappings
        /// </summary>
        /// <param name="content">Source content</param>
        /// <returns>Tuple containing processed content and list of replacements</returns>
        (string processedContent, List<TypeReplacement> replacements) ReplaceTypes(string content, List<int> lineStartPositions = null);

        /// <summary>
        /// Find all type occurrences in content without replacing
        /// </summary>
        /// <param name="content">Source content</param>
        /// <returns>List of type occurrences</returns>
        List<TypeOccurrence> FindTypeOccurrences(string content);

        /// <summary>
        /// Apply replacements to content
        /// </summary>
        /// <param name="content">Original content</param>
        /// <param name="replacements">Replacements to apply</param>
        /// <returns>Processed content</returns>
        string ApplyReplacements(string content, IEnumerable<TypeReplacement> replacements);
    }
}
