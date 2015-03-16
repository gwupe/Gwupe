using System.ComponentModel;
using Gwupe.Agent.Annotations;
using Gwupe.Cloud.Messaging.Elements;

namespace Gwupe.Agent.Components.Person
{
    public class Relationship : INotifyPropertyChanged
    {
        private bool _iHaveUnattendedAccess;
        private bool _theyHaveUnattendedAccess;

        public static Relationship NoRelationship { get { return new Relationship() { _iHaveUnattendedAccess = false, _theyHaveUnattendedAccess = false }; } }

        public Relationship(RelationshipElement relationshipElement)
        {
            InitRelationship(relationshipElement);
        }

        public Relationship() { }

        public bool IHaveUnattendedAccess
        {
            get { return _iHaveUnattendedAccess; }
            set { _iHaveUnattendedAccess = value; OnPropertyChanged("IHaveUnattendedAccess"); }
        }

        public bool TheyHaveUnattendedAccess
        {
            get { return _theyHaveUnattendedAccess; }
            set { _theyHaveUnattendedAccess = value; OnPropertyChanged("TheyHaveUnattendedAccess"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void InitRelationship(RelationshipElement relationshipElement)
        {
            IHaveUnattendedAccess = relationshipElement.ihaveUnattendedAccess;
            TheyHaveUnattendedAccess = relationshipElement.theyHaveUnattendedAccess;
        }
    }
}