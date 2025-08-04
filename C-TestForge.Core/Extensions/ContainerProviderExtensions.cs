using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Extensions
{
    /// <summary>
    /// Extensions for IContainerProvider
    /// </summary>
    public static class ContainerProviderExtensions
    {
        /// <summary>
        /// Gets a required service from the container
        /// </summary>
        /// <typeparam name="T">Type of service to resolve</typeparam>
        /// <param name="provider">Container provider</param>
        /// <returns>Resolved service</returns>
        public static T GetRequiredService<T>(this IContainerProvider provider)
        {
            return provider.Resolve<T>();
        }
    }
}
