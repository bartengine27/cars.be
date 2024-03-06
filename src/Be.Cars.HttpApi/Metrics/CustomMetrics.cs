using System.Diagnostics.Metrics;
using Volo.Abp.DependencyInjection;

namespace Be.Cars.Metrics
{
    /// <summary>
    /// Example of custom metrics based on <see href="https://community.abp.io/posts/asp.net-core-metrics-with-.net-8.0-1xnw1apc"/>.
    /// </summary>
    public class CustomMetrics : ISingletonDependency
    {

        public CustomMetrics(IMeterFactory meterFactory)
        {
            meter = meterFactory.Create("Be.Cars.Metrics.CustomMetrics");
            _carsCounter = meter.CreateCounter<long>("number_of_cars");
        }

        public void IncrementCarsCounter()
        {
            _carsCounter.Add(1);
        }

        private Meter meter;
        private readonly Counter<long> _carsCounter;
    }
}
