using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Cloud.Messaging.API
{
    public interface Processor
    {
        Response process(Request request);
    }
}
