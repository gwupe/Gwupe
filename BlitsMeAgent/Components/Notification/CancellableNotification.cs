using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BlitsMe.Agent.Components.Notification
{
    class CancellableNotification : Notification
    {
        public event EventHandler Cancelled;
        internal String CancelTooltip { get; set; }

        public void OnCancel(EventArgs e)
        {
            EventHandler handler = Cancelled;
            if (handler != null) handler(this, e);
        }

        internal CancellableNotification()
        {
            CancelTooltip = "Cancel";
        }

        private ICommand _cancelCommand;
        public ICommand Cancel
        {
            get { return _cancelCommand ?? (_cancelCommand = new CancelCommand(this.Manager, this)); }
        }
    }
}
