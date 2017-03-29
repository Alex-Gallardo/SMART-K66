using DiamDev.Give.BLL.Excel;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Globalization;

namespace DiamDev.Give.UI.Controllers
{
    public class ProductosCargaMasivaController : Controller
    {
        // GET: ProductoImportacion
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(List<HttpPostedFileBase> archivos)
        {

            bool success = false;
            string error = null;


            if (archivos == null || archivos.Count < 1)
            {
                error = "El archivo está vacío";
                return Resultado(success, error);
            }

            var archivo = archivos.FirstOrDefault(x => x != null && x.ContentLength > 0 && !string.IsNullOrWhiteSpace(x.FileName));

            if (!archivo.FileName.EndsWith("xlsx"))
            {
                error = "El archivo debe ser un excel (.xlsx) valido.";
                return Resultado(success, error);
            }

            var usuario = User.Identity.Name;
            //var id = Guid.NewGuid();
            var hoy = DateTime.Today;
            var anio = hoy.Year;
            var mes = hoy.Month;
            var dia = hoy.Day;
            var fileDir = Server.MapPath("~/App_Data/Productos/");
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            var correlativo = Directory.GetFiles(fileDir, $"{anio}-{mes:00}-{dia:00}-*.xlsx").Length + 1;
            var fileName = Path.Combine(fileDir, $"{anio}-{mes:00}-{dia:00}-{correlativo}.xlsx");
            archivo.SaveAs(fileName);

            var nombresColumnas = new[] { "ID", "CODIGO", "BODEGA", "NOMBRE", "MARCA", "CANTIDAD", "COSTO", "P VENTA", "MIN", "MAX", "RENT Q.", "RENT %", "MODIFICACION" };
            var rowsCount = 0;

            var filas = new List<ProductoCargaMasivaDetalle>();

            foreach (var worksheet in Workbook.Worksheets(fileName))
            {
                foreach (var row in worksheet.Rows)
                {
                    rowsCount++;

                    if (rowsCount == 1)
                    {
                        var nombresEnArchivo = row.Cells.Select(x => x.Text.Trim()).ToArray();
                        for (int i = 0; i < nombresColumnas.Length; i++)
                        {
                            if (nombresColumnas[i] != nombresEnArchivo[i])
                            {
                                error = "la primer linea del archivo debe contener el nombre de las columnas: " +
                                    "[" + string.Join(", ", nombresColumnas) + "]";
                                return Resultado(success, error);
                            }
                        }
                        continue;
                    }

                    var cells = row.Cells;

                    if (cells.Length < 12)
                    {
                        error = $"Error en fila {filas.Count + 1}, debe tener al menos 12 columnas.";
                        return Resultado(success, error);
                    }

                    double rowId;
                    string codigo;
                    string bodega;
                    string nombre;
                    string marca;
                    double cantidad;
                    double costo;
                    double precioVenta;
                    double min;
                    double max;
                    double rentQ;
                    double rentP;
                    string modificacion;


                    rowId = cells[0]?.Amount ?? 0;
                    codigo = cells[1]?.Text?.Trim() ?? "";
                    bodega = cells[2]?.Text?.Trim() ?? "";
                    nombre = cells[3]?.Text?.Trim() ?? "";
                    marca = cells[4]?.Text?.Trim() ?? "";
                    cantidad = cells[5]?.Amount ?? 0;
                    costo = cells[6]?.Amount ?? 0;
                    precioVenta = cells[7]?.Amount ?? 0;
                    min = cells[8]?.Amount ?? 0;
                    max = cells[9]?.Amount ?? 0;
                    rentQ = cells[10]?.Amount ?? 0;
                    rentP = cells[11]?.Amount ?? 0;
                    modificacion = cells.Length > 12 ? cells[12]?.Text?.Trim() ?? "" : "";

                    if (rowId > 0 || !string.IsNullOrWhiteSpace(codigo))
                    {
                        filas.Add(new ProductoCargaMasivaDetalle
                        {
                            Id = rowId,
                            Codigo = codigo,
                            Bodega = bodega,
                            Nombre = nombre,
                            Marca = marca,
                            Cantidad = cantidad,
                            Costo = costo,
                            PrecioVenta = precioVenta,
                            Min = min,
                            Max = max,
                            RentQ = rentQ,
                            RentP = rentP,
                            Modificacion = modificacion
                        });
                    }

                }
            }

            if (filas.Count == 0)
            {
                error = "El archivo está vacío.";
                return Resultado(success, error);
            }

            var jsonName = Path.Combine(fileDir, Path.GetFileNameWithoutExtension(fileName) + ".json");
            var json = JsonConvert.SerializeObject(filas);

            System.IO.File.WriteAllText(jsonName, json);

            success = true;

            return Resultado(success, error, Path.GetFileNameWithoutExtension(fileName));
        }

