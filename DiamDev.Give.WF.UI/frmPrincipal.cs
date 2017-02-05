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
using DPFP;
using System.Net.Http;
using DiamDev.Give.Entities;
using Newtonsoft.Json;
using System.IO;

namespace DiamDev.Give.WF.UI
{
    public partial class frmPrincipal : Form, DPFP.Capture.EventHandler
    {
        #region Propiedades Privadas    
                    
            private DPFP.Capture.Capture Capturer;
            private DPFP.Processing.Enrollment Enroller;
            private DPFP.Template Template;
            private DPFP.Verification.Verification Verificator;
            private DPFP.Sample MemSample;

            private List<Personal> Empleados;

        #endregion

        #region Constructores
            public frmPrincipal()
            {
                InitializeComponent();

                this.lblFecha.Text = DateTime.Today.ToLongDateString();
                this.lblHora.Text = DateTime.Now.ToShortTimeString();
                this.lblTitulo.Text = ConfigurationManager.AppSettings["Agencia"];

                Verificator = new DPFP.Verification.Verification();
                this.CargaEmpleados();

                this.Tiempo.Enabled = true;
                this.Tiempo.Interval = 1000;
            }
        #endregion

        #region Metodos Privados

            protected void MakeReport(string message)
            {
                this.toolStripStatusLectorHuella.Text = message;               
            }         

            protected void Process(DPFP.Sample Sample)
            {
                try
                {
                    MemSample = Sample;                                      
                }
                catch (Exception ex)
                {
                    MakeReport(ex.Message);
                }
            }
       
            protected Bitmap ConvertSampleToBitmap(DPFP.Sample Sample)
            {
                DPFP.Capture.SampleConversion Convertor = new DPFP.Capture.SampleConversion();	// Create a sample convertor.
                Bitmap bitmap = null;												            // TODO: the size doesn't matter
                Convertor.ConvertToPicture(Sample, ref bitmap);									// TODO: return bitmap as a result
                return bitmap;
            }

            protected DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
            {
                DPFP.Processing.FeatureExtraction Extractor = new DPFP.Processing.FeatureExtraction();	// Create a feature extractor
                DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
                DPFP.FeatureSet features = new DPFP.FeatureSet();
                Extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);			// TODO: return features as a result?
                if (feedback == DPFP.Capture.CaptureFeedback.Good)
                    return features;
                else
                    return null;
            }
        
            private void CargaEmpleados()
            {
                try
                {
                    Empleados = new List<Personal>();
                    using (var client = new HttpClient())
                    {
                        using (var response = client.GetAsync(String.Format("{0}/api/personal", ConfigurationManager.AppSettings["Url"])).Result)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var personalJsonString = response.Content.ReadAsStringAsync().Result;
                                Empleados = JsonConvert.DeserializeObject<Personal[]>(personalJsonString).ToList();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Ocurrio un error, por favor verificar su conexión a internet.", "Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }

            private void Tiempo_Tick(object sender, EventArgs e)
            {
                this.ActualizarTiempo();
            }

            private void ActualizarTiempo()
            {
                this.lblFecha.Text = DateTime.Today.ToLongDateString();
                this.lblHora.Text = DateTime.Now.ToShortTimeString();
            }

       #endregion

       #region Eventos del Lector

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            MakeReport("Se ha capturado la huella digital");
            Process(Sample);
            MakeReport("Identificando al empleado");

            if (Empleados != null && Empleados.Count() > 0)
            {
                foreach (Personal item in Empleados)
                {
                    MemoryStream ms = new MemoryStream(item.TemplateBytes);
                    Template template = new DPFP.Template(ms);

                    try
                    {
                        DPFP.FeatureSet features = ExtractFeatures(MemSample, DPFP.Processing.DataPurpose.Verification);

                        if (features != null)
                        {
                            DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                            Verificator.Verify(features, template, ref result);

                            if (result.Verified)
                            {
                                Mensaje(item);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MakeReport(ex.Message);
                    }
                }
            }

            MakeReport("El empleado realizo su marcaje con exito");
        }

        public delegate void MensajeDelegado(Personal empleado);
        private void Mensaje(Personal empleado)
        {
            if (this.InvokeRequired)
            {
                var delegado = new MensajeDelegado(Mensaje);
                this.Invoke(delegado, empleado);
                return;
            }

            PersonalHorario p = new PersonalHorario();
            p.PersonalId = empleado.PersonalId;
            p.Fecha = DateTime.Today;

            using (var client = new HttpClient())
            {
                var serializedProduct = JsonConvert.SerializeObject(p);
                var content = new StringContent(serializedProduct, Encoding.UTF8, "application/json");
                var result = client.PostAsync(string.Format("{0}/api/horario", ConfigurationManager.AppSettings["Url"]), content).Result;

                if (result.IsSuccessStatusCode)
                {
                    MessageBox.Show(string.Format("Gracias por realizar su marcaje: {0}!", empleado.Nombre), "Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(string.Format("Ocurrio un error en el marcaje de: {0} intente de nuevo", empleado.Nombre), "Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            MakeReport("Ha sacado el dedo del lector de huella digital.");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella ha sido tocado.");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella esta conectado.");
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella ha sido desconectado.");
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                MakeReport("La calidad de la muestra de huella digital ha sido buena.");
            else
                MakeReport("La calidad de la muestra de huella digital no ha sido del todo buena.");
        }

        #endregion

        private void btnOpciones_Click(object sender, EventArgs e)
        {
            Capturer.StopCapture();

            frmMenu Menu = new frmMenu();
            Menu.ShowDialog();

            this.CargaEmpleados();

            Capturer.StartCapture();
        }

        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();				// Create a capture operation.

                if (null != Capturer)
                {
                    Capturer.EventHandler = this;
                    Enroller = new DPFP.Processing.Enrollment();
                    if (null != Capturer)
                    {
                        try
                        {
                            Capturer.StartCapture();                            
                        }
                        catch
                        {
                            MakeReport("No se pudo iniciar la captura!");
                        }
                    }
                }
                else
                {
                    MakeReport("No se pudo iniciar la operación de captura!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }       
    }   
}
