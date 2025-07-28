using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.UI
{
    /// <summary>
    /// Type of panel in the UI
    /// </summary>
    public enum PanelType
    {
        /// <summary>
        /// Source code editor panel
        /// </summary>
        SourceEditor,

        /// <summary>
        /// Test case editor panel
        /// </summary>
        TestCaseEditor,

        /// <summary>
        /// Function explorer panel
        /// </summary>
        FunctionExplorer,

        /// <summary>
        /// Variable explorer panel
        /// </summary>
        VariableExplorer,

        /// <summary>
        /// Preprocessor explorer panel
        /// </summary>
        PreprocessorExplorer,

        /// <summary>
        /// Test execution panel
        /// </summary>
        TestExecution,

        /// <summary>
        /// Test coverage panel
        /// </summary>
        TestCoverage,

        /// <summary>
        /// Test generation panel
        /// </summary>
        TestGeneration,

        /// <summary>
        /// Log panel
        /// </summary>
        Log,

        /// <summary>
        /// Custom panel
        /// </summary>
        Custom
    }
}
