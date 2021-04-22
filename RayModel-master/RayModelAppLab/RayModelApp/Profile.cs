using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace RayModelApp
{
    public struct ProfilePoint
    {
        [Description("Depth")]
        public int z { get; set; }
        [Description("Sound velocity")]
        public float c { get; set; }
        [Description("Temperature")]
        public float t { get; set; }
        [Description("Salinity")]
        public float p { get; set; }
    }
    public class Profile
    {
        public int x { get; set; }
        public int y { get; set; }

        public List<ProfilePoint> Points { get; set; }

        public Profile()
        {
            Points = new List<ProfilePoint>();
        }
        public Profile(string line)
        {
            Points = new List<ProfilePoint>();
            string[] ars = line.Split(',');
            x = int.Parse(ars[0]);
            y = int.Parse(ars[1]);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0},{1}", x, y));
            for(int i = 0; i < Points.Count; i++)
            {
                sb.Append(string.Format(",{0},{1},{2},{3}", Points[i].z, Points[i].c, Points[i].t, Points[i].p));
            }
            return sb.ToString();
        }

        public void Save(string fileName)
        {
            using (TextWriter tw = File.CreateText(fileName))
            {
                foreach (ProfilePoint p in Points)
                    tw.WriteLine(string.Format("{0};{1};{2};{3}",p.z,p.c,p.t,p.p));
                tw.Close();
            }

        }
    }
}