        private ActionResult Resultado(bool success, string error, string fileName = null)
        {
            bool isAjaxRequest = Request["ajax"] == "1";
            if (isAjaxRequest)
            {
                return Json(new { success, error, fileName });
            }
            else
            {
                ViewBag.UploadSuccess = success;
                ViewBag.UploadError = error;
                ViewBag.UploadFileName = fileName;
                return View();
            }
        }


        public ActionResult Verificar(string id)
        {
            var fileDir = Server.MapPath("~/App_Data/Productos/");

            var valid = ValidarId(id, fileDir);
            if (valid != null) return valid;

            var archivo = Path.Combine(fileDir, id + ".json");
            var json = System.IO.File.ReadAllText(archivo);
            var productos = JsonConvert.DeserializeObject<ProductoCargaMasivaDetalle[]>(json);

            var errores = PersistirCambios(productos, commit: false);
            errores.ForEach(error => ModelState.AddModelError("", error));

            ViewBag.Id = id;
            ViewBag.IsValid = ModelState.IsValid;
            return View(productos);
        }

        public ActionResult Revisar(string id)
        {
            var fileDir = Server.MapPath("~/App_Data/Productos/Verificados");

            var valid = ValidarId(id, fileDir);
            if (valid != null) return valid;

            var archivo = Path.Combine(fileDir, id + ".json");
            var json = System.IO.File.ReadAllText(archivo);
            var productos = JsonConvert.DeserializeObject<ProductoCargaMasivaDetalle[]>(json);

            ViewBag.Id = id;
            return View(productos);
        }

        [HttpPost]
        public ActionResult Eliminar(string id)
        {
            var fileDir = Server.MapPath("~/App_Data/Productos/Verificados");

            var valid = ValidarId(id, fileDir);
            if (valid != null) return valid;

            var archivo = Path.Combine(fileDir, id + ".json");

            System.IO.File.Delete(archivo);

            return RedirectToAction("Verificados");
        }

        [HttpPost]
        public ActionResult Aprobar(string id)
        {
            var fileDir = Server.MapPath("~/App_Data/Productos/Verificados");

            var valid = ValidarId(id, fileDir);
            if (valid != null) return valid;

            var archivo = Path.Combine(fileDir, id + ".json");
            var json = System.IO.File.ReadAllText(archivo);
            var productos = JsonConvert.DeserializeObject<ProductoCargaMasivaDetalle[]>(json);
            ViewBag.Id = id;

            var errores = PersistirCambios(productos, commit: true);

            if (errores.Count > 0)
            {
                errores.ForEach(error => ModelState.AddModelError("", error));
                return View("Revisar", productos);
            }

            System.IO.File.Delete(archivo);
            return RedirectToAction("Verificados");
        }

