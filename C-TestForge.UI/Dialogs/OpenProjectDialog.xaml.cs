using C_TestForge.Models;
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
using System.Windows.Shapes;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for OpenProjectDialog.xaml
    /// </summary>
    public partial class OpenProjectDialog : Window
    {
        public OpenProjectDialog(List<TestProject> projects)
        {
            InitializeComponent();

            // Order by modified date descending
            projectsListView.ItemsSource = projects.OrderByDescending(p => p.ModifiedDate);

            if (projects.Any())
            {
                projectsListView.SelectedIndex = 0;
            }
        }

        public string SelectedProjectId => ((TestProject)projectsListView.SelectedItem)?.Id;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (projectsListView.SelectedItem == null)
            {
                MessageBox.Show("Please select a project.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
