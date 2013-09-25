using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Agent.Components.Functions.API
{
    interface IFunction
    {
        event EventHandler Activate;
        event EventHandler Deactivate;
        Boolean IsActive { get; }
        event EventHandler<EngagementActivity> NewActivity;
        String Name { get; }
        void Close();
    }
}
