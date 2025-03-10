using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using Movora.Application.AI;
using Movora.Application.AI.Interfaces;
// add namespace 
namespace Movora.WebAPI
{
    public static class ModulePackage
    {
        public static void RegisterServices(IServiceCollection services)
        {
            // Register ILLM implementation
            services.AddSingleton<ILLM, Groq>();

            // Register other services here
        }
    }
}