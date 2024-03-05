using System.Diagnostics.Metrics;

namespace Be.Cars.Metrics
{
    /// <summary>
    /// Example of custom metrics based on <see href="https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc"/>
    /// </summary>
    public class CustomMetrics
    {

        public CustomMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("Be.Cars.Metrics.CustomMetrics");
            _carsCounter = meter.CreateCounter<long>("number_of_cars");
        }

        public void IncrementCarsCounter()
        {
            _carsCounter.Add(1);
        }

        private readonly Counter<long> _carsCounter;
    }
}
