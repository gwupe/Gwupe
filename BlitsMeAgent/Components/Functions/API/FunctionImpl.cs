using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace BlitsMe.Agent.Components.Functions.API
{
    abstract class FunctionImpl : IFunction
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (FunctionImpl));
        public abstract string Name { get; }
        public abstract void Close();
        private bool _isActive;
        private bool _isUnderway;

        public bool IsActive
        {
            get { return _isActive; }
            protected set
            {
                if (_isActive != value)
                {
                    Logger.Debug(Name + " is now " + (value ? "Active" : "Inactive"));
                    _isActive = value;
                    if (value)
                        OnActivate(EventArgs.Empty);
                    else
                        OnDeactivate(EventArgs.Empty);
                }
            }
        }

        public bool IsUnderway
        {
            get { return _isUnderway; }
            set {
                if (_isUnderway != value)
                {
                    Logger.Debug(Name + " is now " + (value ? "underway" : "not underway"));
                    _isUnderway = value;
                    OnFunctionUnderwayChange(new FunctionUnderwayChangeArgs()
                    {
                        FunctionUnderway = value
                    });
                } }
        }

        public event EventHandler<FunctionUnderwayChangeArgs> FunctionUnderway;

        protected virtual void OnFunctionUnderwayChange(FunctionUnderwayChangeArgs e)
        {
            EventHandler<FunctionUnderwayChangeArgs> handler = FunctionUnderway;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<EngagementActivity> NewActivity;

        protected virtual void OnNewActivity(EngagementActivity e)
        {
            Logger.Debug("Raising a " + e);
            EventHandler<EngagementActivity> handler = NewActivity;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Activate;

        public void OnActivate(EventArgs e)
        {
            EventHandler handler = Activate;
            if (handler != null) handler(this, e);
        }

        public event EventHandler Deactivate;

        public void OnDeactivate(EventArgs e)
        {
            EventHandler handler = Deactivate;
            if (handler != null) handler(this, e);
        }
    }

    internal class FunctionUnderwayChangeArgs : EventArgs
    {
        public bool FunctionUnderway;
    }
}
