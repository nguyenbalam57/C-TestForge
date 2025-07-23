using System.Windows;
using System.Windows.Media;

namespace C_TestForge.UI.Helpers
{
    public static class DialogHelper
    {
        public static void ShowErrorDialog(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowInfoDialog(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowWarningDialog(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static bool ShowConfirmDialog(string message, string title = "Confirmation")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }

        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null)
                return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T childType)
                {
                    if (childType is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = childType;
                        break;
                    }
                }

                foundChild = FindChild<T>(child, childName);

                if (foundChild != null)
                    break;
            }

            return foundChild;
        }
    }
}
