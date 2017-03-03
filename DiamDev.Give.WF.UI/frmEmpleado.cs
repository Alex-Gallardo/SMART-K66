using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiamDev.Give.Entities;
using System.Configuration;

namespace DiamDev.Give.WF.UI
{
    public partial class frmEmpleado : Form, DPFP.Capture.EventHandler
    {

        #region Propiedades Privadas
            delegate void Function();
            private DPFP.Capture.Capture Capturer;
            private DPFP.Processing.Enrollment Enroller;
            private DPFP.Sample MemSample;
            private DPFP.Template MemTemplate;
        #endregion

        #region Constructores
            public frmEmpleado()
            {
                try
                {
                    InitializeComponent();
                    Enroller = new DPFP.Processing.Enrollment();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        #endregion      

        protected void SetStatus(string status)
        {
            this.Invoke(new Function(delegate()
            {
                this.lblInstruccionLector.Text = status;
            }));
        }

        protected void MakeReport(string message)
        {
            this.Invoke(new Function(delegate()
            {
                this.toolStripStatusLectorHuella.Text = message;
            }));
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

        protected void Process(DPFP.Sample Sample)
        {

            MemSample = Sample;
            // Draw fingerprint sample image.
            DrawPicture(ConvertSampleToBitmap(Sample));

            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Enrollment);
            // Check quality of the sample and add to enroller if it's good
            if (features != null)
            {
                try
                {
                    MakeReport("The fingerprint feature set was created.");
                    Enroller.AddFeatures(features);		// Add feature set to template.
                }
                finally
                {
                    //UpdateStatus();

                    // Check if template has been created.
                    switch (Enroller.TemplateStatus)
                    {
                        case DPFP.Processing.Enrollment.Status.Ready:	// report success and stop capturing
                            MemTemplate = Enroller.Template;                      
                            break;

                        case DPFP.Processing.Enrollment.Status.Failed:	// report failure and restart capturing

                            SetStatus(string.Format("Faltan {0} Muestras!", Enroller.FeaturesNeeded));
                            Enroller.Clear();
                            MemTemplate = null;                          
                            break;
                        case DPFP.Processing.Enrollment.Status.Insufficient:
                            SetStatus(string.Format("Faltan {0} Muestras!", Enroller.FeaturesNeeded));                         
                            break;
                    }
                }
            }
        }

        private void DrawPicture(Bitmap bitmap)
        {
            this.Invoke(new Function(delegate()
            {
                picHuella.Image = new Bitmap(bitmap, picHuella.Size);	// fit the image into the picture box
            }));
        }

        protected Bitmap ConvertSampleToBitmap(DPFP.Sample Sample)
        {
            DPFP.Capture.SampleConversion Convertor = new DPFP.Capture.SampleConversion();	// Create a sample convertor.
            Bitmap bitmap = null;												            // TODO: the size doesn't matter
            Convertor.ConvertToPicture(Sample, ref bitmap);									// TODO: return bitmap as a result
            return bitmap;
        }

        #region Eventos del Lector

            public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
            {
                SetStatus("La huella digital fue capturada!");
                MakeReport("Se ha capturado la huella digital");
                Process(Sample);
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

        private void btnSalir_Click(object sender, EventArgs e)
        {
            if (Capturer != null)
                Capturer.StopCapture();
            this.Close();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtNombre.Text))
            {
                MessageBox.Show("Se le informa que el nombre del empleado es requerido.", "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MemTemplate == null)
            {
                MessageBox.Show("La huella digital no fue ingresada.", "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.Cursor = Cursors.WaitCursor;

            Personal p = new Personal();           
            p.Nombre = this.txtNombre.Text;
            p.Direccion = "Ciudad";
            p.Huella = ImageToByteArray(this.picHuella.Image);
            p.TemplateBytes = MemTemplate.Bytes;
            p.TemplateSize = MemTemplate.Size;
            p.Activo = true;

            using (var client = new HttpClient())
            {
                var serializedProduct = JsonConvert.SerializeObject(p);
                var content = new StringContent(serializedProduct, Encoding.UTF8, "application/json");
                var result = client.PostAsync(string.Format("{0}/api/personal", ConfigurationManager.AppSettings["Url"]), content).Result;

                this.Cursor = Cursors.Arrow;

                if (Capturer != null)
                    Capturer.StopCapture();
                this.Close();
            }                      
        }

        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }

        private void frmEmpleado_Load(object sender, EventArgs e)
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();				// Create a capture operation.

                if (null != Capturer)
                {
                    Capturer.EventHandler = this;                   
                    if (null != Capturer)
                    {
                        try
                        {
                            Capturer.StartCapture();
                            MakeReport("Ponga su dedo indice en el huellero");
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
                MessageBox.Show(this, ex.Message, "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
