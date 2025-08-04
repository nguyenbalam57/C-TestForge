using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C_TestForge.UI.Controls
{
    /// <summary>
    /// Interaction logic for VariableDetailsView.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for VariableDetailsView.xaml
    /// </summary>
    public partial class VariableDetailsView : UserControl
    {
        /// <summary>
        /// Variable Dependency Property
        /// </summary>
        public static readonly DependencyProperty VariableProperty =
            DependencyProperty.Register(
                "Variable",
                typeof(CVariable),
                typeof(VariableDetailsView),
                new PropertyMetadata(null, OnVariableChanged));

        /// <summary>
        /// Gets or sets the variable to display
        /// </summary>
        public CVariable Variable
        {
            get { return (CVariable)GetValue(VariableProperty); }
            set { SetValue(VariableProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the VariableDetailsView class
        /// </summary>
        public VariableDetailsView()
        {
            InitializeComponent();

            // Set DataContext to self for binding
            DataContext = this;
        }

        /// <summary>
        /// Initializes a new instance of the VariableDetailsView class with a variable
        /// </summary>
        /// <param name="variable">Variable to display</param>
        public VariableDetailsView(CVariable variable) : this()
        {
            Variable = variable;
        }

        /// <summary>
        /// Called when the Variable property changes
        /// </summary>
        private static void OnVariableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VariableDetailsView view)
            {
                view.OnVariableChanged(e.OldValue as CVariable, e.NewValue as CVariable);
            }
        }

        /// <summary>
        /// Handles the Variable property change
        /// </summary>
        protected virtual void OnVariableChanged(CVariable oldVariable, CVariable newVariable)
        {
            // Unsubscribe from old variable events if needed
            if (oldVariable != null)
            {
                // Example: oldVariable.PropertyChanged -= Variable_PropertyChanged;
            }

            // Subscribe to new variable events if needed
            if (newVariable != null)
            {
                // Example: newVariable.PropertyChanged += Variable_PropertyChanged;

                // Additional initialization logic can go here
                // For example, load related data, update visualizations, etc.
            }
        }

        /// <summary>
        /// Click handler for function buttons in the Usage tab
        /// </summary>
        private void Function_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string functionName)
            {
                // Raise an event or use a service to navigate to the function details
                FunctionSelected?.Invoke(this, new FunctionSelectedEventArgs(functionName));
            }
        }

        /// <summary>
        /// Event raised when a function is selected from the Usage tab
        /// </summary>
        public event FunctionSelectedEventHandler FunctionSelected;
    }

    /// <summary>
    /// Event arguments for function selection
    /// </summary>
    public class FunctionSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the selected function
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Initializes a new instance of the FunctionSelectedEventArgs class
        /// </summary>
        /// <param name="functionName">Name of the selected function</param>
        public FunctionSelectedEventArgs(string functionName)
        {
            FunctionName = functionName;
        }
    }

    /// <summary>
    /// Delegate for function selection events
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    public delegate void FunctionSelectedEventHandler(object sender, FunctionSelectedEventArgs e);
}
