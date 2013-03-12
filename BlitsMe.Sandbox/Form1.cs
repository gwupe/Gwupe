using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlitsMe.Sandbox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            byte[] data = Encoding.UTF8.GetBytes("another string");
            test(ref data);
            
        }

        private void test(ref byte[] data)
        {
            data = Encoding.UTF8.GetBytes("the quick bornw");
        }

    }
}
