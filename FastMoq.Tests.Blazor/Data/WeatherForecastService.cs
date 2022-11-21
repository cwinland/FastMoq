using Microsoft.AspNetCore.Components;
using System.IO.Abstractions;

namespace FastMoq.Tests.Blazor.Data
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private static readonly string[] Summaries = {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        // Test file system injection.
        [Inject]
        public IFileSystem FileSystem { get; set; }

        public Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate)
        {
            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray());
        }
    }

    public interface IWeatherForecastService
    {
        IFileSystem FileSystem { get; set; }
        Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate);
    }
}