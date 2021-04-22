using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Media.Media3D;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using mc3vray;

namespace RayModelApp
{
    public partial class RayForm : Form
    {
        #region Global Var

        Sreda Sr = new Sreda();
        BindingList<Point4D> Points = new BindingList<Point4D>();
        List<Point3D> GraphPoints = new List<Point3D>();
        static int pointLength;
        static ConcurrentQueue<Point4D> queue = new ConcurrentQueue<Point4D>();
        static List<Pnt> pnts = new List<Pnt>();
        static object locker = new object();
        Assembly asm;
        Type type;
        static MethodInfo method;
        static object obj;
        static double Omega;
        static int ErrPoint;

        double[] hobj = null;
        double[] lobj = null;
        double[] time = null;

        static double stFreq;
        static double stGAS;

        bool loaded = false;
        bool lbDown = false;

        float x = 0;
        float y = 0;
        float z = -1f;

        double kx = 1;
        double ky = 1;

        double glX = 0;
        double glY = 0;
        double downX = 0;
        double downY = 0;

        int nxGrid = 3;
        int nyGrid = 4;

        string sProfileDir = "";

        #endregion

        //

        #region Load

        public RayForm()
        {
            InitializeComponent();

            #region LoadRayModel

            asm = Assembly.LoadFrom("mc3vray.dll");
            type = asm.GetType("mc3vray.GObject", true, true);
            obj = Activator.CreateInstance(type);
            method = type.GetMethod("calcAmp");

            #endregion


            ofd.InitialDirectory = Environment.CurrentDirectory;
            ofd.Filter = "Environment file|*.env|Trajectory file|*.tr";

            sfd.InitialDirectory = ofd.InitialDirectory;
            sfd.Filter = ofd.Filter;


            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
        }

        private void RayForm_Load(object sender, EventArgs e)
        {
            #region Sr 

            Sr.Length = 100000;
            Sr.Width = 100000;
            Sr.Depth = 50;

            Sr.Up = 0.9; Ray.Ksrf = 0.9;
            Sr.Bottom = 0.7; Ray.Kbtm = 0.7;

            Sr.Frequency = 37.5f;
            Sr.Phase = 0.0f;
            Sr.ReceiverDepth = 45; Ray.Hgas = 45;

            Sr.Traectory.Add(new Point3D() { X = 1, Y = 1, Z = 5 });
            Sr.Traectory.Add(new Point3D() { X = 2000, Y = 2000, Z = 5 });

            Sr.Profile.Add(new ProfilePoint() { z = 00, c = 1540, p = 23.1f, t = 21.0f });
            Sr.Profile.Add(new ProfilePoint() { z = 10, c = 1542, p = 25.1f, t = 19.0f });
            Sr.Profile.Add(new ProfilePoint() { z = 30, c = 1545, p = 27.3f, t = 15.0f });
            Sr.Profile.Add(new ProfilePoint() { z = 50, c = 1548, p = 29.3f, t = 10.0f });

            Points.Add(new Point4D() { X = 1, Y = 1, Z = 5, W = 1 });
            Points.Add(new Point4D() { X = 2000, Y = 2000, Z = 5, W = 0 });

            pg1.SelectedObject = Sr;
            pg1.PropertySort = PropertySort.Categorized;


            Ray.Hz.Clear();
            Ray.Hz.AddRange(Sr.Profile.Select(p => (double)p.z).ToArray());

            Ray.Cz.Clear();
            Ray.Cz.AddRange(Sr.Profile.Select(p => (double)p.c).ToArray());

            #endregion

            #region dgw

            GLC.MouseWheel += OnMouseWheel;

            dgw.AutoGenerateColumns = false;
            dgw.Columns.Add("X", "X");
            dgw.Columns.Add("Y", "Y");
            dgw.Columns.Add("Z", "Z");
            dgw.Columns.Add("V", "W");
            dgw.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgw.AllowUserToAddRows = true;

            foreach (DataGridViewTextBoxColumn c in dgw.Columns)
                c.Width = 60;

            dgw.Columns["X"].DataPropertyName = "X";
            dgw.Columns["Y"].DataPropertyName = "Y";
            dgw.Columns["Z"].DataPropertyName = "Z";
            dgw.Columns["V"].DataPropertyName = "W";

            dgw.DataSource = Points;

            #endregion

            bArtRun.Visible = false;
        }

