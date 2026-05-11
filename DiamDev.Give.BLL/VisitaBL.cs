using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class VisitaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public VisitaBL()
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
                    Visita VisitaActual = db.Set<Visita>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (VisitaActual != null)
                    {
                        Inicial_Id = VisitaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Visita entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngVisitaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngVisitaId > 0)
                        {
                            entidad.VisitaId = lngVisitaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            db.Set<Visita>().Add(entidad);
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

            public string Guardar(Visita entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.VisitaId == 0)   
                {
                    Mensaje = Agregar(entidad);
                }            

                return Mensaje;
            }           

            public List<Visita> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal, long responsableId)
            {
                List<Visita> Visitas = new List<Visita>();

                try
                {
                    Visitas = db.Set<Visita>().Include("Empresa").Include("TipoVisita").Include("Responsable").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.ResponsableId == responsableId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VisitaId).ToList();
                }
                catch (Exception)
                {}

                return Visitas;
            }

            public string ObtenerLocalizacionVisita(DateTime fechaInicial, DateTime fechaFinal, long responsableId)
            {
                string markers = "[";

                try
                {
                    List<Visita> Visitas = db.Set<Visita>().Include("TipoVisita").Include("Responsable").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal && x.ResponsableId == responsableId && x.Latitud != null && x.Longitud != null).ToList();
                    if (Visitas != null && Visitas.Count() > 0)
                    {
                        Visitas.ForEach(x =>
                        {
                            markers += "{";
                            markers += string.Format("'id': '{0}',", x.VisitaId);
                            markers += string.Format("'title': '{0}',", string.Format("{0} - {1}", x.IDK66, x.Nombre.ToUpper()));
                            markers += string.Format("'lat': '{0}',", x.Latitud);
                            markers += string.Format("'lng': '{0}',", x.Longitud);
                            markers += string.Format("'description': 'CLIENTE: {0} <br/> VISITA ID: {1} <br/> FECHA DE VISITA: {2}'", x.Nombre.ToUpper(), x.VisitaId, x.Fecha.ToString("dd/MM/yyyy"));
                            markers += "},";
                        });
                    }

                    markers += "];";
                }
                catch (Exception)
                { }

                return markers;
            }

        #endregion
    }
}