        private List<string> PersistirCambios(ProductoCargaMasivaDetalle[] productos, bool commit)
        {
            var errores = new List<string>();
            var error = "";

            int correlativo = 0;
            var hoy = DateTime.Now;



            using (var db = new GiveContext())
            {

                using (var trx = db.Database.BeginTransaction())
                {


                    foreach (var item in productos)
                    {
                        var agencia = db.Agencias.FirstOrDefault(x => x.Nombre.ToLower() == item.Bodega.ToLower());
                        if (agencia == null)
                        {
                            error = "No existe la bodega '" + item.Bodega + "'.";
                            if (!errores.Contains(error))
                                errores.Add(error);

                            continue;
                        }

                        Producto producto;
                        Marca marca;
                        decimal cantidad;
                        decimal costo;

                        if (string.IsNullOrWhiteSpace(item.Modificacion))
                        {
                            // editar
                            producto = db.Productos.Include(x => x.Precios).FirstOrDefault(x => x.ProductoId == item.Id.ToString() || x.Codigo == item.Codigo);

                            if (producto == null)
                            {
                                error = "No existe el producto con id '" + item.Id + "' ni con codigo '" + item.Codigo + "'.";
                                if (!errores.Contains(error))
                                    errores.Add(error);

                                continue;
                            }

                            marca = db.Marcas.Where(x => x.Nombre.ToLower().Trim() == item.Marca.ToLower().Trim()).FirstOrDefault();

                            if (marca == null)
                            {
                                var fecha = DateTime.Now;
                                var marcaCorrelativo = db.Marcas.Where(x => x.Fecha.Year == fecha.Year && x.Fecha.Month == fecha.Month && x.Fecha.Day == fecha.Day).OrderByDescending(x => x.Correlativo).Select(x => x.Correlativo).FirstOrDefault();

                                marcaCorrelativo = marcaCorrelativo > 0 ? marcaCorrelativo + 1 : 1;

                                var marcaId = long.Parse(string.Format("{0:yyyyMMdd}{1:000}", fecha, marcaCorrelativo));

                                marca = new Marca
                                {
                                    Correlativo = marcaCorrelativo,
                                    MarcaId = marcaId,
                                    Activo = true,
                                    Fecha = DateTime.Now,
                                    Nombre = item.Marca
                                };

                                db.Marcas.Add(marca);
                                producto.Marca = marca;
                            }

                            var productoBodega = db.ProductoInventarios.FirstOrDefault(x => x.AgenciaId == agencia.AgenciaId && x.ProductoId == producto.ProductoId);

                            cantidad = Convert.ToDecimal(item.Cantidad);
                            if (productoBodega == null)
                            {
                                productoBodega = new ProductoInventario
                                {
                                    AgenciaId = agencia.AgenciaId,
                                    ProductoId = producto.ProductoId,
                                    Cantidad = cantidad
                                };
                                db.ProductoInventarios.Add(productoBodega);
                            }
                            else
                            {
                                productoBodega.Cantidad += cantidad;
                            }


                            producto.Nombre = item.Nombre;
                            producto.Minimo = Convert.ToInt32(item.Min);
                            producto.Maximo = Convert.ToInt32(item.Max);

                            var productoPrecioCosto = db.ProductoPrecioCostos.FirstOrDefault(x => x.ProductoId == producto.ProductoId);

                            costo = Convert.ToDecimal(item.Costo);
                            if (productoPrecioCosto == null)
                            {
                                productoPrecioCosto = new ProductoPrecioCosto
                                {
                                    Producto = producto,
                                    PrecioCosto = costo
                                };
                                db.ProductoPrecioCostos.Add(productoPrecioCosto);
                            }
                            else
                            {
                                productoPrecioCosto.PrecioCosto = costo;
                            }

                            producto.PrecioActual = Convert.ToDecimal(item.PrecioVenta);

                            if (producto.Precios.Any(x => x.PrecioId == 5))
                            {
                                producto.Precios.First(x => x.PrecioId == 5).Valor = Convert.ToDecimal(item.PrecioVenta);
                            }
                            else
                            {
                                producto.Precios.Add(new ProductoPrecio
                                {
                                    PrecioId = 5,
                                    Producto = producto,
                                    Valor = Convert.ToDecimal(item.PrecioVenta)
                                });
                            }

                        }
                        else
                        {
                            // crear

                            producto = db.Productos.FirstOrDefault(x => x.ProductoId == item.Id.ToString() || x.Codigo == item.Codigo);

                            if (producto != null)
                            {
                                error = "Ya existe un producto con id '" + item.Id + "' o con codigo '" + item.Codigo + "'.";
                                if (!errores.Contains(error))
                                    errores.Add(error);

                                continue;
                            }

                            marca = db.Marcas.Where(x => x.Nombre.ToLower().Trim() == item.Marca.ToLower().Trim()).FirstOrDefault();

                            if (marca == null)
                            {
                                var marcaCorrelativo = db.Marcas.Where(x => x.Fecha.Year == hoy.Year && x.Fecha.Month == hoy.Month && x.Fecha.Day == hoy.Day).OrderByDescending(x => x.Correlativo).Select(x => x.Correlativo).FirstOrDefault();

                                marcaCorrelativo = marcaCorrelativo > 0 ? marcaCorrelativo + 1 : 1;

                                var marcaId = long.Parse(string.Format("{0:yyyyMMdd}{1:000}", hoy, marcaCorrelativo));

                                marca = new Marca
                                {
                                    Correlativo = marcaCorrelativo,
                                    MarcaId = marcaId,
                                    Activo = true,
                                    Fecha = DateTime.Now,
                                    Nombre = item.Marca
                                };
                            }

                            DateTime productoFecha = hoy;
                            int productoCorrelativo;
                            string productoId = item.Id.ToString();

                            if (item.Id > 0)
                            {
                                try
                                {
                                    //productoFecha = DateTime.ParseExact(productoId.Substring(0, 8), "yyyyMMdd", CultureInfo.CurrentCulture);
                                    productoCorrelativo = int.Parse(productoId.Substring(8, 3));

                                    //if (productoFecha.Year == hoy.Year && productoFecha.Month == hoy.Month && productoFecha.Day == hoy.Day)
                                    //{
                                        if (correlativo < productoCorrelativo)
                                        {
                                            correlativo = productoCorrelativo;
                                        }
                                    //}

                                }
                                catch (Exception)
                                {
                                    error = "El id " + item.Id + " no es válido";
                                    if (!errores.Contains(error))
                                        errores.Add(error);
                                    continue;
                                }
                            }
                            else
                            {
                                productoFecha = hoy;

                                if (correlativo > 0)
                                {
                                    productoCorrelativo = correlativo;
                                }
                                else
                                {
                                    int correlativoDb = db.Productos.Where(x => x.Fecha.Year == hoy.Year && x.Fecha.Month == hoy.Month && x.Fecha.Day == hoy.Day).OrderByDescending(x => x.Correlativo).Select(x => x.Correlativo).FirstOrDefault();
                                    if (correlativo < correlativoDb)
                                    {
                                        productoCorrelativo = correlativoDb;
                                    }
                                    else
                                    {
                                        productoCorrelativo = correlativo;
                                    }
                                }

                                productoCorrelativo = productoCorrelativo > 0 ? productoCorrelativo + 1 : 1;

                                correlativo = productoCorrelativo;

                                productoId = string.Format("{0:yyyyMMdd}{1:000}", hoy, productoCorrelativo);
                            }

                            producto = new Producto
                            {
                                ProductoId = productoId,
                                Correlativo = productoCorrelativo,
                                Codigo = item.Codigo,
                                Nombre = item.Nombre,
                                Descripcion = item.Nombre,
                                Minimo = Convert.ToInt32(item.Min),
                                Maximo = Convert.ToInt32(item.Max),
                                Activo = true,
                                CategoriaId = 20170114001,
                                Cantidad = Convert.ToDecimal(item.Cantidad),
                                UnidadId = 20170114001,
                                Fecha = productoFecha,
                                Precios = new List<ProductoPrecio> { new ProductoPrecio { PrecioId = 5, Valor = Convert.ToDecimal(item.PrecioVenta) } },
                                Marca = marca,
                                PrecioActual = Convert.ToDecimal(item.PrecioVenta)
                            };

                            db.Productos.Add(producto);

                            costo = Convert.ToDecimal(item.Costo);
                            cantidad = Convert.ToDecimal(item.Cantidad);

                            var productoPrecioCosto = new ProductoPrecioCosto
                            {
                                Producto = producto,
                                PrecioCosto = costo
                            };
                            db.ProductoPrecioCostos.Add(productoPrecioCosto);
                            db.ProductoInventarios.Add(new ProductoInventario
                            {
                                AgenciaId = agencia.AgenciaId,
                                ProductoId = producto.ProductoId,
                                Cantidad = cantidad
                            });
                        }

                        if (commit)
                        {
                            db.RegistrosKardex.Add(new RegistroKardex
                            {
                                FechaHora = DateTime.Now,
                                Fecha = DateTime.Today,
                                ProductoId = producto.ProductoId,
                                ProductoCodigo = producto.Codigo,
                                ProductoNombre = producto.Nombre,
                                ProductoDescripcion = producto.Descripcion,
                                MarcaId = marca.MarcaId,
                                MarcaNombre = marca.Nombre,
                                AgenciaId = agencia.AgenciaId,
                                AgenciaNombre = agencia.Nombre,
                                TipoRegistro = "Ingreso Masivo",
                                IngresoCantidadTienda = cantidad,
                                IngresoCostoTienda = costo
                            });
                        }
                    }

                    try
                    {
                        db.SaveChanges();
                        if (commit)
                        {
                            trx.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        errores.Add(ex.Message);
                    }
                }
            }

            return errores;
        }

        [HttpPost]
        public ActionResult AprobarVerificacion(string id)
        {
            var fileDir = Server.MapPath("~/App_Data/Productos/");
            var valid = ValidarId(id, fileDir);
            if (valid != null) return valid;

            var archivo = Path.Combine(fileDir, id + ".json");

            var fileDir2 = Server.MapPath("~/App_Data/Productos/Verificados");

            if (!Directory.Exists(fileDir2))
            {
                Directory.CreateDirectory(fileDir2);
            }

            System.IO.File.Move(archivo, Path.Combine(fileDir2, id + ".json"));

            return RedirectToAction("Index");
        }

        public ActionResult Verificados()
        {
            var fileDir2 = Server.MapPath("~/App_Data/Productos/Verificados");
            var archivos = Directory.GetFiles(fileDir2, "*.json");

            var modelo = new List<ProductoCargaMasivaArchivo>(archivos.Length);

            foreach (var item in archivos)
            {
                var id = Path.GetFileNameWithoutExtension(item);
                var parts = id.Split('-');
                if (parts.Length != 4) continue;

                try
                {
                    var anio = int.Parse(parts[0]);
                    var mes = int.Parse(parts[1]);
                    var dia = int.Parse(parts[2]);
                    var correlativo = int.Parse(parts[3]);

                    modelo.Add(new ProductoCargaMasivaArchivo
                    {
                        Id = id,
                        Fecha = new DateTime(anio, mes, dia),
                        Correlativo = correlativo
                    });

                }
                catch (Exception)
                {
                    continue;
                }
            }

            return View(modelo.OrderBy(x => x.Fecha).ToList());

        }

        private ActionResult ValidarId(string id, string fileDir)
        {
            if (string.IsNullOrWhiteSpace(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var parts = id.Split('-');

            if (parts.Length != 4) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            try
            {
                var anio = int.Parse(parts[0]);
                var mes = int.Parse(parts[1]);
                var dia = int.Parse(parts[2]);
                var correlativo = int.Parse(parts[3]);
                new DateTime(anio, mes, dia);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var archivo = Path.Combine(fileDir, id + ".json");

            if (!System.IO.File.Exists(archivo))
            {
                return HttpNotFound();
            }

            return null;
        }
    }
}