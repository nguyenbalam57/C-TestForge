using C_TestForge.Core.Logging;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Unity;
using System;
using Unity;
using Unity.Lifetime;
using CoreLogger = C_TestForge.Core.Logging.Logger<object>; // Sử dụng alias cho Logger của chúng ta

namespace C_TestForge.Core.Extensions
{
    /// <summary>
    /// Extension methods for working with Unity container in Prism
    /// </summary>
    public static class ContainerExtensions
    {
        /// <summary>
        /// Registers generic logger types with the container
        /// </summary>
        /// <param name="containerRegistry">The container registry</param>
        public static void RegisterGenericLogger(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            // Get the Unity container from the Prism container registry
            if (containerRegistry.GetContainer() is IUnityContainer unityContainer)
            {
                // Register Logger<T> as implementation for ILogger<T>
                unityContainer.RegisterType(typeof(ILogger<>), typeof(Logging.Logger<>), new ContainerControlledLifetimeManager());
            }
            else
            {
                throw new InvalidOperationException("Unable to access the Unity container from IContainerRegistry");
            }
        }
    }
}