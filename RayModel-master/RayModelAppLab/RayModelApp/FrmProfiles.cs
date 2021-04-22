using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace RayModelApp
{
    public partial class FrmProfiles : Form
    {
        public Profile p { get; set; }
        bool bPress = false;
        int ser=0;
        int pIndex = -1;
        ProfilePoint pp = new ProfilePoint();
        public FrmProfiles()
        {

            InitializeComponent();
            p = new Profile();
            LoadProfiles();
            for (int i = 0; i < chart1.ChartAreas.Count; i++)
            {
                
                chart1.ChartAreas[i].Position.Y = 0;
                chart1.ChartAreas[i].Position.Width = 30.0f;
                chart1.ChartAreas[i].Position.Height = 100;
            }
            chart1.ChartAreas[0].Position.X = 0;
            chart1.ChartAreas[1].Position.X = 33;
            chart1.ChartAreas[2].Position.X = 67;
        }
        private void LoadProfiles()
        {
            listBox1.Items.Clear();
            string[] fs = Directory.GetFiles(Environment.CurrentDirectory, "*.pro");
            foreach (string f in fs)
                listBox1.Items.Add(Path.GetFileName(f));
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            p.Points.Clear();
            using (TextReader tr = File.OpenText(listBox1.SelectedItem.ToString()))
            {
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    int z1; 
                    float c1, s1, t1;
                    string[] vals = line.Split(';');
                    int.TryParse(vals[0], out z1);
                    float.TryParse(vals[1], out c1);
                    float.TryParse(vals[2], out s1);
                    float.TryParse(vals[3], out t1);
                    p.Points.Add(new ProfilePoint() { z = z1, c = c1, p = s1, t = t1 });

                }
                MakeChartsFromPoints();
            }
            dataGridView1.DataSource = p.Points;
            foreach (DataGridViewColumn c in dataGridView1.Columns)
                c.Width = 50;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((bPress == true) && (pIndex>-1))
            {
                HitTestResult hit = chart1.HitTest(e.X, e.Y);
                double cx = hit.ChartArea.AxisX.PixelPositionToValue(e.X);
                pp = p.Points[pIndex];
                switch (ser)
                {
                    case 0:
                        pp.c = (float)cx;
                        break;
                    case 1:
                        pp.t = (float)cx;
                        break;
                    case 2:
                        pp.p = (float)cx;
                        break;
                }
                chart1.Series[ser].Points[pIndex].XValue = cx;
                p.Points[pIndex] = pp;                
                Console.WriteLine(ser.ToString() + " " + pIndex.ToString()+" "+cx.ToString());
                dataGridView1.DataSource = p.Points;
                foreach (DataGridViewColumn c in dataGridView1.Columns)
                    c.Width = 50;
            }            
            chart1.Invalidate();
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {            
            HitTestResult hit = chart1.HitTest(e.X, e.Y);
            ser = int.Parse(hit.ChartArea.Name.ToString().Last().ToString()) - 1;
            if (hit.ChartElementType == ChartElementType.DataPoint)
            {               
                pIndex = hit.PointIndex;
                label1.Text = pIndex.ToString();
                dataGridView1.Rows[pIndex].Selected = true;
            }
            bPress = true;
        }

        private void chart1_MouseUp(object sender, MouseEventArgs e)
        {
            dataGridView1.DataSource = null;
            bPress = false;
            dataGridView1.DataSource = p.Points;
            foreach (DataGridViewColumn c in dataGridView1.Columns)
                c.Width = 50;
            pIndex = -1;
            chart1.Invalidate();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                tbDepth.Text = p.Points[dataGridView1.SelectedRows[0].Index].z.ToString();
                tbC.Text = p.Points[dataGridView1.SelectedRows[0].Index].c.ToString();
                tbT.Text = p.Points[dataGridView1.SelectedRows[0].Index].t.ToString();
                tbS.Text = p.Points[dataGridView1.SelectedRows[0].Index].p.ToString();
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int Z;
            float T, P, C;
            Z = int.Parse(tbDepth.Text);
            C = float.Parse(tbC.Text);
            P= float.Parse(tbS.Text);
            T= float.Parse(tbT.Text);
            p.Points.Add(new ProfilePoint() { z = Z, c = C, p = P, t = T });
            p.Points = p.Points.OrderBy(s => s.z).ToList();
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = p.Points;
            foreach (DataGridViewColumn c in dataGridView1.Columns)
                c.Width = 50;
            MakeChartsFromPoints();
        }

        private void MakeChartsFromPoints()
        {
            foreach (Series s in chart1.Series)
                s.Points.Clear();
            p.Points = p.Points.OrderBy(s => s.z).ToList();
            foreach(ProfilePoint pnt in p.Points)
            {
                chart1.Series[0].Points.AddXY(pnt.c, -pnt.z);
                chart1.Series[1].Points.AddXY(pnt.t, -pnt.z);
                chart1.Series[2].Points.AddXY(pnt.p, -pnt.z);
            }            
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                p.Save(sfd.FileName);
                LoadProfiles();
            }
        }
    }
}
