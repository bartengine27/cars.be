using Be.Cars.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Be.Cars.Controllers
{
    public class CustomController : CarsController
    {
        public CustomController(CustomMetrics customMetrics) : base(customMetrics)
        {
        }
    }
}
