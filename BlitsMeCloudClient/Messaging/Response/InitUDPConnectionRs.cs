using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class InitUDPConnectionRs : API.Response
    {
        public override String type
        {
            get { return "InitUDPConnection-RS"; }
            set { }
        }

        public InitUDPConnectionRs()
        {
            internalEndPoints = new List<IpEndPointElement>();
        }

        [DataMember]
        public String internalIP
        {
            get { return internalEndPoints == null || internalEndPoints.Count == 0 ? null : internalEndPoints[0].address; }
            set
            {
                if (internalEndPoints.Count == 0)
                {
                    internalEndPoints.Add(new IpEndPointElement() { address = value });
                }
                else
                {
                    internalEndPoints[0].address = value;
                }
            }
        }
        [DataMember]
        public int internalPort
        {
            get { return internalEndPoints == null || internalEndPoints.Count == 0 ? 0 : internalEndPoints[0].port; }
            set
            {
                if (internalEndPoints.Count == 0)
                {
                    internalEndPoints.Add(new IpEndPointElement() { port = value });
                }
                else
                {
                    internalEndPoints[0].port = value;
                }
            }

        }
        [DataMember]
        public List<IpEndPointElement> internalEndPoints { get; set; }
        [DataMember]
        public String externalIP
        {
            get { return externalEndPoint == null ? null : externalEndPoint.address; }
            set
            {
                if (externalEndPoint == null)
                {
                    externalEndPoint = new IpEndPointElement() { address = value };
                }
                else
                {
                    externalEndPoint.address = value;
                }
            }
        }
        [DataMember]
        public int externalPort
        {
            get { return externalEndPoint == null ? 0 : externalEndPoint.port; }
            set
            {
                if (externalEndPoint == null)
                {
                    externalEndPoint = new IpEndPointElement() { port = value };
                }
                else
                {
                    externalEndPoint.port = value;
                }
            }
        }

        [DataMember]
        public IpEndPointElement externalEndPoint { get; set; }

    }
}
