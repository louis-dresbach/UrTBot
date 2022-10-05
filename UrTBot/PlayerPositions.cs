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
    public partial class PlayerPositions : Form
    {
        Timer updater;
        urt_server_bot bot;

        int MapWidth = 5650;
        int MapHeight = 7850;

        public PlayerPositions(urt_server_bot Bot)
        {
            if (Properties.Resources.ResourceManager.GetObject(Bot.CurrentMap) != null)
            {
                InitializeComponent();
                bot = Bot;

                Image Minimap = (Image)Properties.Resources.ResourceManager.GetObject(Bot.CurrentMap);
                this.BackgroundImage = Minimap;
                this.Height = Minimap.Height;
                this.Width = Minimap.Width;

                updater = new Timer();
                updater.Interval = 2500;
                updater.Tick += new EventHandler(updater_Tick);
                updater.Start();

                for (int i = 0; i < 24; i++)
                {
                    PictureBox p = new PictureBox();
                    p.Name = "PictureBox" + i.ToString();
                    p.Height = p.Width = 10;
                    this.Controls.Add(p);
                }
            }
        }

        void updater_Tick(object sender, EventArgs e)
        {
            String[] Positions = bot.sendRcon("positions").Remove(0, 80).Replace("      ", " ").Replace("     ", " ").Replace("    ", " ").Replace("   ", " ").Replace("  ", " ").TrimStart('\n').Split('\n');
            foreach (String player in Positions)
            {
                if (player.Length > 0)
                {
                    String[] Infos = player.Trim().Split(' ');
                    int Team = bot.FindPlayer(Infos[0]).Team; // 1=Red, 2=Blue
                    PictureBox p = (PictureBox)this.Controls["PictureBox" + Infos[0]];
                    if (p != null)
                    {
                        p.Left = Convert.ToInt32(Infos[1].Split('.')[0]) / MapWidth * this.Width + 90;
                        p.Top = -Convert.ToInt32(Infos[2].Split('.')[0]) / MapHeight * this.Height + 67;
                        p.BackColor = Color.DarkRed;
                        if (Team == 2) p.BackColor = Color.DarkBlue;
                    }
                }
            }
        }
    }
}
