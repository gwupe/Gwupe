using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlitsMe.Agent.UI.WPF.Utils
{
    internal class InputValidator
    {
        private readonly TextBlock _statusText;
        private readonly TextBlock _errorText;

        internal InputValidator(TextBlock statusText, TextBlock errorText)
        {
            _statusText = statusText;
            _errorText = errorText;
        }

        public bool ValidateEmail(TextBox email, Label emailLabel)
        {
            bool dataOK = true;
            if (!Regex.IsMatch(email.Text.Trim(),
                               @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                               @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                               RegexOptions.IgnoreCase))
            {
                setError("Please enter a valid email address");
                email.Background = new SolidColorBrush(Colors.MistyRose);
                if(emailLabel != null)
                    emailLabel.Foreground = new SolidColorBrush(Colors.Red);
                dataOK = false;
            }
            ;
            return dataOK;
        }

        public bool ValidateFieldNonEmpty(Control control, string text, Label textLabel, string errorText, string defaultValue = "")
        {
            if (text.Equals(defaultValue) || String.IsNullOrWhiteSpace(text))
            {
                control.Background = new SolidColorBrush(Colors.MistyRose);
                if(textLabel != null)
                    textLabel.Foreground = new SolidColorBrush(Colors.Red);
                setError(errorText);
                control.Focus();
                Keyboard.Focus(control);
                return false;
            }
            return true;
        }

        public void setError(string error)
        {
            if (_statusText != null)
                _statusText.Visibility = Visibility.Hidden;
            if (_errorText != null)
            {
                _errorText.Text = error;
                _errorText.Visibility = Visibility.Visible;
            }
        }

        public void ResetStatus(Control[] textBoxes, Label[] labels)
        {
            for (int i = 0; i < (textBoxes.Length > labels.Length ? labels.Length : textBoxes.Length); i++)
            {
                if(textBoxes[i] != null)
                    textBoxes[i].Background = new SolidColorBrush(Colors.White);
                if(labels[i] != null)
                    labels[i].Foreground = new SolidColorBrush(Colors.Black);
            }
            if(_errorText != null)
                _errorText.Visibility = Visibility.Hidden;
            if(_statusText != null)
                _statusText.Visibility = Visibility.Hidden;
        }

        public bool ValidateFieldMatches(Control textBox, string text, Label label, string errorString, string placeHolder, string regEx)
        {
            bool dataOK = true;
            if (Regex.IsMatch(text.Trim(),
                               regEx,
                               RegexOptions.IgnoreCase))
            {
                setError(errorString);
                textBox.Background = new SolidColorBrush(Colors.MistyRose);
                if (label != null)
                    label.Foreground = new SolidColorBrush(Colors.Red);
                dataOK = false;
            }
            return dataOK;
        }
    }
}