        #endregion

        //

        #region Set/Load Parametr

        private void button1_Click(object sender, EventArgs e)
        {
            stCoord.Text = string.Format("Capacity {0}", GraphPoints.Count);

            bArtRun.Visible = false;
        }

        private void cmiTraectories_Click(object sender, EventArgs e)
        {
            using (FrmTraectories f = new FrmTraectories())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    dgw.DataSource = null;
                    Points.Clear();
                    Sr.Traectory.Clear();
                    foreach (var p in f.points)
                    {
                        Sr.Traectory.Add(new Point3D() { X = p.X, Y = p.Y, Z = p.Z });
                        Points.Add(new Point4D() { X = p.X, Y = p.Y, Z = p.Z, W = 0 });
                    }
                    dgw.DataSource = Points;

                    bArtRun.Visible = false;
                }
            }
        }

        private void cmiProfiles_Click(object sender, EventArgs e)
        {
            using (FrmProfiles f = new FrmProfiles())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    Sr.Profile.Clear();
                    foreach (var p in f.p.Points)
                        Sr.Profile.Add(p);

                    bArtRun.Visible = false;
                }
            }
        }


        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                switch (sfd.FilterIndex)
                {
                    case 0:
                        using (TextWriter tw = File.CreateText(sfd.FileName))
                        {
                            foreach (Point4D p in Points)
                                tw.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", p.X, p.Y, p.Z, p.W));
                        }
                        break;
                    case 1:
                        Sr.Save(sfd.FileName);
                        break;
                }
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                switch (ofd.FilterIndex)
                {
                    case 0:
                        Points.Clear();
                        using (TextReader tr = File.OpenText(ofd.FileName))
                        {
                            string line;
                            double px, py, pz, pw;
                            while ((line = tr.ReadLine()) != null)
                            {
                                Console.WriteLine(line);
                                string[] vals = line.Split('\t');
                                double.TryParse(vals[0], out px);
                                double.TryParse(vals[1], out py);
                                double.TryParse(vals[2], out pz);
                                double.TryParse(vals[3], out pw);
                                Points.Add(new Point4D() { X = px, Y = py, Z = pz, W = pw });
                            }
                        }
                        bArtRun.Visible = false;
                        break;
                    case 1:
                        Sr.Load(ofd.FileName);
                        pg1.SelectedObject = Sr;
                        bArtRun.Visible = false;
                        break;
                }
                GLC.Invalidate();
            }
        }

        #endregion

        //

        #region GLC

        private void SetupViewport()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            int w = GLC.Width;
            int h = GLC.Height;
            float orthoW = w * (z + 1);
            float orthoH = h * (z + 1);
            GL.Ortho(0, orthoW, 0, orthoH, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
        }

        private void glC_Load(object sender, EventArgs e)
        {
            loaded = true;
            GL.ClearColor(Color.Black);
            SetupViewport();
        }

        private void glC_Resize(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            SetupViewport();
        }


        private void GLC_MouseDown(object sender, MouseEventArgs e)
        {
            lbDown = true;
            downX = e.X;
            downY = e.Y;
        }

        private void GLC_MouseUp(object sender, MouseEventArgs e)
        {
            lbDown = false;
            //glX += 0.1;
            GLC.Invalidate();
        }

        private void glC_MouseMove(object sender, MouseEventArgs e)
        {
            double x1 = e.X;
            double y1 = e.Y;
            double ex1 = (x1 - GLC.Width / 2.0) / (GLC.Width / 2.0);

            stCoord.Text = string.Format("X={0} Y={1} ex={2}", x1, y1, ex1);

            if (e.Button == MouseButtons.Right)
            {
                x = PointToClient(Cursor.Position).X * (z + 1);
                y = (-PointToClient(Cursor.Position).Y + GLC.Height) * (z + 1);
            }
            GLC.Invalidate();
        }

        private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                kx *= 1.1f;
                ky = kx;
            }
            if (e.Delta < 0)
            {
                kx /= 1.1f;
                ky = kx;
            }
            GLC.Invalidate();
        }


        private void glC_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) // Play nice
                return;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            DrawGrid();
            DrawTraectory(Sr.Traectory);
            DrawGraphPoints(GraphPoints);
            #region Pautinka
            GL.Color3(Color.Red);
            double r = 1;
            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < 12; i++)
            {
                GL.Vertex2(0, 0);
                GL.Vertex2(kx * r * Math.Cos(i * Math.PI / 6) / 2.0, ky * r * Math.Sin(i * Math.PI / 6) / 2.0);
            }
            GL.End();
            #endregion
            GLC.SwapBuffers();
        }

        #endregion

        //

        #region Draw

        private void DrawGrid()
        {
            double D = 0.5;
            double m = 10000;
            GL.Color3(Color.SkyBlue);
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(kx * D, ky * D);
            GL.Vertex2(kx * D, -ky * D);
            GL.Vertex2(-kx * D, -ky * D);
            GL.Vertex2(-kx * D, ky * D);
            GL.End();
            GL.Color3(Color.Gray);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(glX, glY);
            GL.Vertex2(glX, glY + 0.5);
            GL.Vertex2(glX, glY);
            GL.Vertex2(glX + 0.5, glY);
            for (int i = 0; i <= nyGrid; i++)
            {
                GL.Vertex2(glX - m * kx * nxGrid, glY + m * ky * i);
                GL.Vertex2(glX + m * kx * nxGrid, glY + m * ky * i);
                GL.Vertex2(glX - m * kx * nxGrid, glY - m * ky * i);
                GL.Vertex2(glX + m * kx * nxGrid, glY - m * ky * i);
            }
            for (int i = 0; i <= nxGrid; i++)
            {
                GL.Vertex2(glX + m * kx * i, glY - m * ky * nyGrid);
                GL.Vertex2(glX + m * kx * i, glY + m * ky * nyGrid);
                GL.Vertex2(glX - m * kx * i, glY - m * ky * nyGrid);
                GL.Vertex2(glX - m * kx * i, glY + m * ky * nyGrid);

            }
            GL.End();
        }


        private void DrawGraphPoints(List<Point3D> ps)
        {
            if (ps.Count > 0)
            {
                GL.Color3(Color.Yellow);
                GL.PointSize(5.0f);
                GL.Begin(PrimitiveType.Points);
                foreach (Point3D p in ps)
                {
                    GL.Vertex3(kx * p.X, ky * p.Y, 0);
                }
                GL.End();
            }
        }

        private void DrawTraectory(List<Point3D> tr)
        {
            if (tr.Count > 1)
            {
                GL.Color3(Color.Magenta);
                GL.Begin(PrimitiveType.Lines);
                for (int i = 0; i < tr.Count - 1; i++)
                {
                    GL.Vertex2(kx * tr[i].X, ky * tr[i].Y);
                    GL.Vertex2(kx * tr[i + 1].X, ky * tr[i + 1].Y);
                }
                GL.End();
            }
        }


        private void dgw_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            Point4D p = new Point4D();
            var oldValue = dgw[e.ColumnIndex, e.RowIndex].Value;
            var newValue = e.FormattedValue;
            Console.WriteLine(string.Format("{0} {1} {2}", "CellValidating", oldValue, newValue));
            switch (e.ColumnIndex)
            {
                case 0:
                    p = Points[e.RowIndex];
                    p.X = double.Parse(newValue.ToString());
                    Points[e.RowIndex] = p;
                    break;
                case 1:
                    p = Points[e.RowIndex];
                    p.Y = double.Parse(newValue.ToString());
                    Points[e.RowIndex] = p;
                    break;
                case 2:
                    p = Points[e.RowIndex];
                    p.Z = double.Parse(newValue.ToString());
                    Points[e.RowIndex] = p;
                    break;
                case 3:
                    p = Points[e.RowIndex];
                    p.W = double.Parse(newValue.ToString());
                    Points[e.RowIndex] = p;
                    break;
            }
            GLC.Refresh();
            dgw.Refresh();
        }

        #endregion

        //

        private void btnMakePoints_Click_1(object sender, EventArgs e)
        {
            Point4D p4d;
            List<double> times = new List<double>();
            double currentTime = 0;
            double dt, path, truepath, lastpath, lasttime, firstpath;
            double Lx, Ly, Lz;
            double px, py, pz;
            double dx, dy, dz;
            int nP;
            double.TryParse(cbTR.Text, out dt);
            GraphPoints.Clear();
            while (!queue.IsEmpty)
                queue.TryDequeue(out p4d);
            Console.WriteLine(string.Format("queue.Count {0}", queue.Count));
            lasttime = 0;
            firstpath = 0;
            px = Points[0].X;
            py = Points[0].Y;
            pz = Points[0].Z;
            GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
            times.Add(currentTime);
            Console.WriteLine(string.Format("Точка[{0}] {1} {2}", GraphPoints.Count, px, py));
            for (int i = 0; i < Points.Count - 1; i++)
            {
                Console.WriteLine();
                path = getDist(Points[i], Points[i + 1]);
                Lx = Points[i + 1].X - Points[i].X;
                Ly = Points[i + 1].Y - Points[i].Y;
                Lz = Points[i + 1].Z - Points[i].Z;

                dx = Points[i].W * Lx / Math.Sqrt(Lx * Lx + Ly * Ly + Lz * Lz);
                dy = Points[i].W * Ly / Math.Sqrt(Lx * Lx + Ly * Ly + Lz * Lz);
                dz = Points[i].W * Lz / Math.Sqrt(Lx * Lx + Ly * Ly + Lz * Lz);
                Console.WriteLine(string.Format("dx {0} dy {1}", dx, dy));

                if (lasttime > 0)
                {
                    Console.WriteLine(string.Format("Начальное время {0}", dt - lasttime));
                    GraphPoints.RemoveAt(GraphPoints.Count - 1);
                    times.RemoveAt(times.Count - 1);
                    px += (dt - lasttime) * dx;
                    py += (dt - lasttime) * dy;
                    pz += (dt - lasttime) * dz;
                    currentTime += dt - lasttime;
                    GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                    times.Add(currentTime);
                    Console.WriteLine(string.Format("Точка[{0}] {1} {2}", GraphPoints.Count, px, py));
                    firstpath = Math.Sqrt(Math.Pow((dt - lasttime) * dx, 2) + Math.Pow((dt - lasttime) * dy, 2) + Math.Pow((dt - lasttime) * dz, 2));
                    path -= firstpath;
                }
                nP = (int)Math.Floor(path / (Points[i].W * dt));
                truepath = nP * Points[i].W * dt;
                Console.WriteLine(string.Format("Отрезок {0} Длина {1} ЦелаяДлина {2} Точок={3}", i, path, truepath, nP));
                for (int j = 0; j < nP; j++)
                {
                    px += dt * dx;
                    py += dt * dy;
                    pz += dt * dz;
                    currentTime += dt;
                    GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                    times.Add(currentTime);
                    Console.WriteLine(string.Format("Точка[{0}] {1} {2}", GraphPoints.Count, px, py));
                }
                lastpath = path - truepath;
                lasttime = lastpath / Points[i].W;
                Console.WriteLine(string.Format("ПоследняяДлина {0} ПоследнееВремя={1}", lastpath, lasttime));
                if (lasttime > 0)
                {
                    px += lasttime * dx;
                    py += lasttime * dy;
                    pz += lasttime * dz;
                    currentTime += lasttime;
                    GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                    times.Add(currentTime);
                    Console.WriteLine(string.Format("Точка[{0}] {1} {2}", GraphPoints.Count, px, py));
                }
            }
            GLC.Invalidate();
            for (int i = 0; i < GraphPoints.Count; i++)
                queue.Enqueue(new Point4D() { X = GraphPoints[i].X, Y = GraphPoints[i].Y, Z = GraphPoints[i].Z, W = times[i] });
            times = null;
            Console.WriteLine(string.Format("GraphPoints.Count = {0} Queue ={1}", GraphPoints.Count, queue.Count));
            pointLength = GraphPoints.Count;
            //TestPoint();

            Ray.Hz.Clear();
            Ray.Hz.AddRange(Sr.Profile.Select(p => (double)p.z).ToArray());

            Ray.Cz.Clear();
            Ray.Cz.AddRange(Sr.Profile.Select(p => (double)p.c).ToArray());

            bArtRun.Visible = true;
        }

        private void bArtem_Click(object sender, EventArgs e)
        {
            #region prepare
            chart1.Series[0].Points.Clear();
            double dT;
            double.TryParse(cbTR.Text, out dT);

            Console.WriteLine(GraphPoints.Count);
            hobj = new double[GraphPoints.Count];
            lobj = new double[GraphPoints.Count];
            time = new double[GraphPoints.Count];

            for (int i = 0; i < GraphPoints.Count; i++)
            {
                hobj[i] = GraphPoints[i].Z;
                lobj[i] = Math.Sqrt(Math.Pow(GraphPoints[i].X, 2.0) + Math.Pow(GraphPoints[i].Y, 2.0));
                time[i] = i * dT;
            }
            Array.Resize(ref Ray.Kz, Ray.Hz.Count - 1);
            Array.Clear(Ray.Kz, 0, Ray.Kz.Length);
            for (int i = 0; i < Ray.Kz.Length; i++)
            {
                if (Ray.Cz[i] == Ray.Cz[i + 1])
                    Ray.Cz[i + 1] += 0.001;                                             // якщо швидкість стала, робимо коефіцієнт наближеним до нуля
                Ray.Kz[i] = (Ray.Cz[i + 1] - Ray.Cz[i]) / (Ray.Hz[i + 1] - Ray.Hz[i]);  // обчислюємо коефіцієнти зміни швидкості звуку за глибиною
            }
            Array.Resize(ref Ray.Yr, Ray.Kz.Length);
            Array.Clear(Ray.Yr, 0, Ray.Yr.Length);
            for (int i = 0; i < Ray.Yr.Length; i++)                                     // обчислюємо ординати центрів кіл для кожного водного шару
                Ray.Yr[i] = Ray.Cz[i] / Ray.Kz[i] - Ray.Hz[i];


            GObject gObj = new GObject();

            List<double> timeRays = new List<double>();
            List<double> ampRaysX = new List<double>();
            List<double> ampRaysY = new List<double>();
            List<double> ampRaysZ = new List<double>();

            #endregion

            //for (int i = 0; i < GraphPoints.Count; i++)
            Parallel.For(0, GraphPoints.Count, (i) =>
             {
                 List<double> tR = new List<double>();
                 List<double> aR = new List<double>();
                 List<double> angR = new List<double>();
                 List<double> lR = new List<double>();

                 gObj.calcAmp(hobj[i], lobj[i], time[i], aR, tR, angR, lR);

                 for (int j = 0; j < tR.Count; j++)
                 {
                     timeRays.Add(Math.Round(tR[j] / dT) * dT + time[i]);
                    //double rxy = aR[j] * Math.Cos(angR[j]);
                    double rxy = aR[j] * Math.Cos(angR[j]) * Math.Sin(2 * Math.PI * Sr.Frequency * tR[j]);
                     ampRaysX.Add(rxy * GraphPoints[i].X / lobj[i]);
                     ampRaysY.Add(rxy * GraphPoints[i].Y / lobj[i]);
                    //ampRaysZ.Add(aR[j] * Math.Sin(angR[j]));
                    ampRaysZ.Add(aR[j] * Math.Sin(angR[j]) * Math.Sin(2 * Math.PI * Sr.Frequency * tR[j]));
                 }
             });

            #region Graph
            int N = Math.Min(Math.Min(timeRays.Count, ampRaysX.Count), Math.Min(ampRaysY.Count, ampRaysZ.Count));
            for (int d = N / 2; d >= 1; d /= 2)
                for (int i = d; i < N; i++)
                    for (int j = i; j >= d && timeRays[j - d] > timeRays[j]; j -= d)
                    {
                        double rT = timeRays[j];
                        timeRays[j] = timeRays[j - d];
                        timeRays[j - d] = rT;
                        rT = ampRaysX[j];
                        ampRaysX[j] = ampRaysX[j - d];
                        ampRaysX[j - d] = rT;
                        rT = ampRaysY[j];
                        ampRaysY[j] = ampRaysY[j - d];
                        ampRaysY[j - d] = rT;
                        rT = ampRaysZ[j];
                        ampRaysZ[j] = ampRaysZ[j - d];
                        ampRaysZ[j - d] = rT;
                    }
            List<double> resTime = new List<double>();
            List<double> resAmp = new List<double>();
            for (int i = 0; i < N; i++)
            {
                Console.WriteLine(string.Format("timeRays = {0}", i));
                if (i > 0 && timeRays[i - 1] == timeRays[i])
                    continue;
                double ampx = 0;
                double ampy = 0;
                double ampz = 0;
                for (int k = i; k < N; k++)
                {
                    if (timeRays[k] == timeRays[i])
                    {
                        ampx += ampRaysX[k];
                        ampy += ampRaysY[k];
                        ampz += ampRaysZ[k];
                    }
                }
                double amp0 = Math.Sqrt(ampx * ampx + ampy * ampy + ampz * ampz);
                resTime.Add(timeRays[i]);
                resAmp.Add(amp0);
            }
            for (int i = 0; i < resTime.Count; i++)
                chart1.Series[0].Points.AddXY(resTime[i], resAmp[i]);

            #endregion

        }

        //

        #region отхер

        private void btnMakePoints_Click(object sender, EventArgs e)
        {
            Point4D p4d;
            double dt, dx, dy, dz, px, py, pz;
            double path, len, currentTime;
            double.TryParse(cbTR.Text, out dt);
            GraphPoints.Clear();
            while (!queue.IsEmpty)
                queue.TryDequeue(out p4d);
            chart1.Series["sDist"].Points.Clear();
            currentTime = 0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                path = getDist(Points[i], Points[i + 1]);
                px = Points[i + 1].X - Points[i].X;
                py = Points[i + 1].Y - Points[i].Y;
                pz = Points[i + 1].Z - Points[i].Z;

                dx = Points[i].W * px / Math.Sqrt(px * px + py * py + pz * pz);
                dy = Points[i].W * py / Math.Sqrt(px * px + py * py + pz * pz);
                dz = Points[i].W * pz / Math.Sqrt(px * px + py * py + pz * pz);

                px = Points[i].X;
                py = Points[i].Y;
                pz = Points[i].Z;
                GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                queue.Enqueue(new Point4D() { X = px, Y = py, Z = pz, W = currentTime });
                chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                len = 0;
                currentTime += dt;
                do
                {
                    px += dt * dx;
                    py += dt * dy;
                    pz += dt * dz;
                    Console.WriteLine(string.Format("{0} {1} {2}", px, py, pz));
                    GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                    queue.Enqueue(new Point4D() { X = px, Y = py, Z = pz, W = currentTime });
                    chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                    len += dt;
                    currentTime += dt;
                } while (len < path / Points[i].W);
            }
            Console.WriteLine(GraphPoints.Count);
        }

        private double getDist(Point4D a, Point4D b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
        }

        #region notused

        private void btnParallel_Click(object sender, EventArgs e)
        {
            ConcurrentQueue<Point3D> query = new ConcurrentQueue<Point3D>();
            double dt, dx, dy, dz, px, py, pz;
            double path, len;
            double.TryParse(cbTR.Text, out dt);
            chart1.Series["sDist"].Points.Clear();
            pnts.Clear();
            for (int i = 0; i < Points.Count - 1; i++)
            {
                path = getDist(Points[i], Points[i + 1]);
                px = Points[i + 1].X - Points[i].X;
                py = Points[i + 1].Y - Points[i].Y;
                pz = Points[i + 1].Z - Points[i].Z;

                dx = Points[i].W * px / Math.Sqrt(px * px + py * py + pz * pz);
                dy = Points[i].W * py / Math.Sqrt(px * px + py * py + pz * pz);
                dz = Points[i].W * pz / Math.Sqrt(px * px + py * py + pz * pz);

                px = Points[i].X;
                py = Points[i].Y;
                pz = Points[i].Z;
                query.Enqueue(new Point3D() { X = px, Y = py, Z = pz });
                //chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                //chart1.Series["sDepth"].Points.Add(pz);
                len = 0;
                do
                {
                    px += dt * dx;
                    py += dt * dy;
                    pz += dt * dz;
                    Console.WriteLine(string.Format("{0} {1} {2}", px, py, pz));
                    query.Enqueue(new Point3D() { X = px, Y = py, Z = pz });
                    //chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                    //chart1.Series["sDepth"].Points.Add(pz);
                    len += dt;
                } while (len < path / Points[i].W);
            }
            //
            Console.WriteLine(string.Format("Queue :: Number points for calculation {0}", query.Count));
            Task[] tasks = new Task[16];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ErrPoint = 0;
            for (int i = 0; i < 16; i++)
            {
                tasks[i] = new Task(Calc);
                tasks[i].Start();
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Text = string.Format("Parallel {0} sec Count={1}", sw.ElapsedMilliseconds / 1000.0, pnts.Count);
            MessageBox.Show(string.Format("Points {0} by {1} sec.", GraphPoints.Count, sw.ElapsedMilliseconds / 1000.0));
            MessageBox.Show(string.Format("Error Points {0}", ErrPoint));
            chart1.Series["sDist"].Points.Clear();
            var ps = pnts.OrderBy(p => p.x);
            foreach (Pnt p in ps)
                chart1.Series["sDist"].Points.AddXY(p.x, p.y);
        }

        static void Calc()
        {
            double hobj;
            double lobj;
            Point4D p4d;
            GObject go = new GObject();
            while (!queue.IsEmpty)
            {
                queue.TryDequeue(out p4d);
                hobj = 5;
                lobj = Math.Sqrt(Math.Pow(p4d.X, 2.0) + Math.Pow(p4d.Y, 2.0));

                if (method != null)
                {
                    //object result = method.Invoke(obj, new object[] { hgas, hobj, lobj, kP, kD, omega, c, h });
                    //object result = go.calcAmp(hgas, hobj, lobj, kP, kD, omega, c, h);
                    double[] tR;
                    double[] aR;
                    double[] lR;
                    double[] angR;
                    // HACH: to change - go.calcAmp(hobj, lobj, out tR, out aR, out angR, out lR);
                    lock (locker)
                    {
                        //pnts.Add(new Pnt() { x = p4d.W, y = (double)result });
                        try
                        {
                            // HACH: to change - Console.WriteLine(string.Format("amp: {0}", aR[0]));
                            // HACH: to change - pnts.Add(new Pnt() { x = p4d.W, y = aR[0] });
                            //Application.OpenForms[0].Text = queue.Count.ToString();
                        }
                        catch (Exception ex)
                        {
                            ErrPoint++;
                        }
                    }
                }
            }
        }


        private void bRun_Click(object sender, EventArgs e)
        {
            int i;
            bool isVelocityCheck = true;
            Console.WriteLine(Points.Count);
            if (Points.Count > 1)
            {
                for (i = 0; i < Points.Count - 1; i++)
                {
                    ///Console.WriteLine(Points[i].W);
                    if (Points[i].W == 0)
                    {
                        isVelocityCheck = false;
                    }
                }
                if (isVelocityCheck == true) // Швидкість на відрізках існує, можна робити розрахунок
                {
                    pnts.Clear();
                    Task[] tasks = new Task[4];
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    ErrPoint = 0;
                    Omega = Sr.Frequency;
                    for (int j = 0; j < 4; j++)
                    {
                        tasks[j] = new Task(Calculate);
                        tasks[j].Start();
                    }
                    Task.WaitAll(tasks);
                    sw.Stop();
                    Text = string.Format("Parallel {0} sec Count={1}", sw.ElapsedMilliseconds / 1000.0, pnts.Count);
                    MessageBox.Show(string.Format("Points {0} by {1} sec.", GraphPoints.Count, sw.ElapsedMilliseconds / 1000.0));
                    MessageBox.Show(string.Format("Error Points {0}", ErrPoint));
                    chart1.Series["sDist"].Points.Clear();
                    var ps = pnts.OrderBy(p => p.x);
                    foreach (Pnt p in ps)
                        chart1.Series["sDist"].Points.AddXY(p.x, p.y);
                }
                else
                {
                    MessageBox.Show("Velosity not check", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Traectoty not found", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        static void Calculate()
        {
            double p;
            while (!queue.IsEmpty)
            {
                queue.TryDequeue(out Point4D p4d);
                lock (locker)
                {
                    //pnts.Add(new Pnt() { x = p4d.W, y = (double)result });
                    try
                    {
                        p = Math.Cos(Omega * Math.PI * p4d.Z) * Math.Exp(-0.01 * Math.Sqrt(p4d.X * p4d.X + p4d.Y * p4d.Y));
                        pnts.Add(new Pnt() { x = p4d.W, y = p });
                        Application.OpenForms[0].Text = queue.Count.ToString();
                    }
                    catch (Exception ex)
                    {
                        ErrPoint++;
                    }
                }
            }
        }


        private void btnSequential_Click(object sender, EventArgs e)
        {
            double dt, dx, dy, dz, px, py, pz;
            double path, len;
            double.TryParse(cbTR.Text, out dt);
            GraphPoints.Clear();
            chart1.Series["sDist"].Points.Clear();
            for (int i = 0; i < Points.Count - 1; i++)
            {
                path = getDist(Points[i], Points[i + 1]);
                px = Points[i + 1].X - Points[i].X;
                py = Points[i + 1].Y - Points[i].Y;
                pz = Points[i + 1].Z - Points[i].Z;

                dx = Points[i].W * px / Math.Sqrt(px * px + py * py + pz * pz);
                dy = Points[i].W * py / Math.Sqrt(px * px + py * py + pz * pz);
                dz = Points[i].W * pz / Math.Sqrt(px * px + py * py + pz * pz);

                px = Points[i].X;
                py = Points[i].Y;
                pz = Points[i].Z;
                GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                //chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                //chart1.Series["sDepth"].Points.Add(pz);
                len = 0;
                do
                {
                    px += dt * dx;
                    py += dt * dy;
                    pz += dt * dz;
                    Console.WriteLine(string.Format("{0} {1} {2}", px, py, pz));
                    GraphPoints.Add(new Point3D() { X = px, Y = py, Z = pz });
                    //chart1.Series["sDist"].Points.Add(Math.Sqrt(px * px + py * py));
                    //chart1.Series["sDepth"].Points.Add(pz);
                    len += dt;
                } while (len < path / Points[i].W);
            }
            //

            double[] c = { 1545, 1540, 1550, 1565 };
            double[] h = { 0, 40, 70, 100 };

            // double[] c = Sr.Profiles[0].Points.Select(p => (double)p.c).ToArray();
            // double[] h = Sr.Profiles[0].Points.Select(p => (double)p.z).ToArray();
            double hgas = Sr.ReceiverDepth;
            double hobj = 55;
            double lobj = 1000;
            double kP = 0.9;
            double kD = 0.7;
            double omega = 37;
            //
            Console.WriteLine(string.Format("Number points for calculation {0}", GraphPoints.Count));
            long ellapledTicks;
            Stopwatch sWatch = new Stopwatch();
            sWatch.Start();
            for (int i = 0; i < GraphPoints.Count; i++)
            {
                ellapledTicks = DateTime.Now.Ticks;
                hobj = GraphPoints[i].Z;
                lobj = Math.Sqrt(Math.Pow(GraphPoints[i].X, 2.0) + Math.Pow(GraphPoints[i].Y, 2.0));
                //object result = method.Invoke(obj, new object[] { hgas, hobj, lobj, kP, kD, omega, c, h });
                object result = method.Invoke(obj, new object[] { hgas, hobj, lobj, kP, kD, omega, c, h });
                /* Console.WriteLine(string.Format("{0} {1}", i, (result as GObject).ampry[0]));
                 chart1.Series["sDist"].Points.Add((result as GObject).ampry[0]);
                 ellapledTicks = DateTime.Now.Ticks - ellapledTicks;
                 chart1.Series["sDepth"].Points.Add(ellapledTicks);*/
            }
            sWatch.Stop();
            MessageBox.Show(string.Format("Points {0} by {1} sec.", GraphPoints.Count, sWatch.ElapsedMilliseconds / 1000.0));
        }


        private void TestPoint()
        {
            Point4D p4d;
            foreach (Point3D p in GraphPoints)
            {
                queue.TryDequeue(out p4d);
                Console.WriteLine(string.Format("{0}  {1}", p, p4d));
            }
        }

        #endregion

        #endregion
    }

    public struct Pnt
    {
        public double x;
        public double y;
    }
}
