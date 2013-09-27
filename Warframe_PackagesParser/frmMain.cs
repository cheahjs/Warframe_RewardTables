using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Warframe_PackagesParser
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ParseChunks();
            ParseLabels();
        }

        private void ParseChunks()
        {
            var chunks = File.ReadAllText("chunks.txt");
        }

        private void ParseLabels()
        {
            var labels = File.ReadAllText("labels.txt");
        }
    }
}
