using System;

namespace BlitsMe.Cloud.Messaging.Response
{
    public class NotifyChangeRs : API.Response
    {
        public override String type
        {
            get { return "NotifyChange-RS"; }
            set { }
        }
    }
}
