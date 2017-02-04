using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiamDev.Give.WF.UI
{
    public partial class frmMenu : Form
    {
        public frmMenu()
        {
            InitializeComponent();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnConfiguracion_Click(object sender, EventArgs e)
        {
            frmAutenticar Autenticar = new frmAutenticar();
            Autenticar.ShowDialog();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
