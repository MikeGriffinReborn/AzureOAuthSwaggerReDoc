using System;

namespace AzureOAuthSwaggerReDoc
{
    public class WeatherForecast
    {
        /// <summary>
        /// Date of Forecast
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Temperature in Celsius
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperature in Fahrenheit
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// The Forecast
        /// </summary>
        public string Summary { get; set; }
    }
}
