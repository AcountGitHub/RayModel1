using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace RayModelApp
{

    public class Sreda
    {
        #region Water area

        [Category("Water area")]
        [Description("Length of water area")]
        public int Length { get; set; }

        [Category("Water area")]
        [Description("Width of water area")]
        public int Width { get; set; }

        private int depth;
        [Category("Water area")]
        [Description("Depth of water area")]
        public int Depth
        {
            get
            {
                return depth;
            }
            set
            {
                if (value > 40 && value < 800)
                    depth = value;
                else
                {
                    if (value < 40)
                        MessageBox.Show("Depth is less than 40 m", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    else
                        MessageBox.Show("Depth is more than 800 m", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        #endregion


        #region Coefficients

        private double up;
        [Category("Coefficients")]
        [Description("The attenuation factor of the signal when reflected from the surface")]
        public double Up
        {
            get
            {
                return up;
            }
            set
            {
                double v100 = 100 * value;

                if (Math.Abs(v100 - Math.Truncate(v100)) > 0)
                    MessageBox.Show("Value resolution must be 0.01", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    if (value < 0 || value > 1)
                    MessageBox.Show("Value must be between 0 and 1", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                {
                    up = value;
                    mc3vray.Ray.Ksrf = value;
                }
            }
        }

        private double bottom;
        [Category("Coefficients")]
        [Description("The attenuation factor of the signal when reflected from the bottom")]
        public double Bottom
        {
            get { return bottom; }
            set
            {
                double v100 = 100 * value;
                if (Math.Abs(v100 - Math.Truncate(v100)) > 0)
                    MessageBox.Show("Value resolution must be 0.01", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    if (value < 0 || value > 1)
                    MessageBox.Show("Value must be between 0 and 1", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                {
                    bottom = value;
                    mc3vray.Ray.Kbtm = value;
                }
            }
        }

        #endregion


        #region Other

        private int receiverDepth;
        [Description("Depth of receiver")]
        public int ReceiverDepth
        {
            get { return receiverDepth; }
            set
            {
                receiverDepth = value;
                mc3vray.Ray.Hgas = value;
            }
        }

        [Category("Source")]
        [Description("Source Frequency")]
        public float Frequency { get; set; }

        [Category("Source")]
        [Description("Source Phase")]
        public float Phase { get; set; }

        [Description("Profile")]
        public List<ProfilePoint> Profile { get; set; }

        [Description("Traectory")]
        public List<Point3D> Traectory { get; set; }

        public void Save(string filename)
        {
            using (TextWriter tw = File.CreateText(filename))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Format("{0},", Length));
                sb.Append(string.Format("{0},", Width));
                sb.Append(string.Format("{0},", Depth));
                sb.Append(string.Format("{0}", ReceiverDepth));
                tw.WriteLine(sb.ToString());
                tw.Close();
            }
        }

        #endregion


        public Sreda()
        {
            Traectory = new List<Point3D>();
            Profile = new List<ProfilePoint>();
        }

        public void Load(string filename)
        {
            using (TextReader tr = File.OpenText(filename))
            {
                int nNer, nSources, nProf;
                string s = tr.ReadLine();
                string[] pars = s.Split(',');
                nNer = int.Parse(pars[0]);
                nProf = ushort.Parse(pars[1]);
                nSources = ushort.Parse(pars[2]);
                Length = int.Parse(pars[3]);
                Width = int.Parse(pars[4]);
                Depth = int.Parse(pars[5]);
                ReceiverDepth = int.Parse(pars[7]);
                tr.Close();
            }
        }
    }
}
