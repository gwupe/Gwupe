using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Cloud.Messaging.API
{
    public interface Processor
    {
        Response process(Request request);
    }
}
