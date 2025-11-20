using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WhosThatPokemon.Services
{
    public class DataInitializerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public DataInitializerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000, stoppingToken);

            using (var scope = _serviceProvider.CreateScope())
            {
                var pokemonService = scope.ServiceProvider.GetRequiredService<IPokemonListService>();
                await pokemonService.InitializeDataAsync();
            }
        }
    }
}