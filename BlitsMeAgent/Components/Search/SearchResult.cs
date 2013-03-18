using System.ComponentModel;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Agent.Components.Search
{
    public class SearchResult
    {
        private readonly Person.Person _person;

        public SearchResult(ResultElement resultElement)
        {
            _person = new Person.Person(resultElement.user);
        }

        public Person.Person Person
        {
            get { return _person; }
        }

        public string Username
        {
            get { return _person.Username; }
        }
    }
}