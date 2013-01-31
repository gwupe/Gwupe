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
        internal static ToolStripMenuItem generateItem(string text, EventHandler eventHandler)
        {
            var item = new ToolStripMenuItem(text);
            if (eventHandler != null) { item.Click += eventHandler; }
            return item;
        }

        internal static ToolStripMenuItem generateItem(string text, EventHandler eventHandler, ArrayList subItems)
        {
            var item = generateItem(text, eventHandler);
            foreach (Hashtable subItem in subItems)
            {
                item.DropDownItems.Add(generateItem(subItem["text"].ToString(),(EventHandler)subItem["handler"]));
            }
            return item;
        }
    }
}
