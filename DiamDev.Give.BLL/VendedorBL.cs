using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            private bool Agregar(Vendedor entidad)
            {
                bool VendedorAgregar = false;

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
                           
                            db.Set<Vendedor>().Add(entidad);
                            db.SaveChanges();
                            VendedorAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return VendedorAgregar;
            }

            private bool Actualizar(Vendedor entidad)
            {
                bool VendedorActualizar = false;

                try
                {

                    Vendedor VendedorActual = ObtenerPorId(entidad.VendedorId);

                    if (VendedorActual.VendedorId > 0)
                    {                        
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

                        db.SaveChanges();
                        VendedorActualizar = true;
                    }

                }
                catch (Exception)
                {
                }

                return VendedorActualizar;
            }

        #endregion

        #region Metodos Publicos

            public string Guardar(Vendedor entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.VendedorId > 0)
                {
                    OperacionExitosa = Actualizar(entidad);
                }
                else
                {
                    OperacionExitosa = Agregar(entidad);
                }

                if (!OperacionExitosa)
                {
                    Mensaje = "La información ingresada no es valida";
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
                        VendedorActual = db.Set<Vendedor>().Include("Agencias").Include("Agencias.Agencia").Where(x => x.VendedorId == id).FirstOrDefault();
                    }
                    else
                    {
                        VendedorActual = db.Set<Vendedor>().Where(x => x.VendedorId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return VendedorActual;
            }

            public List<Vendedor> ObtenerListado(bool todos)
            {
                List<Vendedor> Vendedors = new List<Vendedor>();

                try
                {
                    if (todos)
                    {
                        Vendedors = db.Set<Vendedor>().Include("Agencias").OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                    }
                    else
                    {
                        Vendedors = db.Set<Vendedor>().Where(x => x.Activo == true).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                    }
                }
                catch (Exception)
                {
                }

                return Vendedors;
            }

            public List<Vendedor> Buscar(string search)
            {
                List<Vendedor> Vendedors = new List<Vendedor>();

                try
                {
                    Vendedors = db.Set<Vendedor>().Include("Agencias").Where(x => x.Nombre.Contains(search)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.VendedorId).ToList();
                }
                catch (Exception)
                {
                }

                return Vendedors;
            }

            public List<Vendedor> ObtenerVendedoresPorAgencia(long agenciaId) 
            {
                List<Vendedor> Vendedores = new List<Vendedor>();

                try
                {
                    Vendedores = db.Set<VendedorAgencia>().Where(x => x.AgenciaId == agenciaId).Join(db.Set<Vendedor>().Where(x => x.Activo == true), VA => VA.VendedorId, V => V.VendedorId, (VA,V) => new { V }).Select(x => x.V).ToList();
                }
                catch (Exception)
                {
                }

                return Vendedores;
            }

        #endregion

    }
}
