using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.UI
{
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
        public string DefaultLayoutId { get; set; } = string.Empty;

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
}
