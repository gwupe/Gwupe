using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using BlitsMe.Agent.Components.Person;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Roster
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
            ContactsView.SortDescriptions.Add(new SortDescription("Attendance.Name", ListSortDirection.Ascending));
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
                                ((attendance.Person.Firstname != null &&
                                  attendance.Person.Firstname.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Person.Lastname != null &&
                                  attendance.Person.Lastname.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Person.Username != null &&
                                  attendance.Person.Username.StartsWith(searchString, true,
                                      CultureInfo.CurrentUICulture)) ||
                                 (attendance.Person.Email != null &&
                                  attendance.Person.Email.StartsWith(searchString, true,
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


