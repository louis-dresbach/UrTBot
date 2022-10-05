using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace UrTBot
{
    public partial class Levels : Form
    {
        StringDictionary PlayerLevels;

        public Levels()
        {
            InitializeComponent();

            PlayerLevels = LoadPlayerLevels();

            foreach (DictionaryEntry de in PlayerLevels)
            {
                dataGridView1.Rows.Add(de.Key.ToString(), de.Value.ToString());
            }
        }

        StringDictionary LoadPlayerLevels()
        {
            if (Properties.Settings.Default.PlayerLevels == String.Empty) { return new StringDictionary(); }
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(Properties.Settings.Default.PlayerLevels)))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (StringDictionary)bf.Deserialize(ms);
            }
        }

        void SavePlayerLevels()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, PlayerLevels);
                    ms.Position = 0;
                    byte[] buffer = new byte[(int)ms.Length];
                    ms.Read(buffer, 0, buffer.Length);
                    Properties.Settings.Default.PlayerLevels = Convert.ToBase64String(buffer);
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PlayerLevels.Clear();
            for(int i=0; i<dataGridView1.Rows.Count-1; i++) 
            {
                String Key = dataGridView1.Rows[i].Cells[0].Value.ToString();
                String Value = dataGridView1.Rows[i].Cells[1].Value.ToString();
                PlayerLevels.Add(Key, Value);
            }
            SavePlayerLevels();
            this.Close();
        }
    }
}
