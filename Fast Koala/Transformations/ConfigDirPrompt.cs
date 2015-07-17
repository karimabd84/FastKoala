﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wijits.FastKoala.Transformations
{
    public partial class ConfigDirPrompt : Form
    {
        public ConfigDirPrompt()
        {
            InitializeComponent();
        }

        public string ConfigDir
        {
            get { return txtConfigDir.Text.Replace("/", "\\"); }
            set { txtConfigDir.Text = value; }
        }

        private bool ValidateConfigDir(string configDir)
        {
            // invalid: \ / : * ? " < > |
            // actually, allowing for "\" so that people can have multiple levels deep :D
            foreach (var c in configDir)
            {
                if (":*?\"<>|".Contains(c))
                {
                    MessageBox.Show(@"A folder name cannot contain any of the following characters:
\ / : * ? "" < > |", "Invalid folder name", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    txtConfigDir.Focus();
                    return false;
                }
            }
            return true;
        }

        private void txtConfigDir_Validating(object sender, CancelEventArgs e)
        {
            if (!ValidateConfigDir(ConfigDir))
            {
                e.Cancel = true;
            }
        }
    }
}
