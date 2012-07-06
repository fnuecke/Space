using System;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    public partial class DataEditor : Form
    {
        public DataEditor()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
