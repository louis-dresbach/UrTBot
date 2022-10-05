using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace UrTBot
{
    [global::System.Configuration.SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
    public partial class Bot : Form
    {
        urt_server_bot server_Bot;
        Timer Refresher = new Timer();

        public Bot()
        {
            InitializeComponent();
            Refresher.Interval = 3000;
            Refresher.Tick += new EventHandler(Refresher_Tick);
            Refresher.Start();

            textBox1.Text = Properties.Settings.Default.IP;
            textBox2.Text = Properties.Settings.Default.Port;
            textBox3.Text = Properties.Settings.Default.Password;
            textBox5.Text = Properties.Settings.Default.FtpUsername;
            textBox6.Text = Properties.Settings.Default.FtpPassword;

            if (!Properties.Settings.Default.IsFtp)
            {
                textBox4.Text = Properties.Settings.Default.LocalPath;
                radioButton1.Checked = true;
                radioButton2.Checked = false;
            }
            else
            {
                textBox4.Text = Properties.Settings.Default.FtpPath;
                radioButton1.Checked = false;
                radioButton2.Checked = true;
            }
        }

        void Refresher_Tick(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            foreach (Player p in server_Bot.Players)
            {
                listBox1.Items.Add(p.CleanName);
            }
            button2.Enabled = button3.Enabled = linkLabel1.Enabled = server_Bot.Connected;
            if (server_Bot.Running)
            {
                tabControl1.SelectedTab.Controls["label4"].Text = "Online";
                tabControl1.SelectedTab.Controls["label4"].ForeColor = Color.YellowGreen;
                tabControl1.SelectedTab.Controls["button2"].Text = "Stop Bot";
                tabControl1.SelectedTab.Controls["label11"].Text = server_Bot.CurrentMap;
                tabControl1.SelectedTab.Controls["label12"].Text = server_Bot.Nextmap;
                tabControl1.SelectedTab.Controls["button6"].Enabled = false;
                tabControl1.SelectedTab.Controls["radioButton1"].Enabled = false;
                tabControl1.SelectedTab.Controls["radioButton2"].Enabled = false;
            }
            else
            {
                tabControl1.SelectedTab.Controls["button2"].Text = "Start Bot";
                tabControl1.SelectedTab.Controls["label4"].Text = "Offline";
                tabControl1.SelectedTab.Controls["label4"].ForeColor = Color.Maroon;
                tabControl1.SelectedTab.Controls["label11"].Text = "-";
                tabControl1.SelectedTab.Controls["button6"].Enabled = true;
                tabControl1.SelectedTab.Controls["radioButton1"].Enabled = true;
                tabControl1.SelectedTab.Controls["radioButton2"].Enabled = true;
            }
        }

        private void Bot_Load(object sender, EventArgs e)
        {
            server_Bot = new urt_server_bot();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            server_Bot.RCON_IP = textBox1.Text;
            server_Bot.RCON_PORT = textBox2.Text;
            server_Bot.RCON_PASSWORD = textBox3.Text;
            server_Bot.checkConnection();
            Properties.Settings.Default.IP = textBox1.Text;
            Properties.Settings.Default.Port = textBox2.Text;
            Properties.Settings.Default.Password = textBox3.Text;
            Properties.Settings.Default.Save();

            if (server_Bot.Connected)
            {
                this.Text = "UrT Server Bot - Connected to " + server_Bot.RCON_IP + ":" + server_Bot.RCON_PORT;
                button2.Enabled = true;
            }
            else
            {
                this.Text = "UrT Server Bot - Not Connected";
                button2.Enabled = false;
                MessageBox.Show("Failed to connect.");
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RCON window = new RCON();
            window.setBot(server_Bot);
            window.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (server_Bot.Connected && button2.Text == "Start Bot" && !IsFileLocked(textBox4.Text))
            {
                if (radioButton1.Checked)
                {
                    server_Bot.Start(textBox4.Text);
                    Properties.Settings.Default.LocalPath = textBox4.Text;
                }
                else
                {
                    if (!textBox4.Text.StartsWith("ftp://")) textBox4.Text = "ftp://" + textBox4.Text;
                    server_Bot.Start(textBox4.Text, textBox5.Text, textBox6.Text);
                    Properties.Settings.Default.FtpPath = textBox4.Text;
                }
                Properties.Settings.Default.FtpUsername = textBox5.Text;
                Properties.Settings.Default.FtpPassword = textBox6.Text;
                Properties.Settings.Default.IsFtp = radioButton2.Checked;
                Properties.Settings.Default.Save();
                radioButton1.Enabled = false;
                radioButton2.Enabled = false;
                textBox4.ReadOnly = true;
                textBox5.ReadOnly = true;
                textBox6.ReadOnly = true;
                label4.Text = "Online";
                label4.ForeColor = Color.YellowGreen;
                button2.Text = "Stop Bot";

                listBox1.Items.Clear();
                foreach (Player p in server_Bot.Players)
                {
                    listBox1.Items.Add(p.CleanName);
                }
            }
            else
            {
                button6.Enabled = true;
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                textBox4.ReadOnly = false;
                textBox5.ReadOnly = false;
                textBox6.ReadOnly = false;
                server_Bot.Stop();
                button2.Text = "Start Bot";
                label4.Text = "Offline";
                label4.ForeColor = Color.Maroon;
            }
        }

        protected virtual bool IsFileLocked(String pathToFile)
        {
            try
            {
                FileStream stream = null;
                FileInfo file = new FileInfo(pathToFile);

                try
                {
                    stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                catch (IOException)
                {
                    //the file is unavailable because it is:
                    //still being written to
                    //or being processed by another thread
                    //or does not exist (has already been processed)
                    return true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }

                //file is not locked
                return false;
            }
            catch { return false; }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new Levels().ShowDialog();
            server_Bot.LoadPlayerLevels();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new Gear(server_Bot).ShowDialog();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                Player p = server_Bot.FindPlayer(listBox1.SelectedItem.ToString());
                if (p != null)
                {
                    label9.Text = p.IP;
                    numericUpDown1.Value = p.Level;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (numericUpDown1.Value == 0)
            {
                if (server_Bot.PlayerLevels[label9.Text] != null && server_Bot.PlayerLevels[label9.Text] != String.Empty)
                {
                    server_Bot.PlayerLevels.Remove(label9.Text);
                    server_Bot.Players.Find(delegate(Player pl) { return pl.IP==label9.Text; }).Level = 0;
                }
            }
            else
            {
                server_Bot.PlayerLevels[label9.Text] = numericUpDown1.Value.ToString();
                server_Bot.Players.Find(delegate(Player pl) { return pl.IP == label9.Text; }).Level = Convert.ToInt32(numericUpDown1.Value);
            }
            server_Bot.SavePlayerLevels();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            new PlayerPositions(server_Bot).Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.CheckFileExists = true;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = fd.FileName;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                label14.Visible = false;
                label15.Visible = false;
                textBox5.Visible = false;
                textBox6.Visible = false;
                button2.Location = new Point(12, 142);
                button7.Location = new Point(327, 142);
                button7.Visible = true;
                textBox4.Text = Properties.Settings.Default.LocalPath;
            }
            else
            {
                label14.Visible = true;
                label15.Visible = true;
                textBox5.Visible = true;
                textBox6.Visible = true;
                button2.Location = new Point(12, 166);
                button7.Location = new Point(327, 166);
                button7.Visible = false;
                textBox4.Text = Properties.Settings.Default.FtpPath;
            }
        }

        private void Bot_FormClosing(object sender, FormClosingEventArgs e)
        {
            server_Bot.Stop();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == tabControl1.TabCount - 1)
            {
                tabControl1.SelectedIndex--;

                TabPage tp = new TabPage("Server " + tabControl1.TabCount);
                tp.Location = new System.Drawing.Point(4, 22);
                tp.Name = "tabPage + tabControl1.TabCount";
                tp.Padding = new System.Windows.Forms.Padding(3);
                tp.Size = new System.Drawing.Size(681, 513);
                tp.UseVisualStyleBackColor = true;
                Label label15 = new Label();
                Label label14 = new Label();
                Label label12 = new Label();
                Label label13 = new Label();
                Label label11 = new Label();
                Label label10 = new Label();
                Label label9 = new Label();
                Label label8 = new Label();
                Label label7 = new Label();
                Label label6 = new Label();
                Label label5 = new Label();
                Label label4 = new Label();
                Label label3 = new Label();
                Label label2 = new Label();
                Label label1 = new Label();
                TextBox textBox6 = new TextBox();
                TextBox textBox5 = new TextBox();
                TextBox textBox4 = new TextBox();
                TextBox textBox3 = new TextBox();
                TextBox textBox2 = new TextBox();
                TextBox textBox1 = new TextBox();
                RadioButton radioButton2 = new RadioButton();
                RadioButton radioButton1 = new RadioButton();
                LinkLabel linkLabel2 = new LinkLabel();
                LinkLabel linkLabel1 = new LinkLabel();
                Button button7 = new Button();
                Button button6 = new Button();
                Button button5 = new Button();
                Button button4 = new Button();
                Button button3 = new Button();
                Button button2 = new Button();
                Button button1 = new Button();
                NumericUpDown numericUpDown1 = new NumericUpDown();
                ListBox listBox1 = new ListBox();
                // 
                // label15
                // 
                label15.AutoSize = true;
                label15.Location = new System.Drawing.Point(229, 145);
                label15.Name = "label15";
                label15.Size = new System.Drawing.Size(60, 12);
                label15.TabIndex = 67;
                label15.Text = "Password: ";
                label15.Visible = false;
                // 
                // textBox6
                // 
                textBox6.Location = new System.Drawing.Point(295, 142);
                textBox6.Name = "textBox6";
                textBox6.Size = new System.Drawing.Size(100, 19);
                textBox6.TabIndex = 66;
                textBox6.Visible = false;
                // 
                // label14
                // 
                label14.AutoSize = true;
                label14.Location = new System.Drawing.Point(8, 145);
                label14.Name = "label14";
                label14.Size = new System.Drawing.Size(62, 12);
                label14.TabIndex = 65;
                label14.Text = "Username: ";
                label14.Visible = false;
                // 
                // textBox5
                // 
                textBox5.Location = new System.Drawing.Point(83, 142);
                textBox5.Name = "textBox5";
                textBox5.Size = new System.Drawing.Size(100, 19);
                textBox5.TabIndex = 64;
                textBox5.Visible = false;
                // 
                // radioButton2
                // 
                radioButton2.AutoSize = true;
                radioButton2.Location = new System.Drawing.Point(68, 95);
                radioButton2.Name = "radioButton2";
                radioButton2.Size = new System.Drawing.Size(44, 16);
                radioButton2.TabIndex = 63;
                radioButton2.Text = "FTP";
                radioButton2.UseVisualStyleBackColor = true;
                radioButton2.CheckedChanged += new System.EventHandler(radioButton2_CheckedChanged);
                // 
                // radioButton1
                // 
                radioButton1.AutoSize = true;
                radioButton1.Checked = true;
                radioButton1.Location = new System.Drawing.Point(12, 95);
                radioButton1.Name = "radioButton1";
                radioButton1.Size = new System.Drawing.Size(50, 16);
                radioButton1.TabIndex = 62;
                radioButton1.TabStop = true;
                radioButton1.Text = "Local";
                radioButton1.UseVisualStyleBackColor = true;
                // 
                // button7
                // 
                button7.Location = new System.Drawing.Point(322, 143);
                button7.Name = "button7";
                button7.Size = new System.Drawing.Size(73, 23);
                button7.TabIndex = 61;
                button7.Text = "Browse..";
                button7.UseVisualStyleBackColor = true;
                button7.Click += new System.EventHandler(button7_Click);
                // 
                // label12
                // 
                label12.AutoSize = true;
                label12.Location = new System.Drawing.Point(505, 361);
                label12.Name = "label12";
                label12.Size = new System.Drawing.Size(11, 12);
                label12.TabIndex = 60;
                label12.Text = "-";
                // 
                // label13
                // 
                label13.AutoSize = true;
                label13.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
                label13.Location = new System.Drawing.Point(409, 357);
                label13.Name = "label13";
                label13.Size = new System.Drawing.Size(90, 16);
                label13.TabIndex = 59;
                label13.Text = "Next Map: ";
                // 
                // label11
                // 
                label11.AutoSize = true;
                label11.Location = new System.Drawing.Point(232, 361);
                label11.Name = "label11";
                label11.Size = new System.Drawing.Size(11, 12);
                label11.TabIndex = 58;
                label11.Text = "-";
                // 
                // label10
                // 
                label10.AutoSize = true;
                label10.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
                label10.Location = new System.Drawing.Point(189, 357);
                label10.Name = "label10";
                label10.Size = new System.Drawing.Size(49, 16);
                label10.TabIndex = 57;
                label10.Text = "Map: ";
                // 
                // button6
                // 
                button6.Location = new System.Drawing.Point(554, 117);
                button6.Name = "button6";
                button6.Size = new System.Drawing.Size(112, 23);
                button6.TabIndex = 56;
                button6.Text = "Player Positions";
                button6.UseVisualStyleBackColor = true;
                button6.Click += new System.EventHandler(button6_Click);
                // 
                // button5
                // 
                button5.Location = new System.Drawing.Point(554, 88);
                button5.Name = "button5";
                button5.Size = new System.Drawing.Size(112, 23);
                button5.TabIndex = 55;
                button5.Text = "Spam messages";
                button5.UseVisualStyleBackColor = true;
                button5.Click += new System.EventHandler(button5_Click);
                // 
                // button4
                // 
                button4.Location = new System.Drawing.Point(83, 451);
                button4.Name = "button4";
                button4.Size = new System.Drawing.Size(100, 23);
                button4.TabIndex = 54;
                button4.Text = "Save";
                button4.UseVisualStyleBackColor = true;
                button4.Click += new System.EventHandler(button4_Click);
                // 
                // numericUpDown1
                // 
                numericUpDown1.Location = new System.Drawing.Point(83, 426);
                numericUpDown1.Name = "numericUpDown1";
                numericUpDown1.Size = new System.Drawing.Size(100, 19);
                numericUpDown1.TabIndex = 53;
                // 
                // label9
                // 
                label9.AutoSize = true;
                label9.Location = new System.Drawing.Point(85, 403);
                label9.Name = "label9";
                label9.Size = new System.Drawing.Size(0, 12);
                label9.TabIndex = 52;
                // 
                // label8
                // 
                label8.AutoSize = true;
                label8.Location = new System.Drawing.Point(50, 403);
                label8.Name = "label8";
                label8.Size = new System.Drawing.Size(21, 12);
                label8.TabIndex = 51;
                label8.Text = "IP: ";
                // 
                // label7
                // 
                label7.AutoSize = true;
                label7.Location = new System.Drawing.Point(39, 428);
                label7.Name = "label7";
                label7.Size = new System.Drawing.Size(38, 12);
                label7.TabIndex = 50;
                label7.Text = "Level: ";
                // 
                // button3
                // 
                button3.Enabled = false;
                button3.Location = new System.Drawing.Point(554, 58);
                button3.Name = "button3";
                button3.Size = new System.Drawing.Size(112, 23);
                button3.TabIndex = 49;
                button3.Text = "Change g_gear";
                button3.UseVisualStyleBackColor = true;
                button3.Click += new System.EventHandler(button3_Click);
                // 
                // label6
                // 
                label6.AutoSize = true;
                label6.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
                label6.Location = new System.Drawing.Point(9, 376);
                label6.Name = "label6";
                label6.Size = new System.Drawing.Size(148, 16);
                label6.TabIndex = 48;
                label6.Text = "Players on server:";
                // 
                // listBox1
                // 
                listBox1.FormattingEnabled = true;
                listBox1.ItemHeight = 12;
                listBox1.Location = new System.Drawing.Point(189, 376);
                listBox1.Name = "listBox1";
                listBox1.Size = new System.Drawing.Size(474, 88);
                listBox1.TabIndex = 47;
                listBox1.SelectedIndexChanged += new System.EventHandler(listBox1_SelectedIndexChanged);
                // 
                // linkLabel2
                // 
                linkLabel2.AutoSize = true;
                linkLabel2.Location = new System.Drawing.Point(539, 487);
                linkLabel2.Name = "linkLabel2";
                linkLabel2.Size = new System.Drawing.Size(127, 12);
                linkLabel2.TabIndex = 46;
                linkLabel2.TabStop = true;
                linkLabel2.Text = "Edit Player Permissions";
                linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel2_LinkClicked);
                // 
                // button2
                // 
                button2.Enabled = false;
                button2.Location = new System.Drawing.Point(7, 143);
                button2.Name = "button2";
                button2.Size = new System.Drawing.Size(75, 23);
                button2.TabIndex = 34;
                button2.Text = "Start Bot";
                button2.UseVisualStyleBackColor = true;
                button2.Click += new System.EventHandler(button2_Click);
                // 
                // textBox4
                // 
                textBox4.Location = new System.Drawing.Point(7, 117);
                textBox4.Name = "textBox4";
                textBox4.Size = new System.Drawing.Size(388, 19);
                textBox4.TabIndex = 45;
                textBox4.Text = "C:\\Users\\Hiroyuki\\Documents\\UrbanTerror\\q3ut4\\thisserver.log";
                // 
                // label5
                // 
                label5.AutoSize = true;
                label5.Location = new System.Drawing.Point(10, 79);
                label5.Name = "label5";
                label5.Size = new System.Drawing.Size(103, 12);
                label5.TabIndex = 44;
                label5.Text = "Path to server log: ";
                // 
                // label4
                // 
                label4.AutoSize = true;
                label4.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
                label4.ForeColor = System.Drawing.Color.Maroon;
                label4.Location = new System.Drawing.Point(7, 59);
                label4.Name = "label4";
                label4.Size = new System.Drawing.Size(58, 16);
                label4.TabIndex = 43;
                label4.Text = "Offline";
                // 
                // linkLabel1
                // 
                linkLabel1.AutoSize = true;
                linkLabel1.Location = new System.Drawing.Point(8, 487);
                linkLabel1.Name = "linkLabel1";
                linkLabel1.Size = new System.Drawing.Size(66, 12);
                linkLabel1.TabIndex = 42;
                linkLabel1.TabStop = true;
                linkLabel1.Text = "Send RCON";
                linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
                // 
                // button1
                // 
                button1.Location = new System.Drawing.Point(554, 9);
                button1.Name = "button1";
                button1.Size = new System.Drawing.Size(112, 23);
                button1.TabIndex = 41;
                button1.Text = "Connect";
                button1.UseVisualStyleBackColor = true;
                button1.Click += new System.EventHandler(button1_Click_1);
                // 
                // textBox3
                // 
                textBox3.Location = new System.Drawing.Point(345, 11);
                textBox3.Name = "textBox3";
                textBox3.PasswordChar = '*';
                textBox3.Size = new System.Drawing.Size(203, 19);
                textBox3.TabIndex = 40;
                textBox3.Text = "password";
                // 
                // label3
                // 
                label3.AutoSize = true;
                label3.Location = new System.Drawing.Point(279, 14);
                label3.Name = "label3";
                label3.Size = new System.Drawing.Size(60, 12);
                label3.TabIndex = 39;
                label3.Text = "Password: ";
                // 
                // textBox2
                // 
                textBox2.Location = new System.Drawing.Point(192, 11);
                textBox2.Name = "textBox2";
                textBox2.Size = new System.Drawing.Size(42, 19);
                textBox2.TabIndex = 38;
                textBox2.Text = "27960";
                // 
                // label2
                // 
                label2.AutoSize = true;
                label2.Location = new System.Drawing.Point(164, 14);
                label2.Name = "label2";
                label2.Size = new System.Drawing.Size(32, 12);
                label2.TabIndex = 37;
                label2.Text = "Port: ";
                // 
                // label1
                // 
                label1.AutoSize = true;
                label1.Location = new System.Drawing.Point(8, 14);
                label1.Name = "label1";
                label1.Size = new System.Drawing.Size(44, 12);
                label1.TabIndex = 36;
                label1.Text = "Server: ";
                // 
                // textBox1
                // 
                textBox1.Location = new System.Drawing.Point(58, 11);
                textBox1.Name = "textBox1";
                textBox1.Size = new System.Drawing.Size(100, 19);
                textBox1.TabIndex = 35;
                textBox1.Text = "127.0.0.1";
                tp.Controls.Add(label15);
                tp.Controls.Add(textBox6);
                tp.Controls.Add(label14);
                tp.Controls.Add(textBox5);
                tp.Controls.Add(radioButton2);
                tp.Controls.Add(radioButton1);
                tp.Controls.Add(button7);
                tp.Controls.Add(label12);
                tp.Controls.Add(label13);
                tp.Controls.Add(label11);
                tp.Controls.Add(label10);
                tp.Controls.Add(button6);
                tp.Controls.Add(button5);
                tp.Controls.Add(button4);
                tp.Controls.Add(numericUpDown1);
                tp.Controls.Add(label9);
                tp.Controls.Add(label8);
                tp.Controls.Add(label7);
                tp.Controls.Add(button3);
                tp.Controls.Add(label6);
                tp.Controls.Add(listBox1);
                tp.Controls.Add(linkLabel2);
                tp.Controls.Add(button2);
                tp.Controls.Add(textBox4);
                tp.Controls.Add(label5);
                tp.Controls.Add(label4);
                tp.Controls.Add(linkLabel1);
                tp.Controls.Add(button1);
                tp.Controls.Add(textBox3);
                tp.Controls.Add(label3);
                tp.Controls.Add(textBox2);
                tp.Controls.Add(label2);
                tp.Controls.Add(label1);
                tp.Controls.Add(textBox1);

                tabControl1.TabPages.Insert(tabControl1.SelectedIndex + 1, tp);
                tabControl1.SelectedIndex++;
            }
        }
    }
}
