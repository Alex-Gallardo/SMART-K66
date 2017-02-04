using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace DiamDev.Give.WF.UI
{
    public partial class frmAutenticar : Form
    {
        public frmAutenticar()
        {
            InitializeComponent();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            string PasswordAutenticar = ConfigurationManager.AppSettings["Password_Autenticar"];
            if (PasswordAutenticar.Equals(this.txtPassword.Text))
            {
                frmConfiguracion Configuracion = new frmConfiguracion();
                Configuracion.ShowDialog();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Se le informa que el password que esta ingresando no es valido.", "Autenticar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
