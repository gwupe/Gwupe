using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Alert
{
    internal class Alert
    {
        public NotificationManager Manager { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return "[Alert: " + Message + " ]";
        }
    }
}