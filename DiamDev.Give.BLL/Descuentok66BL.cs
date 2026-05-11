using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class Descuentok66BL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public Descuentok66BL()
            {
                this.db = new GiveContext();
            }

        #endregion

        #region Metodos Privados                  
        
            private string Agregar(DescuentoK66 entidad)
            {
                string Mensaje = "OK";

                try
                {
                    bool Existe = db.Set<DescuentoK66>().AsNoTracking().Where(x => x.IDK66 == entidad.IDK66 && x.EmpresaId == entidad.EmpresaId && x.ProductoId == entidad.ProductoId).Count() > 0;
                    if (Existe)
                    {
                        return "Se le informa que ya existe el producto asignado al cliente";
                    }

                    entidad.Fecha = DateTime.Today;    
                    db.Set<DescuentoK66>().Add(entidad);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private string Actualizar(DescuentoK66 entidad)
            {
                string Mensaje = "OK";

                try
                {
                    DescuentoK66 DescuentoActual = db.Set<DescuentoK66>().Where(x => x.DescuentoId == entidad.DescuentoId).FirstOrDefault();
                    if (DescuentoActual != null)
                    {
                        DescuentoActual.Descuento = entidad.Descuento;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            private bool Existe(Guid id) 
            {
                bool Existe = false;

                try
                {
                    Existe = db.Set<DescuentoK66>().AsNoTracking().Where(x => x.DescuentoId == id).Count() > 0;
                }
                catch (Exception)
                {}    

                return Existe;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(DescuentoK66 entidad)
            {
                string Mensaje = "OK";

                if (Existe(entidad.DescuentoId))
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }

                return Mensaje;
            }

            public string Eliminar(Guid id)
            {
                string Mensaje = "OK";

                try
                {
                    DescuentoK66 DescuentoActual = db.Set<DescuentoK66>().Where(x => x.DescuentoId == id).FirstOrDefault();
                    if (DescuentoActual != null)
                    {
                        db.Set<DescuentoK66>().Remove(DescuentoActual);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = string.Format("Descripción del Error {0}", ex.Message);
                }

                return Mensaje;
            }

            public DescuentoK66 ObtenerxId(Guid id) 
            {
                DescuentoK66 DescuentoActual = new DescuentoK66();

                try
                {
                    DescuentoActual = db.Set<DescuentoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => x.DescuentoId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return DescuentoActual;
            }

            public List<DescuentoK66> ObtenerListadoxFecha(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<DescuentoK66> Descuentos = new List<DescuentoK66>();

                try
                {
                    Descuentos = db.Set<DescuentoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DescuentoId).ToList();
                }
                catch (Exception)
                {}

                return Descuentos;
            }

            public List<DescuentoK66> ObtenerListadoxEmpresa(long empresaId)
            {
                List<DescuentoK66> Descuentos = new List<DescuentoK66>();

                try
                {
                    Descuentos = db.Set<DescuentoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DescuentoId).ToList();
                }
                catch (Exception)
                { }

                return Descuentos;
            }

            public List<DescuentoK66> Buscar(string search)
            {
                List<DescuentoK66> Descuentos = new List<DescuentoK66>();               

                try
                {
                    Descuentos = db.Set<DescuentoK66>().Include("Empresa").Include("Responsable").AsNoTracking().Where(x => (x.IDK66.ToLower().Contains(search.ToLower()) || x.Nombre.ToLower().Contains(search.ToLower()) || x.ProductoId.ToLower().Contains(search.ToLower()) || x.Producto.ToLower().Contains(search.ToLower()))).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DescuentoId).ToList();
                }
                catch (Exception)
                {}

                return Descuentos;
            }            
            
        #endregion
    }
}
