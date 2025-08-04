using C_TestForge.UI.Models;
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

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for MaterialDialogView.xaml
    /// </summary>
    public partial class MaterialDialogView : UserControl
    {
        public MaterialDialogView()
        {
            InitializeComponent();
        }

        public MaterialDialogView(MaterialDialogModel model)
        {
            InitializeComponent();

            DataContext = model;
        }
    }
}
