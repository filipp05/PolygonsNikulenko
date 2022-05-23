using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace polygons
{
    


    public partial class RadiusForm : Form
    {
        public event RadiusEventHandler RadiusChanging;
        public event RadiusEventHandler RadiusChanged;

        public RadiusForm()
        {
            InitializeComponent();
            trackBar1.Value = Shape.Radius; 

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            /*if (RadiusChanged != null)
            {*/
                this.RadiusChanging(this, new RadiusEventArgs(trackBar1.Value));
            //}
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            this.RadiusChanged(this, new RadiusEventArgs(trackBar1.Value));
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            
        }
        /*
public void SetTrackBarValue(int value)
{
trackBar1.Value = value;
}*/
    }
}
