using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace img2publish
{
    public partial class Form1 : Form
    {
        public delegate void MessageProcDeleg(params object [] p);
        public HashSet<string> exts = new HashSet<string>();
        public string mainDir = String.Empty;
        public string targetDir = String.Empty;

        public Form1()
        {
            InitializeComponent();
            textBox1.Text = Properties.Settings.Default.InputDir;
            textBox2.Text = Properties.Settings.Default.OutputDir;
            textBox3.Text = Properties.Settings.Default.Extensions;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox1.Text;
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox2.Text;
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!Directory.Exists(textBox1.Text))
            {
                SendMyMessage("print", "Directory ", textBox1.Text, " does not exists.");
                return;
            }

            if (!Directory.Exists(textBox2.Text))
            {
                SendMyMessage("print", "Directory ", textBox2.Text, " does not exists.");
                return;
            }

            Properties.Settings.Default.InputDir = textBox1.Text;
            Properties.Settings.Default.OutputDir = textBox2.Text;
            Properties.Settings.Default.Extensions = textBox3.Text;
            Properties.Settings.Default.Save();

            mainDir = textBox1.Text;
            targetDir = textBox2.Text;

            string[] ext2 = textBox3.Text.Split(',', ';');
            exts.Clear();
            foreach (string s in ext2)
            {
                exts.Add(s.Trim());
            }

            ProcessDirectory(textBox1.Text, textBox2.Text, false);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        public void SendMyMessage(params object[] p)
        {
            MessageProcDeleg mpd = new MessageProcDeleg(ReceiveMyMessage);
            //richTextBox1.Invoke(mpd, p);
        }

        private void ReceiveMyMessage(params object[] p)
        {
            if (p.Length > 0)
            {
                if (p[0] is string)
                {
                    string cmd = p[0] as string;
                    if (cmd == "print")
                    {
                        for (int i = 1; i < p.Length; i++)
                        {
                            richTextBox1.AppendText(p[i].ToString());
                        }
                    }
                }
            }
        }

        public string NormDir(string s)
        {
            if (s.StartsWith(mainDir))
                return s.Substring(mainDir.Length).Trim('\\');
            if (s.StartsWith(targetDir))
                return s.Substring(targetDir.Length).Trim('\\');
            return s;
        }

        public void ProcessDirectory(string pInpDir, string pOutDir, bool hasParent)
        {
            string firstFile = String.Empty;
            if (!Directory.Exists(pOutDir))
            {
                Directory.CreateDirectory(pOutDir);
            }

            File.WriteAllText(Path.Combine(pOutDir, "i0.html"), "<html><head><title>T</title></head><body><h1>No files</h1></body></html>");
            StringBuilder sb = new StringBuilder();
            StringBuilder sbc = new StringBuilder();

            sb.Append("<html><head><title>File list</title></head>");
            sb.Append("<body>");

            if (hasParent)
            {
                sb.Append("<p><a href=\"../index.html\" target=\"master\">[parent]</a></p>");
            }
            // processing directories
            string[] dirs = Directory.GetDirectories(pInpDir);
            if (dirs.Length > 0)
            {
                sb.Append("<h1>Directories</h1>");
                sb.Append("<table>\n");
                foreach (string dir in dirs)
                {
                    Debugger.Log(0, "", "Dir " + dir + "\n");
                    string nd = NormDir(dir);
                    string newDir = Path.Combine(targetDir, nd);
                    sb.AppendFormat("<tr><td><a href=\"{0}\" target=\"_top\">{1}</a></td></tr>\n",
                        Path.Combine(nd, "index.html"), nd);
                    ProcessDirectory(dir, newDir, true);
                }
                sb.Append("</table>");
            }


            string[] filesOrig = Directory.GetFiles(pInpDir);
            List<string> files = new List<string>();
            foreach(string f in filesOrig)
            {
                string ff = Path.GetFileName(f);
                if (ff.StartsWith(".") || ff.StartsWith("_"))
                    continue;
                string ee = Path.GetExtension(f);
                if (exts.Contains(ee))
                {
                    files.Add(f);
                }
            }
            if (files.Count > 0)
            {
                sb.Append("<h1>Files</h1>");
                sb.Append("<table>\n");
                // processing files
                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    SendMyMessage("print", "Processing file ", file);
                    sb.AppendFormat("<tr><td><a href=\"i{1}.html\" target=\"detail\">{0}</a></td></tr>\n", Path.GetFileName(file), i);

                    sbc.Clear();
                    sbc.Append("<html><head><title>" + NormDir(file) + "</title></head>");
                    sbc.Append("<body>");
                    sbc.Append("<p style='font-size:18pt;'>");
                    if (i > 0)
                    {
                        sbc.AppendFormat("<a href=\"i{0}.html\" target=\"detail\">&lt;&lt; Prev</a> | ", i - 1);
                    }
                    else
                    {
                        sbc.Append("&lt;&lt; Prev | ");
                    }
                    sbc.AppendFormat("<b> {0} </b>", NormDir(file));
                    if (i < files.Count - 1)
                    {
                        sbc.AppendFormat(" | <a href=\"i{0}.html\" target=\"detail\">Next &gt;&gt;</a>", i + 1);
                    }
                    else
                    {
                        sbc.Append(" | Next &gt;&gt;");
                    }
                    sbc.Append("</p>");
                    sbc.AppendFormat("<div><img style='width:100%' src=\"{0}\"></div>", Path.GetFileName(file));
                    sbc.Append("</body></html>");

                    File.WriteAllText(Path.Combine(pOutDir, "i" + i + ".html"), sbc.ToString());

                    string newFileName = Path.Combine(pOutDir, Path.GetFileName(file));
                    if (!File.Exists(newFileName))
                        File.Copy(file, newFileName);
                    SendMyMessage("print", ".. done\n");
                }
                sb.Append("</table>");
            }
            sb.Append("</body></html>");


            File.WriteAllText(Path.Combine(pOutDir, "master.html"), sb.ToString());

            sb.Clear();
            sb.Append("<html><head><title>Images in " + pInpDir + "</title></head>\n");
            sb.Append("<frameset cols=\"200,*\" frameborder=\"0\" border=\"0\" framespacing=\"4\">\n");
            sb.Append("<frame name=\"master\" src=\"master.html\" scrolling=\"auto\">\n");
            sb.Append("<frame name=\"detail\" src=\"i0.html\" scrolling=\"auto\">\n");
            sb.Append("</frameset></html>\n");

            File.WriteAllText(Path.Combine(pOutDir, "index.html"), sb.ToString());
        }
    }
}
