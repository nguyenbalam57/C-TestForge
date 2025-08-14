using System.Windows.Controls;
using C_TestForge.UI.ViewModels;

namespace C_TestForge.UI.Controls
{
    /// <summary>
    /// Interaction logic for FunctionsAnalysisControl.xaml
    /// </summary>
    public partial class FunctionsAnalysisControl : UserControl
    {
        public FunctionsAnalysisControl()
        {
            InitializeComponent();
            DataContext = new FunctionsAnalysisViewModel();
        }
    }
}