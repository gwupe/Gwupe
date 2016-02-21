using System;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using System.Windows.Threading;
using log4net;
using CheckBox = System.Windows.Controls.CheckBox;
using Control = System.Windows.Controls.Control;
using TextBox = System.Windows.Controls.TextBox;

namespace Gwupe.Agent
{
    public static class UiUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UiUtils));

        internal static T GetFieldValue<T>(Control control, Dispatcher dispatcher)
        {
            T fieldValue = default(T);
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(new Action(() => { fieldValue = GetFieldValue<T>(control ,dispatcher); }));
                return fieldValue;
            }
            else
            {
                try
                {
                    // TextBox
                    var textbox = control as TextBox;
                    if (textbox != null)
                        return (T)Convert.ChangeType(textbox.Text,typeof(T));

                    // Checkbox
                    var checkbox = control as CheckBox;
                    if (checkbox != null)
                        return (T)Convert.ChangeType(checkbox.IsChecked == true, typeof(T));

                    // PasswordBox
                    var passwordBox = control as PasswordBox;
                    if (passwordBox != null)
                        return (T)Convert.ChangeType(passwordBox.Password, typeof(T));
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get value of " + control, e);
                }
            }
            return fieldValue;
        }

        internal static ToolStripMenuItem GenerateItem(string text, EventHandler eventHandler)
        {
            var item = new ToolStripMenuItem(text);
            if (eventHandler != null) { item.Click += eventHandler; }
            return item;
        }

        internal static ToolStripMenuItem GenerateItem(string text, EventHandler eventHandler, ArrayList subItems)
        {
            var item = GenerateItem(text, eventHandler);
            foreach (Hashtable subItem in subItems)
            {
                item.DropDownItems.Add(GenerateItem(subItem["text"].ToString(), (EventHandler)subItem["handler"]));
            }
            return item;
        }
    }
}
