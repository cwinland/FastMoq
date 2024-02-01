using Microsoft.AspNetCore.Components;
using System.IO.Abstractions;
#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests.Blazor.Data
{
    public class WeatherForecastService : IWeatherForecastService
    {
        #region Fields

        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        };

        #endregion

        #region IWeatherForecastService

        // Test file system injection.
        [Inject] public IFileSystem FileSystem { get; set; }

        public Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate) => Task.FromResult(Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = startDate.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                }
            ).ToArray()
        );

        #endregion
    }

    public interface IWeatherForecastService
    {
        #region Properties

        IFileSystem FileSystem { get; set; }

        #endregion

        Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate);
    }
}
