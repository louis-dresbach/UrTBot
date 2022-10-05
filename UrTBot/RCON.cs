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
    public partial class RCON : Form
    {
        urt_server_bot theBot;
        
        public RCON()
        {
            InitializeComponent();
        }

        public void setBot(urt_server_bot bot)
        {
            theBot = bot;
        }

        private void RCON_Load(object sender, EventArgs e)
        {
            textBox2.Clear();
            textBox1.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox2.AppendText(theBot.sendRcon(textBox1.Text));
            textBox1.Clear();
        }
    }
}
