using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using Gwupe.Agent.Components.Person;
using log4net;

namespace Gwupe.Agent.UI.WPF.Roster
{
    internal class SearchRosterList : RosterList
    {
        private readonly TextBox _searchTextBox;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SearchRosterList));

        public SearchRosterList(DispatchingCollection<ObservableCollection<Attendance>, Attendance> source,
            ListBox listBox, TextBox searchTextBox)
            : base(source, listBox)
        {
            _searchTextBox = searchTextBox;
            ContactsView.SortDescriptions.Add(new SortDescription("Person.Name", ListSortDirection.Ascending));
        }

        protected override void FilterEventHandler(object sender, FilterEventArgs filterEventArgs)
        {

            if (RosterCollection.Dispatcher.CheckAccess())
            {
                filterEventArgs.Accepted = false;
                if (!String.IsNullOrWhiteSpace(_searchTextBox.Text))
                {
                    string[] searchStrings = Regex.Split(_searchTextBox.Text, "\\s+");
                    foreach (var searchString in searchStrings)
                    {
                        if (!String.IsNullOrWhiteSpace(searchString))
                        {
                            Attendance attendance = filterEventArgs.Item as Attendance;

                            if (attendance != null &&
                                ((attendance.Party.Firstname != null &&
                                  attendance.Party.Firstname.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Party.Lastname != null &&
                                  attendance.Party.Lastname.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Party.Username != null &&
                                  attendance.Party.Username.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Party.Email != null &&
                                  attendance.Party.Email.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)))
                                )
                            {
                                filterEventArgs.Accepted = true;
                            }
                            else
                            {
                                filterEventArgs.Accepted = false;
                                break;
                            }
                        }
                        else
                        {
                            filterEventArgs.Accepted = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                RosterCollection.Dispatcher.Invoke(new Action(() => FilterEventHandler(sender, filterEventArgs)));
            }
        }

    }
}


