using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace RayModelApp
{
    public partial class FrmTraectories : Form
    {
        private BindingSource bs;
        public List<Point3D> points;
        public FrmTraectories()
        {
            InitializeComponent();
            string[] fs = Directory.GetFiles(Environment.CurrentDirectory, "*.tra");
            listBox1.Items.Clear();
            foreach (string s in fs)
                listBox1.Items.Add(Path.GetFileName(s));

            bs = new BindingSource();
            bs.DataSource = fs;
            bindingNavigator1.BindingSource = bs;
            bs.CurrentItemChanged += Bs_CurrentItemChanged;
            points = new List<Point3D>();

        }

        private void Bs_CurrentItemChanged(object sender, EventArgs e)
        {           
            Console.WriteLine(bs.Current);
            DrawTraectory(bs.Current.ToString());
        }

        private void DrawTraectory(string filename)
        {
            using (TextReader tr = File.OpenText(filename))
            {
                points.Clear();
                chart1.Series[0].Points.Clear();
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    int x1, y1, z1;
                    string[] vals = line.Split(';');
                    int.TryParse(vals[0], out x1);
                    int.TryParse(vals[1], out y1);
                    int.TryParse(vals[2], out z1);
                    chart1.Series[0].Points.AddXY(x1, y1);
                    points.Add(new Point3D() { X = x1, Y = y1, Z = z1 });
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawTraectory(listBox1.SelectedItem.ToString());
        }
    }
}
