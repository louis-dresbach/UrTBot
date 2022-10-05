using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UrTBot
{
    public partial class Gear : Form
    {
        urt_server_bot theBot;
        Boolean dontRun = false;
        
        public Gear(urt_server_bot Bot)
        {
            InitializeComponent();

            theBot = Bot;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextBox();
        }

        private void UpdateTextBox()
        {
            if (!dontRun)
            {
                dontRun = true;
                int g_gear = 0;
                if (!checkBox1.Checked) g_gear += 1;
                if (!checkBox2.Checked) g_gear += 2;
                if (!checkBox3.Checked) g_gear += 4;
                if (!checkBox4.Checked) g_gear += 8;
                if (!checkBox6.Checked) g_gear += 16;
                if (!checkBox5.Checked) g_gear += 32;
                numericUpDown1.Value = g_gear;
                dontRun = false;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (!dontRun)
            {
                dontRun = true;
                decimal g_gear = numericUpDown1.Value;
                checkBox1.Checked = true;
                checkBox2.Checked = true;
                checkBox3.Checked = true;
                checkBox4.Checked = true;
                checkBox5.Checked = true;
                checkBox6.Checked = true;
                if (g_gear >= 32)
                {
                    g_gear -= 32;
                    checkBox5.Checked = false;
                }
                if (g_gear >= 16)
                {
                    g_gear -= 16;
                    checkBox6.Checked = false;
                }
                if (g_gear >= 8)
                {
                    g_gear -= 8;
                    checkBox4.Checked = false;
                }
                if (g_gear >= 4)
                {
                    g_gear -= 4;
                    checkBox3.Checked = false;
                }
                if (g_gear >= 2)
                {
                    g_gear -= 2;
                    checkBox2.Checked = false;
                }
                if (g_gear >= 1)
                {
                    g_gear -= 1;
                    checkBox1.Checked = false;
                }
                dontRun = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            theBot.sendRcon("@set g_gear" + numericUpDown1.Value.ToString());
            this.Close();
        }
    }
}
