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
    public partial class frmConfiguracion : Form
    {
        public frmConfiguracion()
        {
            InitializeComponent();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnEmpleado_Click(object sender, EventArgs e)
        {
            frmEmpleado Empleado = new frmEmpleado();
            Empleado.ShowDialog();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }      
    }
}
