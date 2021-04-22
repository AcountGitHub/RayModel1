using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace RayModelApp
{
    public enum RayNumbers
    {
        [Description("180")]
        rn180,
        [Description("360")]
        rn360,
        [Description("900")]
        rn900,
        [Description("1800")]
        rn1800,
        [Description("9000")]
        rn9000,
        [Description("18000")]
        rn18000
    };

    class RayNumberConverter : EnumConverter
    {
        private Type type;

        public RayNumberConverter(Type type)
            : base(type)
        {
            this.type = type;
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destType)
        {
            FieldInfo fi = type.GetField(Enum.GetName(type, value));
            DescriptionAttribute descAttr =
              (DescriptionAttribute)Attribute.GetCustomAttribute(
                fi, typeof(DescriptionAttribute));

            if (descAttr != null)
                return descAttr.Description;
            else
                return value.ToString();
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            CultureInfo culture, object value)
        {
            foreach (FieldInfo fi in type.GetFields())
            {
                DescriptionAttribute descAttr =
                  (DescriptionAttribute)Attribute.GetCustomAttribute(
                    fi, typeof(DescriptionAttribute));

                if ((descAttr != null) && ((string)value == descAttr.Description))
                    return Enum.Parse(type, fi.Name);
            }
            return Enum.Parse(type, (string)value);
        }
    }

    public class Characteristic
    {
        [Description("Length of the water area")]
        public int Length { get; set; }
        [Description("Width of the water area")]
        private int w;
        public int Width
        {
            get { return w; }
            set { w = value; }
        }
        private int d;
        [Description("Depth of the water area")]
        [DefaultValue(500)]
        public int Depth
        {
            get { return d; }
            set
            {
                //if (value < 0 || value > 800) throw new ArgumentException("Value must be between 0 and 800");
                if (value < 0 || value > 800) MessageBox.Show("Value must be between 0 and 800", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    d = value;
            }
        }
        private double up;
        [Description("The attenuation factor of the signal when reflected from the surface")]
        [DefaultValue(0.9)]
        public double Up
        {
            get { return up; }
            set
            {
                double v100 = 100 * value;
                if (Math.Abs(v100 - Math.Truncate(v100)) > 0)
                    MessageBox.Show("Value resolution must be 0.01", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    if (value < 0 || value > 1)
                        MessageBox.Show("Value must be between 0 and 1", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    else
                        up = value;
            }
        }
        private double bottom;
        [Description("The attenuation factor of the signal when reflected from the bottom")]
        [DefaultValue(0.7)]
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
                        bottom = value;
            }
        }

        private RayModelApp.RayNumbers rn;
        [Description("Number of rays")]
        [TypeConverter(typeof(RayNumberConverter))]
        public RayModelApp.RayNumbers RayNumber
        {
            get { return rn; }
            set { rn = value; }
        }
    }
}
