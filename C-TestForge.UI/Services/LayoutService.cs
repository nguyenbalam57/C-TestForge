using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.UI.Services
{
    public class LayoutService : ILayoutService
    {
        private readonly DockingManager _dockingManager;
        private readonly IDialogService _dialogService;

        public LayoutService(DockingManager dockingManager, IDialogService dialogService)
        {
            _dockingManager = dockingManager;
            _dialogService = dialogService;
        }

        public void SaveLayout(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    _dockingManager.SaveLayout(stream);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error saving layout", ex.Message);
            }
        }

        public void LoadLayout(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    _dockingManager.LoadLayout(stream);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error loading layout", ex.Message);
            }
        }

        public void ResetLayout()
        {
            // Tạo layout mặc định
            var layoutRoot = new LayoutRoot();

            // Cấu hình layout mặc định...

            _dockingManager.Layout = layoutRoot;
        }
    }
}
