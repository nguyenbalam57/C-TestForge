using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Modularity;

namespace C_TestForge.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Không cần làm gì ở đây vì đây chỉ là module chứa interfaces
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Không cần đăng ký interfaces
        }
    }
}
