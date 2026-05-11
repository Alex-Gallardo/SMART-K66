using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class VendedorBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public VendedorBL()
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
                    Vendedor VendedorActual = db.Set<Vendedor>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (VendedorActual != null)
                    {
                        Inicial_Id = VendedorActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private string Agregar(Vendedor entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngVendedorId = new Herramienta().Formato_Correlativo(Id);

                        if (lngVendedorId > 0)
                        {
                            entidad.VendedorId = lngVendedorId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                            {
                                foreach (VendedorAgencia Vendedor in entidad.Agencias)
                                {
                                    Vendedor.VendedorId = entidad.VendedorId;                                    
                                }                                
                            }

                            if (entidad.Escalas != null && entidad.Escalas.Count() > 0)
                            {
                                int i = 1;
                                foreach (VendedorEscala Escala in entidad.Escalas)
                                {
                                    Escala.EscalaId = i;
                                    Escala.VendedorId = entidad.VendedorId;
                                    i++;
                                }
                            }

                            if (entidad.Metas != null && entidad.Metas.Count() > 0)
                            {
                                foreach (VendedorMeta Meta in entidad.Metas)
                                {
                                    Meta.ResponsableId = entidad.ResponsableId;
                                    Meta.VendedorId = entidad.VendedorId;
                                }
                            }

                            if (entidad.MetasxDia != null && entidad.MetasxDia.Count() > 0)
                            {
                                foreach (VendedorMetaxDia Meta in entidad.MetasxDia)
                                {
                                    Meta.ResponsableId = entidad.ResponsableId;
                                    Meta.VendedorId = entidad.VendedorId;
                                }
                            }

                            db.Set<Vendedor>().Add(entidad);
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

            private string Actualizar(Vendedor entidad)
            {
                string Mensaje = "OK";

                try
                {

                    Vendedor VendedorActual = ObtenerPorId(entidad.VendedorId);

                    if (VendedorActual.VendedorId > 0)
                    {
                        VendedorActual.EmpresaId = entidad.EmpresaId;

                        VendedorActual.Nombre = entidad.Nombre;                       
                        VendedorActual.Activo = entidad.Activo;

                        if (entidad.Agencias != null && entidad.Agencias.Count() > 0)
                        {
                            var Agencias = db.Set<VendedorAgencia>().Where(x => x.VendedorId == entidad.VendedorId);
                            db.Set<VendedorAgencia>().RemoveRange(Agencias);

                            foreach (var Agencia in entidad.Agencias)
                            {
                                Agencia.VendedorId = entidad.VendedorId;
                                db.Set<VendedorAgencia>().Add(Agencia);
                            }
                        }

                        if (entidad.Escalas != null && entidad.Escalas.Count() > 0)
                        {
                            var Escalas = db.Set<VendedorEscala>().Where(x => x.VendedorId == entidad.VendedorId);
                            db.Set<VendedorEscala>().RemoveRange(Escalas);

                            int i = 1;
                            foreach (var Escala in entidad.Escalas)
                            {
                                Escala.EscalaId = i;
                                Escala.VendedorId = entidad.VendedorId;
                                db.Set<VendedorEscala>().Add(Escala);
                                i++;
                            }
                        }

                        if (entidad.Metas != null && entidad.Metas.Count() > 0)
                        {
                            var Metas = db.Set<VendedorMeta>().Where(x => x.VendedorId == entidad.VendedorId && x.MontoMensualReal == 0);
                            db.Set<VendedorMeta>().RemoveRange(Metas);

                            foreach (var Meta in entidad.Metas)
                            {
                                Meta.VendedorId = entidad.VendedorId;
                                Meta.ResponsableId = entidad.ResponsableId;
                                db.Set<VendedorMeta>().Add(Meta);
                            }
                        }

                        if (entidad.MetasxDia != null && entidad.MetasxDia.Count() > 0)
                        {
                            var Metas = db.Set<VendedorMetaxDia>().Where(x => x.VendedorId == entidad.VendedorId);
                            db.Set<VendedorMetaxDia>().RemoveRange(Metas);

                            foreach (var Meta in entidad.MetasxDia)
                            {
                                Meta.VendedorId = entidad.VendedorId;
                                Meta.ResponsableId = entidad.ResponsableId;
                                db.Set<VendedorMetaxDia>().Add(Meta);
                            }
                        }

                        db.SaveChanges();                        
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

            public string Guardar(Vendedor entidad)
            {
                string Mensaje = "OK";
              
                if (entidad.VendedorId > 0)
                {
                    Mensaje = Actualizar(entidad);
                }
                else
                {
                    Mensaje = Agregar(entidad);
                }
             
                return Mensaje;
            }

            public Vendedor ObtenerPorId(long id, bool todo = false)
            {
                Vendedor VendedorActual = new Vendedor();

                try
                {
                    if (todo)
                    {
                        VendedorActual = db.Set<Vendedor>().Include("Agencias").Include("Agencias.Agencia").Include("Escalas").Include("Metas").Include("Metas.Mes").Include("MetasxDia").Where(x => x.VendedorId == id).FirstOrDefault();
                    }
                    else
                    {
                        VendedorActual = db.Set<Vendedor>().Where(x => x.VendedorId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {}

                return VendedorActual;
            }

            public List<Vendedor> ObtenerListado(bool todos, long empresaId)
            {
                List<Vendedor> Vendedors = new List<Vendedor>();

                try
                {
                    if (todos)
                    {
                        Vendedors = db.Set<Vendedor>().Include("Agencias").AsNoTracking().Where(x => x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                    }
                    else
                    {
                        Vendedors = db.Set<Vendedor>().AsNoTracking().Where(x => x.Activo).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Vendedors;
            }

            public List<Vendedor> Buscar(string search, long empresaId)
            {
                List<Vendedor> Vendedors = new List<Vendedor>();

                try
                {
                    Vendedors = db.Set<Vendedor>().Include("Agencias").AsNoTracking().Where(x => x.Nombre.Contains(search) && x.EmpresaId == empresaId).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                }
                catch (Exception)
                {}

                return Vendedors;
            }

            public List<Vendedor> ObtenerVendedoresPorAgencia(long agenciaId, bool aplicarFiltroActivo = true) 
            {
                List<Vendedor> Vendedores = new List<Vendedor>();

                try
                {
                    if (aplicarFiltroActivo)
                    {
                        Vendedores = db.Set<VendedorAgencia>().Where(x => x.AgenciaId == agenciaId).Join(db.Set<Vendedor>().Where(x => x.Activo == true), VA => VA.VendedorId, V => V.VendedorId, (VA, V) => new { V }).Select(x => x.V).ToList();
                    }
                    else
                    {
                        Vendedores = db.Set<VendedorAgencia>().Where(x => x.AgenciaId == agenciaId).Join(db.Set<Vendedor>(), VA => VA.VendedorId, V => V.VendedorId, (VA, V) => new { V }).Select(x => x.V).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Vendedores;
            }

            public MetaModel ObtenerVentaYMetaxVendedor(DateTime fecha, long usuarioId) 
            {
                MetaModel Metas = new MetaModel();
                bool MetaxDia = false;

                try
                {
                    //Se inicializa los valores
                    Metas.Vendedor = new Vendedor();
                    Metas.Meta = new VendedorMeta();
                    Metas.MetaxDia = new VendedorMetaxDia();
                    Metas.Comision = 0;

                    //Se obtiene al vendedor
                    Metas.Vendedor = db.Set<Usuario>().AsNoTracking().Where(x => x.UsuarioId == usuarioId).Join(db.Set<Vendedor>().AsNoTracking(), U => U.VendedorId, V => V.VendedorId, (U, V) => new { V }).Select(x => x.V).FirstOrDefault();
                    if (Metas.Vendedor != null)
                    {
                        //Se obtiene la meta del vendedor
                        Configuracion ConfiguracionActual = db.Set<Configuracion>().AsNoTracking().Where(x => x.ConfiguracionId == 20210412002).FirstOrDefault();
                        if (ConfiguracionActual != null)
                        {
                            if (ConfiguracionActual.Valor.Equals("1"))
                            {
                                MetaxDia = true;
                            }                            
                        }

                        Metas.VentaxDia = MetaxDia;

                        if (MetaxDia)
                        {
                            Metas.MetaxDia = db.Set<VendedorMetaxDia>().AsNoTracking().Where(x => x.VendedorId == Metas.Vendedor.VendedorId && x.Fecha == fecha).FirstOrDefault();
                        }
                        else
                        {
                            Metas.Meta = db.Set<VendedorMeta>().AsNoTracking().Where(x => x.VendedorId == Metas.Vendedor.VendedorId && x.MesId == fecha.Month && x.Anio == fecha.Year).FirstOrDefault();
                        }
                    }

                    if (Metas.Vendedor != null && Metas.Meta != null)
                    {
                        if (MetaxDia)
                        {
                            Metas.MontoMeta = Metas.MetaxDia.MontoxDia;
                        }
                        else
                        {
                            Metas.MontoMeta = Metas.Meta.MontoMensualMeta;
                        }

                        //Se obtienen recibos pagados
                        List<Recibo> Ventas = new List<Recibo>();

                        if (MetaxDia)
                        {
                            Ventas = db.Set<Recibo>().Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.Pagada && x.VendedorId == Metas.Vendedor.VendedorId && x.Fecha == fecha).ToList();
                        }
                        else
                        {
                            Ventas = db.Set<Recibo>().Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.Pagada && x.VendedorId == Metas.Vendedor.VendedorId && x.Fecha.Month == fecha.Month && x.Fecha.Year == fecha.Year).ToList();
                        }
                    
                        if (Ventas != null && Ventas.Count() > 0)
                        {
                            Metas.MontoVenta = Ventas.Sum(x => x.Detalles.Sum(y => y.Cantidad * y.Precio));
                        }

                        //Se obtienen facturas pagadas
                        List<Factura> FacturaVentas = new List<Factura>();

                        if (MetaxDia)
                        {
                            FacturaVentas = db.Set<Factura>().Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.Pagada && x.VendedorId == Metas.Vendedor.VendedorId && x.Fecha == fecha).ToList();
                        }
                        else
                        {
                            FacturaVentas = db.Set<Factura>().Include("Detalles").AsNoTracking().Where(x => !x.Anulada && x.Pagada && x.VendedorId == Metas.Vendedor.VendedorId && x.Fecha.Month == fecha.Month && x.Fecha.Year == fecha.Year).ToList();
                        }
                        
                        if (FacturaVentas != null && FacturaVentas.Count() > 0)
                        {
                            Metas.MontoVenta += FacturaVentas.Sum(x => x.Detalles.Sum(y => y.Cantidad * y.Precio));
                        }

                        Metas.MontoFaltante = Metas.MontoMeta - Metas.MontoVenta;

                        //Se obtiene la comision del vendedor                    
                        VendedorEscala EscalaActual = db.Set<VendedorEscala>().AsNoTracking().Where(x => x.VendedorId == Metas.Vendedor.VendedorId && Metas.MontoVenta >= x.Inicio && Metas.MontoVenta <= x.Fin).FirstOrDefault();
                        if (EscalaActual != null)
                        {
                            Metas.Comision = EscalaActual.Porcentaje;
                        }
                    }
                }
                catch (Exception)
                {}

                return Metas;
            }

        #endregion
    }
}
