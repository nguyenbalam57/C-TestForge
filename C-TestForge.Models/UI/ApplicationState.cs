using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.UI
{
    /// <summary>
    /// Application state
    /// </summary>
    public class ApplicationState : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Current project
        /// </summary>
        public Project CurrentProject { get; set; }

        /// <summary>
        /// Currently active source file
        /// </summary>
        public string ActiveSourceFile { get; set; }

        /// <summary>
        /// Currently active test case
        /// </summary>
        public string ActiveTestCaseId { get; set; }

        /// <summary>
        /// Currently active function
        /// </summary>
        public string ActiveFunction { get; set; }

        /// <summary>
        /// Currently active layout configuration
        /// </summary>
        public string ActiveLayoutId { get; set; }

        /// <summary>
        /// List of open source files
        /// </summary>
        public List<string> OpenSourceFiles { get; set; } = new List<string>();

        /// <summary>
        /// List of open test cases
        /// </summary>
        public List<string> OpenTestCases { get; set; } = new List<string>();

        /// <summary>
        /// List of expanded nodes in the project explorer
        /// </summary>
        public List<string> ExpandedNodes { get; set; } = new List<string>();

        /// <summary>
        /// Window state (Normal, Maximized, Minimized)
        /// </summary>
        public string WindowState { get; set; } = "Normal";

        /// <summary>
        /// Window width
        /// </summary>
        public double WindowWidth { get; set; } = 1200;

        /// <summary>
        /// Window height
        /// </summary>
        public double WindowHeight { get; set; } = 800;

        /// <summary>
        /// Window X position
        /// </summary>
        public double WindowX { get; set; } = 100;

        /// <summary>
        /// Window Y position
        /// </summary>
        public double WindowY { get; set; } = 100;

        /// <summary>
        /// Creates a clone of the application state
        /// </summary>
        public ApplicationState Clone()
        {
            return new ApplicationState
            {
                Id = Id,
                CurrentProject = CurrentProject?.Clone(),
                ActiveSourceFile = ActiveSourceFile,
                ActiveTestCaseId = ActiveTestCaseId,
                ActiveFunction = ActiveFunction,
                ActiveLayoutId = ActiveLayoutId,
                OpenSourceFiles = OpenSourceFiles != null ? new List<string>(OpenSourceFiles) : new List<string>(),
                OpenTestCases = OpenTestCases != null ? new List<string>(OpenTestCases) : new List<string>(),
                ExpandedNodes = ExpandedNodes != null ? new List<string>(ExpandedNodes) : new List<string>(),
                WindowState = WindowState,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                WindowX = WindowX,
                WindowY = WindowY
            };
        }
    }
}
