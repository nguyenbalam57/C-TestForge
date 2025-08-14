using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Infrastructure.Views
{
    /// <summary>
    /// Base interface for views
    /// </summary>
    public interface IView
    {
    }

    /// <summary>
    /// Dashboard view interface
    /// </summary>
    public interface IDashboardView : IView
    {
    }

    /// <summary>
    /// Project explorer view interface
    /// </summary>
    public interface IProjectExplorerView : IView
    {
    }

    /// <summary>
    /// Source analysis view interface
    /// </summary>
    public interface ISourceAnalysisView : IView
    {
    }

    /// <summary>
    /// Test cases management view interface
    /// </summary>
    public interface ITestCasesView : IView
    {
    }

    /// <summary>
    /// Test case management view interface
    /// </summary>
    public interface ITestCaseManagementView : IView
    {
    }

    /// <summary>
    /// Dialog interface for test case editor
    /// </summary>
    public interface ITestCaseEditorDialog : IView
    {
    }

    /// <summary>
    /// Dialog interface for test case comparison
    /// </summary>
    public interface ITestCaseComparisonDialog : IView
    {
    }

    /// <summary>
    /// Dialog interface for generating test cases
    /// </summary>
    public interface IGenerateTestCaseDialog : IView
    {
    }

    /// <summary>
    /// Dialog interface for confirmation
    /// </summary>
    public interface IConfirmationDialog : IView
    {
    }

    /// <summary>
    /// Type mapping manager view interface
    /// </summary>
    public interface ITypeMappingManagerView : IView
    {
    }
}
