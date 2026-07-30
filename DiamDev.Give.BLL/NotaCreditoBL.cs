using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class NotaCreditoBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public NotaCreditoBL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados

            private int Correlativo()
            {
                int Id = 0;

                try
                {

                    NotaCredito NotaCreditoActual = db.Set<NotaCredito>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (NotaCreditoActual != null)
                    {
                        Inicial_Id = NotaCreditoActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;

                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(NotaCredito entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngNotaCreditoId = new Herramienta().Formato_Correlativo(Id);

                        if (lngNotaCreditoId > 0)
                        {
                            entidad.CreditoId = lngNotaCreditoId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (string.IsNullOrWhiteSpace(entidad.Nota))
                            {
                                if (!entidad.Devolucion)
                                {
                                    entidad.Nota = "VALE DE REGALO";                                    
                                }                                
                            }

                            if (entidad.Pagos != null && entidad.Pagos.Count() > 0)
                            {
                                foreach (var Pago in entidad.Pagos)
                                {
                                    Pago.CreditoId = entidad.CreditoId;                                    
                                }                                
                            }

                            db.Set<NotaCredito>().Add(entidad);
                            db.SaveChanges();                            
                        }
                    }

                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(NotaCredito entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.CreditoId > 0)
                {
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
          
                return Mensaje;
            }

            public NotaCredito ObtenerPorId(long id, bool todo)
            {
                NotaCredito NotaCreditoActual = new NotaCredito();

                try
                {
                    if (todo)
                    {
                        NotaCreditoActual = db.Set<NotaCredito>().Include("Cliente").Include("Agencia").Include("Factura").Include("Factura.Cliente").Include("Factura.Serie").Include("UsuarioCreo").Include("Pagos").Include("Pagos.FormaPago").Where(x => x.CreditoId == id).FirstOrDefault();                  
                    }
                    else 
                    {
                        NotaCreditoActual = db.Set<NotaCredito>().Where(x => x.CreditoId == id).FirstOrDefault();                    
                    }
                   
                }
                catch (Exception)
                {
                }

                return NotaCreditoActual;
            }

            public List<NotaCredito> ObtenerListadoPorFecha(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<NotaCredito> NotaCreditos = new List<NotaCredito>();

                try
                {
                    NotaCreditos = db.Set<NotaCredito>().Include("Agencia").Include("UsuarioCreo").Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.CreditoId).ToList();
                }
                catch (Exception)
                {
                }

                return NotaCreditos;
            }
           
            public string Anular(long creditoId, string comentario, long usuarioId)
            {
                string Mensaje = "OK";

                try
                {

                    NotaCredito NotaCreditoActual = db.Set<NotaCredito>().Where(x => x.CreditoId == creditoId).FirstOrDefault();
                    if (NotaCreditoActual == null)
                    {
                        return "La nota de credito que selecciono no se encuentra disponible";
                    }                   
                                        
                    NotaCreditoActual.Comentario = comentario;
                    NotaCreditoActual.Anulada = true;
                    NotaCreditoActual.UsrAnular = usuarioId;
                    NotaCreditoActual.FechaAnular = DateTime.Now;
                   
                    db.SaveChanges();                   
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public List<NotaCredito> ObtenerTodasNotasCreditos() 
            {
                List<NotaCredito> Notas = new List<NotaCredito>();

                try
                {
                    Notas = db.Set<NotaCredito>().Where(x => !x.Operado && !x.Anulada).AsEnumerable().Select(x => new NotaCredito() { CreditoId = x.CreditoId, Nota = string.Format("{0} - {1}({2:C})",x.Serie, x.NoNotaCredito, x.Monto) }).ToList();
                }
                catch (Exception)
                {
                }

                return Notas;
            }
       
        #endregion
    }
}
