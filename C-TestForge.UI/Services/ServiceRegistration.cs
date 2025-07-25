using C_TestForge.Core.Interfaces;
using C_TestForge.Core.Services;
using C_TestForge.Solver;
using C_TestForge.TestCase.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace C_TestForge.UI.Services
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterPhase3Services(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Z3 Solver services
            services.AddSingleton<IZ3SolverService, Z3SolverService>();
            services.AddSingleton<IVariableValueFinderService, VariableValueFinderService>();

            // Register Test Generation services
            services.AddSingleton<IStubGeneratorService, StubGeneratorService>();
            services.AddSingleton<IUnitTestGeneratorService, UnitTestGeneratorService>();
            services.AddSingleton<IIntegrationTestGeneratorService, IntegrationTestGeneratorService>();

            return services;
        }

        public static IServiceCollection RegisterAllServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Phase 1 services
            services.RegisterPhase1Services();

            // Register Phase 2 services
            services.RegisterPhase2Services();

            // Register Phase 3 services
            services.RegisterPhase3Services();

            return services;
        }
    }
}