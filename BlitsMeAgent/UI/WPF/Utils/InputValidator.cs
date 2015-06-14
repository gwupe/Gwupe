using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Gwupe.Agent.UI.WPF.Utils
{
    public class InputValidator
    {
        private readonly TextBlock _statusText;
        private readonly TextBlock _errorText;
        private readonly Dispatcher _dispatcher;
        private readonly bool _collapse;

        internal InputValidator(TextBlock statusText, TextBlock errorText, Dispatcher dispatcher, bool collapse = false)
        {
            _statusText = statusText;
            _errorText = errorText;
            _dispatcher = dispatcher;
            _collapse = collapse;
        }

        public bool ValidateEmail(TextBox email, Label emailLabel)
        {
            bool dataOK = true;
            if (_dispatcher.CheckAccess())
            {
                if (!Regex.IsMatch(email.Text.Trim(),
                    @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                    RegexOptions.IgnoreCase))
                {
                    SetError("Please enter a valid email address",email, emailLabel);
                    dataOK = false;
                }
            }
            else
            {
                _dispatcher.Invoke(new Action(() => { dataOK = ValidateEmail(email, emailLabel); }));
            }

            return dataOK;
        }

        private static void MarkErrorField(Control control, Label label = null)
        {
            control.Background = new SolidColorBrush(Colors.MistyRose);
            if (label != null)
                label.Foreground = new SolidColorBrush(Colors.Red);
            control.Focus();
            Keyboard.Focus(control);
        }

        public bool ValidateFieldNonEmpty(Control control, string text, Label textLabel, string errorText, string defaultValue = "")
        {
            if (_dispatcher.CheckAccess())
            {
                if (text.Equals(defaultValue) || String.IsNullOrWhiteSpace(text))
                {
                    SetError(errorText,control,textLabel);
                    return false;
                }
            }
            else
            {
                bool res = false;
                _dispatcher.Invoke(new Action(() =>
                {
                    res = ValidateFieldNonEmpty(control, text, textLabel, errorText, defaultValue);
                }));
                return res;
            }
            return true;
        }

        public void SetError(string error, Control control = null, Label label = null)
        {
            if (_dispatcher.CheckAccess())
            {
                if (_statusText != null)
                    _statusText.Visibility = _collapse ? Visibility.Collapsed : Visibility.Hidden;
                if (_errorText != null)
                {
                    _errorText.Text = error;
                    _errorText.Visibility = Visibility.Visible;
                }
                if (control != null)
                {
                    MarkErrorField(control, label);
                }
            }
            else
            {
                _dispatcher.Invoke(new Action(() => SetError(error, control, label)));
            }
        }

        public void ResetStatus(Control[] textBoxes = null, Label[] labels = null)
        {
            if (_dispatcher.CheckAccess())
            {
                if (textBoxes != null)
                {
                    foreach (Control t in textBoxes)
                    {
                        if (t != null)
                            t.Background = new SolidColorBrush(Colors.White);
                    }
                }
                if (textBoxes != null && labels != null)
                {
                    foreach (Label t in labels)
                    {
                        if (t != null)
                            t.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
                if (_errorText != null)
                    _errorText.Visibility = Visibility.Hidden;
                if (_statusText != null)
                    _statusText.Visibility = Visibility.Hidden;
            }
            else
            {
                _dispatcher.Invoke(new Action(() => ResetStatus(textBoxes, labels)));
            }
        }

        public bool ValidateFieldMatches(Control textBox, string text, Label label, string errorString, string placeHolder, string regEx)
        {
            bool dataOK = true;
            if (_dispatcher.CheckAccess())
            {
                if (Regex.IsMatch(text.Trim(),
                    regEx,
                    RegexOptions.IgnoreCase))
                {
                    SetError(errorString,textBox,label);
                    dataOK = false;
                }
            }
            else
            {
                _dispatcher.Invoke(new Action(() =>
                {
                    dataOK = ValidateFieldMatches(textBox, text, label, errorString, placeHolder, regEx);
                }));
            }
            return dataOK;
        }

        public void SetStatus(string status)
        {
            if (_dispatcher.CheckAccess())
            {
                if (_errorText != null)
                    _errorText.Visibility = _collapse ? Visibility.Collapsed : Visibility.Hidden;
                if (_statusText != null)
                {
                    _statusText.Text = status;
                    _statusText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _dispatcher.Invoke(new Action(() => SetStatus(status)));
            }
        }
    }
}
