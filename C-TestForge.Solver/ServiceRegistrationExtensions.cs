using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.SolverServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Core.Interfaces.Solver;

namespace C_TestForge.Solver
{
    // Register Z3 Solver Service
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection RegisterSolverServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Z3 solver service
            services.AddSingleton<IZ3SolverService, Z3SolverService>();

            return services;
        }

        /// <summary>
        /// Registers phase 3 services (Advanced features)
        /// </summary>
        public static IServiceCollection RegisterPhase3Services(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Z3 solver service
            services.RegisterSolverServices();

            // Register automatic test case generation services
            services.AddSingleton<ITestCaseGenerationService, TestCaseGenerationService>();

            return services;
        }
    }
}
