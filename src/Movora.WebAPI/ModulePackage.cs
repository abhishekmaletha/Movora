using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using Movora.Application.AI;
using Movora.Application.AI.Interfaces;
using Movora.Application.MovieService;
using Movora.Application.MovieService.Interfaces;
// add namespace 
namespace Movora.WebAPI
{
    public static class ModulePackage
    {
        public static void RegisterServices(IServiceCollection services)
        {
            // Register ILLM implementation
            services.AddSingleton<ILLM, Groq>();
            services.AddSingleton<IMovieService, TMDBService>();

            // Register other services here
        }
    }
}