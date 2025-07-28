using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models
{
    #region UI State Models

    /// <summary>
    /// Theme mode for the application
    /// </summary>
    public enum ThemeMode
    {
        /// <summary>
        /// Light theme
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme
        /// </summary>
        Dark,

        /// <summary>
        /// System theme (follows OS settings)
        /// </summary>
        System
    }

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
        public string Name { get; set; }

        /// <summary>
        /// Description of the layout
        /// </summary>
        public string Description { get; set; }

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

    /// <summary>
    /// User preferences for the application
    /// </summary>
    public class UserPreferences : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Theme mode
        /// </summary>
        public ThemeMode Theme { get; set; } = ThemeMode.System;

        /// <summary>
        /// Font size for the editor
        /// </summary>
        public int EditorFontSize { get; set; } = 12;

        /// <summary>
        /// Font family for the editor
        /// </summary>
        public string EditorFontFamily { get; set; } = "Consolas";

        /// <summary>
        /// Whether to show line numbers
        /// </summary>
        public bool ShowLineNumbers { get; set; } = true;

        /// <summary>
        /// Whether to enable word wrap
        /// </summary>
        public bool EnableWordWrap { get; set; } = false;

        /// <summary>
        /// Whether to highlight the current line
        /// </summary>
        public bool HighlightCurrentLine { get; set; } = true;

        /// <summary>
        /// Tab size in the editor
        /// </summary>
        public int TabSize { get; set; } = 4;

        /// <summary>
        /// Whether to use spaces instead of tabs
        /// </summary>
        public bool UseSpaces { get; set; } = true;

        /// <summary>
        /// Whether to auto-save files
        /// </summary>
        public bool AutoSave { get; set; } = true;

        /// <summary>
        /// Auto-save interval in seconds
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// ID of the default layout configuration
        /// </summary>
        public string DefaultLayoutId { get; set; }

        /// <summary>
        /// List of recent projects
        /// </summary>
        public List<string> RecentProjects { get; set; } = new List<string>();

        /// <summary>
        /// Custom key bindings
        /// </summary>
        public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Custom UI settings
        /// </summary>
        public Dictionary<string, string> UISettings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a clone of the user preferences
        /// </summary>
        public UserPreferences Clone()
        {
            return new UserPreferences
            {
                Id = Id,
                Theme = Theme,
                EditorFontSize = EditorFontSize,
                EditorFontFamily = EditorFontFamily,
                ShowLineNumbers = ShowLineNumbers,
                EnableWordWrap = EnableWordWrap,
                HighlightCurrentLine = HighlightCurrentLine,
                TabSize = TabSize,
                UseSpaces = UseSpaces,
                AutoSave = AutoSave,
                AutoSaveIntervalSeconds = AutoSaveIntervalSeconds,
                DefaultLayoutId = DefaultLayoutId,
                RecentProjects = RecentProjects != null ? new List<string>(RecentProjects) : new List<string>(),
                KeyBindings = KeyBindings != null ? new Dictionary<string, string>(KeyBindings) : new Dictionary<string, string>(),
                UISettings = UISettings != null ? new Dictionary<string, string>(UISettings) : new Dictionary<string, string>()
            };
        }
    }

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

    #endregion
}