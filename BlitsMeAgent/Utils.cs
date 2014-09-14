using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;


namespace BlitsMe.Agent
{
    class Utils
    {
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
                item.DropDownItems.Add(GenerateItem(subItem["text"].ToString(),(EventHandler)subItem["handler"]));
            }
            return item;
        }
    }
}
