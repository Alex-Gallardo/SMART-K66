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
            delegate void Function();
            private DPFP.Capture.Capture Capturer;
            private DPFP.Processing.Enrollment Enroller;
            private DPFP.Template Template;
            private DPFP.Verification.Verification Verificator;
            private DPFP.Sample MemSample;
        #endregion

        #region Constructores
            public frmPrincipal()
            {
                InitializeComponent();

                this.lblFecha.Text = DateTime.Today.ToLongDateString();
                this.lblHora.Text = DateTime.Now.ToShortTimeString();
                this.lblTitulo.Text = ConfigurationManager.AppSettings["Agencia"];

                Verificator = new DPFP.Verification.Verification();
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

       #endregion

       #region Eventos del Lector

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {  
            Process(Sample);
                       
            using (var client = new HttpClient())
            {
                using (var response = client.GetAsync(String.Format("{0}/api/personal",ConfigurationManager.AppSettings["Url"])).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var personalJsonString = response.Content.ReadAsStringAsync().Result;
                        List<Personal> Personals = JsonConvert.DeserializeObject<Personal[]>(personalJsonString).ToList();
                        if (Personals != null && Personals.Count() > 0)
                        {
                            foreach (Personal item in Personals)
                            {
                                MemoryStream ms = new MemoryStream(item.TemplateBytes);
                                Template template = new DPFP.Template(ms);

                                try
                                {
                                    // Process the sample and create a feature set for the enrollment purpose.                  
                                    DPFP.FeatureSet features = ExtractFeatures(MemSample, DPFP.Processing.DataPurpose.Verification);

                                    // Check quality of the sample and start verification if it's good
                                    // TODO: move to a separate task
                                    if (features != null)
                                    {
                                        // Compare the feature set with our template
                                        DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                                        Verificator.Verify(features, template, ref result);
                                        //UpdateStatus(result.FARAchieved);
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
                    }
                }
            }
        }

        private void Mensaje(Personal empleado) 
        {
            this.Invoke(new Function(delegate()
            {
                PersonalHorario p = new PersonalHorario();
                p.PersonalId = empleado.PersonalId;
                p.Fecha = DateTime.Today;
                p.Entrada = new TimeSpan(DateTime.Today.Hour,DateTime.Today.Minute, DateTime.Today.Second);
                p.Salida = new TimeSpan(DateTime.Today.Hour, DateTime.Today.Minute, DateTime.Today.Second);

                using (var client = new HttpClient())
                {
                    var serializedProduct = JsonConvert.SerializeObject(p);
                    var content = new StringContent(serializedProduct, Encoding.UTF8, "application/json");
                    var result = client.PostAsync(string.Format("{0}/api/horario", ConfigurationManager.AppSettings["Url"]), content).Result;                 
                }   

                MessageBox.Show(string.Format("Gracias por realizar su marcaje: {0}!", empleado.Nombre), "Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));            
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
