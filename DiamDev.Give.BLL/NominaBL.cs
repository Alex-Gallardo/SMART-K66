using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class NominaBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public NominaBL()
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
                    Nomina NominaActual = db.Set<Nomina>().AsNoTracking().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (NominaActual != null)
                    {
                        Inicial_Id = NominaActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {
                }

                return Id;
            }

            private bool Agregar(Nomina entidad)
            {
                bool NominaAgregar = false;

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngNominaId = new Herramienta().Formato_Correlativo(Id);

                        if (lngNominaId > 0)
                        {
                            entidad.NominaId = lngNominaId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            string Mensaje = string.Empty;

                            switch (entidad.TipoId)
                            {
                                case 1:
                                    Mensaje = string.Format("Nomina de Quincena del Mes de {0} del año {1}", new Herramienta().MesTexto(entidad.FechaInicial.Month), entidad.FechaInicial.Year);
                                    break;
                                case 2:
                                    Mensaje = string.Format("Nomina del Mes de {0} del año {1}", new Herramienta().MesTexto(entidad.FechaInicial.Month), entidad.FechaInicial.Year);
                                    break;
                                case 3:
                                    Mensaje = string.Format("Nomina de Bono 14 del año {0}", entidad.FechaInicial.Year);
                                    break;
                                case 4:
                                    Mensaje = string.Format("Nomina de Aguinaldo año {0}", entidad.FechaInicial.Year);
                                    break;
                            }

                            entidad.Descripcion = Mensaje;

                            if (entidad.Detalles != null && entidad.Detalles.Count() > 0)
                            {
                                int i = 1;
                                foreach (var Detalle in entidad.Detalles)
                                {
                                    Detalle.DetalleId = i;
                                    Detalle.NominaId = entidad.NominaId;
                                    i++;
                                }
                            }

                            db.Set<Nomina>().Add(entidad);
                            db.SaveChanges();
                            NominaAgregar = true;
                        }
                    }

                }
                catch (Exception)
                {
                }

                return NominaAgregar;
            }

            private decimal CalcularPago(int pagoId, decimal salario)
            {
                decimal Total = 0;
                decimal Salario = salario;

                try
                {
                    if (Salario > 0)
                    {
                        switch (pagoId)
                        {
                            case 1:
                                Total = Salario;
                                break;
                            case 2:
                                Total = decimal.Round((Salario / 30) * 15, 2);
                                break;
                            case 3:
                                Total = decimal.Round((Salario / 30) * 7, 2);
                                break;
                            case 4:
                                Total = decimal.Round(Salario / 30, 2);
                                break;
                            case 5:
                                Total = decimal.Round((Salario / 30) / 8, 2);
                                break;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Total;
            }
        #endregion

        #region Metodos Publicos

            public string Guardar(Nomina entidad)
            {
                string Mensaje = "OK";
                bool OperacionExitosa = false;

                if (entidad.NominaId > 0)
                {

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

            public Nomina ObtenerPorId(long id, bool todo = false)
            {
                Nomina NominaActual = new Nomina();

                try
                {
                    if (todo)
                    {
                        NominaActual = db.Set<Nomina>().Include("Tipo").Include("Detalles").Include("Detalles.Personal").Where(x => x.NominaId == id).FirstOrDefault();
                    }
                    else
                    {
                        NominaActual = db.Set<Nomina>().Where(x => x.NominaId == id).FirstOrDefault();
                    }
                }
                catch (Exception)
                {
                }

                return NominaActual;
            }

            public List<Nomina> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Nomina> Nominas = new List<Nomina>();

                try
                {
                    Nominas = db.Set<Nomina>().Include("Tipo").Include("Detalles").AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.NominaId).ToList();
                }
                catch (Exception)
                {
                }

                return Nominas;
            }

            public List<NominaModel> ObtenerListadoNominaLiquidar(DateTime fechaInicial, DateTime fechaFinal, int tipoId)
            {
                List<NominaModel> Empleados = new List<NominaModel>();

                try
                {
                    List<Personal> EmpleadosActivos = db.Set<Personal>().Include("Puesto").AsNoTracking().Where(x => x.Activo == true).ToList();
                    if (EmpleadosActivos != null && EmpleadosActivos.Count() > 0)
                    {
                        List<AnotacionTipo> Tipos = db.Set<AnotacionTipo>().AsNoTracking().ToList();
                        Configuracion ConfiguracionIGSS = db.Set<Configuracion>().AsNoTracking().Where(x => x.Identificador.Equals("IGSS%")).FirstOrDefault();
                        double IGSSPorcentaje = 0.0483;
                        if (ConfiguracionIGSS != null)
                        {
                            IGSSPorcentaje = double.Parse(ConfiguracionIGSS.Valor);
                        }

                        int DiasDelMes = DateTime.DaysInMonth(fechaFinal.Year, fechaFinal.Month);

                        foreach (Personal item in EmpleadosActivos)
                        {
                            NominaModel Empleado = new NominaModel();
                            Empleado.PersonalId = item.PersonalId;
                            Empleado.Nombre = item.Nombre;
                            Empleado.Puesto = item.Puesto.Nombre;

                            if (tipoId == 1 || tipoId == 2)
                            {
                                List<Anotacion> AnotacionesDelMes = db.Set<Anotacion>().AsNoTracking().Where(x => x.FechaInicial >= fechaInicial && x.FechaFinal <= fechaFinal && x.PersonalId == item.PersonalId).ToList();
                                if (AnotacionesDelMes != null && AnotacionesDelMes.Count() > 0)
                                {
                                    int DiasDescontar = 0;
                                    foreach (Anotacion AnotacionActual in AnotacionesDelMes)
                                    {
                                        if (AnotacionActual.TipoId == 20170301002)
                                        {
                                            DateTime fechaInicialAnotacion = AnotacionActual.FechaInicial;
                                            DateTime fechaFinalAnotacion = AnotacionActual.FechaFinal;
                                            TimeSpan duracion = fechaFinalAnotacion - fechaInicialAnotacion;
                                            DiasDescontar += duracion.Days;
                                        }

                                        bool Descuento = Tipos.Where(x => x.Descuento && x.TipoId == AnotacionActual.TipoId).Count() > 0;
                                        if (Descuento)
                                        {
                                            Empleado.OtrosDescuentos += AnotacionActual.Monto;
                                        }
                                    }

                                    if (DiasDescontar == 0)
                                    {
                                        Empleado.Dias = DiasDelMes;
                                        Empleado.Sueldo = item.Sueldo;
                                        Empleado.Bonificacion = item.Bonificacion;
                                    }
                                    else if (DiasDescontar > DiasDelMes)
                                    {
                                        Empleado.Dias = 0;
                                        Empleado.Sueldo = 0;
                                        Empleado.Bonificacion = 0;
                                    }
                                    else
                                    {
                                        Empleado.Dias = DiasDelMes - DiasDescontar;

                                        if (tipoId == 1)
                                        {
                                            Empleado.Sueldo = Empleado.Dias * CalcularPago(2, item.Sueldo);
                                            Empleado.Bonificacion = Empleado.Dias * CalcularPago(2, item.Bonificacion); 
                                        }
                                        else if (tipoId == 2)
                                        {
                                            Empleado.Sueldo = Empleado.Dias * CalcularPago(4, item.Sueldo);
                                            Empleado.Bonificacion = Empleado.Dias * CalcularPago(4, item.Bonificacion);
                                        }
                                       
                                    }
                                }
                                else
                                {
                                    Empleado.Dias = DiasDelMes;
                                    Empleado.Sueldo = item.Sueldo;
                                    Empleado.Bonificacion = item.Bonificacion;
                                    Empleado.OtrosDescuentos = 0;
                                }

                                Empleado.OtrosIngresos = 0;

                                if (item.IGSS)
                                {
                                    double CalculoIGSS = (Convert.ToDouble(Empleado.Sueldo) + Convert.ToDouble(Empleado.Bonificacion)) * IGSSPorcentaje;
                                    Empleado.IGSS = decimal.Round(Convert.ToDecimal(CalculoIGSS), 2);
                                }
                                else
                                {
                                    Empleado.IGSS = 0;
                                }
                            }
                            else if (tipoId == 3)
                            {
                                Empleado.Dias = 365;
                            }
                            else if (tipoId == 4)
                            {
                                Empleado.Dias = 365;
                            }

                            Empleado.SubTotal = (Empleado.Sueldo + Empleado.Bonificacion + Empleado.OtrosIngresos) - (Empleado.IGSS + Empleado.OtrosDescuentos);
                            Empleados.Add(Empleado);
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Empleados;
            }

        #endregion
    }
}
