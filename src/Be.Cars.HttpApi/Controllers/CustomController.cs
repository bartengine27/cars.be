using Be.Cars.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Be.Cars.Controllers
{
    public class CustomController : CarsController
    {
        public CustomController(CustomMetrics customMetrics, ILogger logger) : base(customMetrics, logger)
        {
  
        }
    }
}
