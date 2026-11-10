using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DiamDev.Give.DAL;

namespace DiamDev.Give.UI.Controllers
{
    /// <summary>Informe PDF de solo lectura. Cada página se dibuja a 144 ppp y se incorpora al PDF.</summary>
    internal static class GerenciaComercialAvancesPdf
    {
        private sealed class Agente
        {
            public string Codigo, Nombre, Frente;
            public decimal[] Valores;
        }

        private const int Ancho = 1680;
        private const int Alto = 1188;
        private static readonly Color Azul = Color.FromArgb(21, 55, 83);
        private static readonly Color Texto = Color.FromArgb(39, 66, 88);
        private static readonly Color Muted = Color.FromArgb(103, 126, 144);
        private static readonly Color Verde = Color.FromArgb(31, 137, 104);
        private static readonly Color Rojo = Color.FromArgb(189, 77, 82);
        private static readonly Color Fondo = Color.FromArgb(247, 250, 252);
        private static readonly Color Borde = Color.FromArgb(221, 232, 239);
        private static readonly Color[] Colores = { Color.FromArgb(23, 111, 165), Color.FromArgb(231, 75, 89), Color.FromArgb(25, 145, 126), Color.FromArgb(140, 105, 196), Color.FromArgb(210, 135, 38), Color.FromArgb(58, 121, 185), Color.FromArgb(176, 74, 131), Color.FromArgb(100, 155, 53), Color.FromArgb(189, 104, 61), Color.FromArgb(78, 112, 166), Color.FromArgb(204, 66, 117), Color.FromArgb(65, 124, 113) };
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-GT");

        private static string Dinero(decimal valor, string moneda) { return moneda + " " + valor.ToString("N2", Cultura); }
        private static string Porcentaje(decimal actual, decimal baseValor) { return baseValor == 0 ? "Sin base" : ((actual - baseValor) / Math.Abs(baseValor)).ToString("+0.0%;-0.0%;0.0%", Cultura); }
        private static string Rango(GerenciaComercialDA.PeriodoAvance p) { return p.Desde.ToString("dd/MM/yyyy") + " al " + p.Hasta.ToString("dd/MM/yyyy"); }

        internal static byte[] Crear(string empresa, string moneda, IList<GerenciaComercialDA.PeriodoAvance> periodos, DataTable ventas, DataTable unidades)
        {
            if (periodos == null || periodos.Count < 2 || periodos.Count > 12 || ventas == null || unidades == null) throw new ArgumentException("Datos de avances inválidos.");
            var mapa = new Dictionary<string, Agente>(StringComparer.Ordinal);
            foreach (DataRow fila in ventas.Rows)
            {
                string codigo = Convert.ToString(fila["Codigo"]);
                Agente agente;
                if (!mapa.TryGetValue(codigo, out agente))
                {
                    agente = new Agente { Codigo = codigo, Nombre = Convert.ToString(fila["Nombre"]), Frente = ventas.Columns.Contains("Frente") ? Convert.ToString(fila["Frente"]) : "Agente comercial", Valores = new decimal[periodos.Count] };
                    mapa.Add(codigo, agente);
                }
                int indice = Convert.ToInt32(fila["Periodo"]);
                if (indice >= 0 && indice < periodos.Count) agente.Valores[indice] = fila["Neto"] == DBNull.Value ? 0 : Convert.ToDecimal(fila["Neto"]);
            }
            var agentes = mapa.Values.OrderByDescending(a => a.Valores[periodos.Count - 1]).ThenBy(a => a.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
            var totales = new decimal[periodos.Count];
            foreach (var agente in agentes) for (int i = 0; i < totales.Length; i++) totales[i] += agente.Valores[i];
            var paginas = new List<byte[]>();
            var grupos = new List<List<int>>();
            // La primera gráfica compara los períodos más recientes; las siguientes completan el histórico.
            for (int fin = periodos.Count; fin > 0; fin -= 4) grupos.Add(Enumerable.Range(Math.Max(0, fin - 4), fin - Math.Max(0, fin - 4)).ToList());
            int numero = 1;
            foreach (var grupo in grupos)
            {
                int pagina = numero++;
                paginas.Add(Pagina(g => DibujarGrafica(g, empresa, moneda, periodos, agentes, totales, grupo, pagina, grupo == grupos[0])));
            }
            int paginaLectura = numero++;
            paginas.Add(Pagina(g => DibujarLectura(g, empresa, moneda, periodos, agentes, totales, unidades, paginaLectura)));
            int porPagina = periodos.Count > 6 ? 5 : 7;
            int paginasAgentes = Math.Max(1, (agentes.Count + porPagina - 1) / porPagina);
            for (int i = 0; i < paginasAgentes; i++)
            {
                int pagina = numero++, indice = i;
                paginas.Add(Pagina(g => DibujarPanorama(g, empresa, moneda, periodos, agentes.Skip(indice * porPagina).Take(porPagina).ToList(), totales, pagina, indice + 1, paginasAgentes)));
            }
            return Empaquetar(paginas);
        }

        private static byte[] Pagina(Action<Graphics> dibujar)
        {
            using (var imagen = new Bitmap(Ancho, Alto))
            using (var g = Graphics.FromImage(imagen))
            using (var salida = new MemoryStream())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.White);
                dibujar(g);
                var codec = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
                using (var parametros = new EncoderParameters(1))
                {
                    parametros.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 91L);
                    imagen.Save(salida, codec, parametros);
                }
                return salida.ToArray();
            }
        }

        private static void Caja(Graphics g, float x, float y, float ancho, float alto, Color fondo, Color borde, float radio)
        {
            using (var ruta = new GraphicsPath())
            {
                float d = radio * 2;
                ruta.AddArc(x, y, d, d, 180, 90); ruta.AddArc(x + ancho - d, y, d, d, 270, 90);
                ruta.AddArc(x + ancho - d, y + alto - d, d, d, 0, 90); ruta.AddArc(x, y + alto - d, d, d, 90, 90); ruta.CloseFigure();
                using (var relleno = new SolidBrush(fondo)) g.FillPath(relleno, ruta);
                if (borde != Color.Empty) using (var pluma = new Pen(borde, 1.5f)) g.DrawPath(pluma, ruta);
            }
        }

        private static void TextoEn(Graphics g, string texto, float x, float y, float ancho, float alto, int tamano, Color color, bool negrita = false)
        {
            using (var fuente = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
            using (var pincel = new SolidBrush(color))
            using (var formato = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(texto ?? "", fuente, pincel, new RectangleF(x, y, ancho, alto), formato);
        }

        private static void Encabezado(Graphics g, string empresa, string moneda, string titulo, int pagina)
        {
            using (var fondo = new SolidBrush(Azul)) g.FillRectangle(fondo, 0, 0, Ancho, 132);
            TextoEn(g, "GRUPO K66  /  GERENCIA COMERCIAL", 54, 26, 1100, 25, 17, Color.FromArgb(163, 213, 232), true);
            TextoEn(g, empresa + "  ·  " + titulo, 54, 59, 1350, 55, 31, Color.White, true);
            TextoEn(g, moneda + " · " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 1290, 32, 325, 35, 17, Color.White);
            using (var pluma = new Pen(Borde)) g.DrawLine(pluma, 54, 1146, 1626, 1146);
            TextoEn(g, "Fuente: SAP HANA · ventas netas sin impuesto · facturas menos notas de crédito · fecha de documento", 54, 1154, 1300, 23, 15, Muted);
            TextoEn(g, "Página " + pagina, 1510, 1154, 120, 23, 15, Muted);
        }

        private static void Indicador(Graphics g, float x, float y, float ancho, string titulo, string valor, string detalle, Color acento)
        {
            Caja(g, x, y, ancho, 132, Fondo, Borde, 14);
            using (var pincel = new SolidBrush(acento)) g.FillRectangle(pincel, x + 16, y + 17, 5, 96);
            TextoEn(g, titulo.ToUpperInvariant(), x + 34, y + 17, ancho - 49, 23, 15, Muted, true);
            TextoEn(g, valor, x + 34, y + 47, ancho - 49, 47, 29, acento, true);
            TextoEn(g, detalle, x + 34, y + 101, ancho - 49, 22, 14, Muted);
        }

        private static void DibujarGrafica(Graphics g, string empresa, string moneda, IList<GerenciaComercialDA.PeriodoAvance> periodos, IList<Agente> agentes, decimal[] totales, IList<int> grupo, int pagina, bool resumen)
        {
            Encabezado(g, empresa, moneda, resumen ? "Resumen y avances de ventas" : "Avances de ventas · períodos anteriores", pagina);
            if (resumen)
            {
                int ultimo = periodos.Count - 1, anterior = ultimo - 1;
                TextoEn(g, "COMPARACIÓN PRINCIPAL", 54, 154, 500, 27, 17, Color.FromArgb(38, 121, 157), true);
                Indicador(g, 54, 190, 377, "Período anterior", Dinero(totales[anterior], moneda), Rango(periodos[anterior]), Azul);
                Indicador(g, 451, 190, 377, "Último período", Dinero(totales[ultimo], moneda), Rango(periodos[ultimo]), Color.FromArgb(23, 111, 165));
                Indicador(g, 848, 190, 377, "Variación", Porcentaje(totales[ultimo], totales[anterior]), "Último frente al anterior", totales[ultimo] >= totales[anterior] ? Verde : Rojo);
                Indicador(g, 1245, 190, 381, "Cambio absoluto", Dinero(totales[ultimo] - totales[anterior], moneda), "Facturas menos notas de crédito", totales[ultimo] >= totales[anterior] ? Verde : Rojo);
            }
            int graficaY = resumen ? 353 : 165;
            Caja(g, 54, graficaY, 1572, resumen ? 770 : 958, Color.White, Borde, 14);
            TextoEn(g, "VENTAS NETAS POR AGENTE", 77, graficaY + 20, 650, 28, 20, Azul, true);
            TextoEn(g, "Primeros 12 agentes por venta del último período · importes en " + moneda, 77, graficaY + 49, 1300, 24, 15, Muted);
            int leyendaY = graficaY + 83;
            for (int j = 0; j < grupo.Count; j++)
            {
                int indice = grupo[j];
                using (var pincel = new SolidBrush(Colores[indice])) g.FillRectangle(pincel, 80 + j * 375, leyendaY + 3, 17, 17);
                TextoEn(g, Rango(periodos[indice]), 106 + j * 375, leyendaY, 345, 27, 16, Texto, true);
            }
            var visibles = agentes.Take(12).ToList();
            if (visibles.Count == 0) { TextoEn(g, "No hay ventas de agentes para los períodos seleccionados.", 88, graficaY + 200, 1200, 60, 22, Muted); return; }
            float inicioY = leyendaY + 43, filaAlto = Math.Min(55f, (graficaY + (resumen ? 743 : 925) - inicioY) / visibles.Count);
            float x0 = 455, xFin = 1550, ancho = xFin - x0;
            decimal minimo = 0, maximo = 0;
            foreach (var agente in visibles) foreach (int indice in grupo) { minimo = Math.Min(minimo, agente.Valores[indice]); maximo = Math.Max(maximo, agente.Valores[indice]); }
            if (maximo == minimo) maximo = minimo + 1;
            Func<decimal, float> eje = v => x0 + (float)((v - minimo) / (maximo - minimo)) * ancho;
            for (int r = 0; r < visibles.Count; r++) if (r % 2 == 0) using (var pincel = new SolidBrush(Fondo)) g.FillRectangle(pincel, 70, inicioY + r * filaAlto, 1520, filaAlto);
            using (var pluma = new Pen(Borde))
            {
                for (int t = 0; t <= 4; t++) { float x = x0 + ancho * t / 4; g.DrawLine(pluma, x, inicioY, x, inicioY + visibles.Count * filaAlto); TextoEn(g, Dinero(minimo + (maximo - minimo) * t / 4, moneda), x - 70, inicioY - 23, 140, 21, 13, Muted); }
            }
            for (int r = 0; r < visibles.Count; r++)
            {
                float y = inicioY + r * filaAlto;
                TextoEn(g, visibles[r].Nombre, 80, y + 12, 345, 29, 17, Texto, true);
                float h = Math.Min(10f, (filaAlto - 9) / grupo.Count - 1), totalH = grupo.Count * (h + 1), arriba = y + (filaAlto - totalH) / 2;
                for (int j = 0; j < grupo.Count; j++)
                {
                    decimal valor = visibles[r].Valores[grupo[j]]; float x = eje(valor), cero = eje(0);
                    using (var pincel = new SolidBrush(Colores[grupo[j]])) g.FillRectangle(pincel, Math.Min(cero, x), arriba + j * (h + 1), Math.Max(2, Math.Abs(x - cero)), h);
                }
            }
            TextoEn(g, "Se muestran los 12 agentes principales. El panorama del informe incluye todos.", 80, graficaY + (resumen ? 728 : 908), 1300, 24, 14, Muted);
        }

        private static void Bloque(Graphics g, float x, float y, float ancho, string titulo, string contenido, Color acento)
        {
            Caja(g, x, y, ancho, 112, Color.White, Borde, 10);
            using (var pincel = new SolidBrush(acento)) g.FillRectangle(pincel, x, y + 13, 5, 86);
            TextoEn(g, titulo, x + 18, y + 13, ancho - 36, 28, 17, acento, true);
            TextoEn(g, contenido, x + 18, y + 42, ancho - 36, 64, 16, Texto);
        }

        private static void DibujarLectura(Graphics g, string empresa, string moneda, IList<GerenciaComercialDA.PeriodoAvance> periodos, IList<Agente> agentes, decimal[] totales, DataTable unidades, int pagina)
        {
            Encabezado(g, empresa, moneda, "Lectura ejecutiva y prioridades", pagina);
            int ultimo = periodos.Count - 1, anterior = ultimo - 1;
            var movidos = agentes.Where(a => a.Valores[anterior] != 0 || a.Valores[ultimo] != 0).ToList();
            var suben = movidos.Where(a => a.Valores[ultimo] > a.Valores[anterior]).ToList();
            var bajan = movidos.Where(a => a.Valores[ultimo] < a.Valores[anterior]).ToList();
            var mayorAlza = suben.OrderByDescending(a => a.Valores[ultimo] - a.Valores[anterior]).FirstOrDefault();
            var mayorCaida = bajan.OrderBy(a => a.Valores[ultimo] - a.Valores[anterior]).FirstOrDefault();
            decimal total = totales[ultimo], anteriorTotal = totales[anterior], top3 = agentes.Take(3).Sum(a => a.Valores[ultimo]);
            TextoEn(g, "LECTURA EJECUTIVA", 54, 160, 650, 30, 21, Azul, true);
            Bloque(g, 54, 207, 762, "Resultado general", "Las ventas netas cambiaron " + Porcentaje(total, anteriorTotal) + " (" + Dinero(total - anteriorTotal, moneda) + ") entre los últimos dos períodos.", total >= anteriorTotal ? Verde : Rojo);
            Bloque(g, 844, 207, 782, "Actividad comercial", suben.Count + " de " + movidos.Count + " agentes con movimiento crecieron; " + bajan.Count + " retrocedieron.", Azul);
            Bloque(g, 54, 332, 762, "Concentración", total > 0 ? "Los tres principales agentes representan " + (top3 / total).ToString("P1", Cultura) + " de la venta del último período." : "Sin base positiva para calcular participación.", Azul);
            Bloque(g, 844, 332, 782, "Comparabilidad", (periodos[ultimo].Hasta - periodos[ultimo].Desde).Days == (periodos[anterior].Hasta - periodos[anterior].Desde).Days ? "Los últimos dos períodos tienen la misma duración." : "Los períodos tienen distinta duración; revise el corte antes de interpretar el crecimiento.", Color.FromArgb(183, 119, 37));
            TextoEn(g, "ALERTAS Y PRIORIDADES", 54, 490, 700, 30, 21, Azul, true);
            Bloque(g, 54, 537, 762, "Mayor alza absoluta", mayorAlza == null ? "No hay agentes con crecimiento en esta comparación." : mayorAlza.Nombre + ": " + Dinero(mayorAlza.Valores[ultimo] - mayorAlza.Valores[anterior], moneda) + ". Revisar cuentas que impulsaron el avance.", Verde);
            Bloque(g, 844, 537, 782, "Mayor caída absoluta", mayorCaida == null ? "No hay agentes con retroceso en esta comparación." : mayorCaida.Nombre + ": " + Dinero(mayorCaida.Valores[ultimo] - mayorCaida.Valores[anterior], moneda) + ". Revisar clientes y pedidos.", Rojo);
            Bloque(g, 54, 662, 762, "Concentración y riesgo", total > 0 && top3 / total >= .6m ? "Concentración superior al 60 %: dar seguimiento a las cuentas principales." : "Sin concentración superior al umbral del 60 %.", Color.FromArgb(183, 119, 37));
            Bloque(g, 844, 662, 782, "Prioridad inmediata", bajan.Count > 0 ? "Revisar " + string.Join(", ", bajan.OrderBy(a => a.Valores[ultimo] - a.Valores[anterior]).Take(3).Select(a => a.Nombre)) + " y definir acciones de recuperación." : "Mantener seguimiento de los agentes y replicar las oportunidades de crecimiento.", bajan.Count > 0 ? Rojo : Verde);
            TextoEn(g, "UNIDADES POR MEDIDA", 54, 816, 650, 30, 21, Azul, true);
            var medidas = unidades.AsEnumerable().Where(r => Convert.ToInt32(r["Periodo"]) == ultimo).GroupBy(r => Convert.ToString(r["Unidad"])).Select(grupo => grupo.Key + ": " + grupo.Sum(r => Convert.ToDecimal(r["Cantidad"] == DBNull.Value ? 0 : r["Cantidad"])).ToString("N1", Cultura)).Take(12).ToList();
            Caja(g, 54, 861, 1572, 205, Fondo, Borde, 12);
            TextoEn(g, medidas.Count == 0 ? "No se registran cantidades para el último período." : string.Join("   ·   ", medidas), 75, 885, 1520, 132, 19, Texto);
            TextoEn(g, "Las medidas se presentan por separado; no se suman unidades distintas.", 75, 1027, 1470, 25, 15, Muted);
        }

        private static void DibujarPanorama(Graphics g, string empresa, string moneda, IList<GerenciaComercialDA.PeriodoAvance> periodos, IList<Agente> agentes, decimal[] totales, int pagina, int parte, int totalPartes)
        {
            Encabezado(g, empresa, moneda, "Panorama por agente · " + parte + "/" + totalPartes, pagina);
            TextoEn(g, "RESULTADOS POR AGENTE", 54, 157, 700, 32, 21, Azul, true);
            TextoEn(g, "Importes por período en el orden seleccionado · variación respecto al período anterior", 54, 192, 1400, 25, 15, Muted);
            float y = 237, altoFila = periodos.Count > 6 ? 165 : 125;
            int ultimo = periodos.Count - 1, anterior = ultimo - 1;
            foreach (var agente in agentes)
            {
                Caja(g, 54, y, 1572, altoFila - 9, Fondo, Borde, 8);
                TextoEn(g, agente.Nombre + "  ·  " + agente.Codigo, 70, y + 7, 600, 23, 17, Azul, true);
                TextoEn(g, agente.Frente, 70, y + 30, 500, 20, 13, Muted);
                TextoEn(g, "Último: " + Dinero(agente.Valores[ultimo], moneda), 680, y + 8, 380, 22, 16, Texto, true);
                TextoEn(g, "Var.: " + Porcentaje(agente.Valores[ultimo], agente.Valores[anterior]), 1090, y + 8, 250, 22, 16, agente.Valores[ultimo] >= agente.Valores[anterior] ? Verde : Rojo, true);
                TextoEn(g, "Part.: " + (totales[ultimo] == 0 ? "—" : (agente.Valores[ultimo] / totales[ultimo]).ToString("P1", Cultura)), 1370, y + 8, 220, 22, 16, Texto);
                var valores = periodos.Select((p, i) => p.Desde.ToString("dd/MM/yy") + "–" + p.Hasta.ToString("dd/MM/yy") + ": " + Dinero(agente.Valores[i], moneda)).ToList();
                var lineas = new List<string>();
                for (int i = 0; i < valores.Count; i += 4) lineas.Add(string.Join("   ·   ", valores.Skip(i).Take(4)));
                TextoEn(g, string.Join("\n", lineas), 70, y + 62, 1510, altoFila - 70, 15, Texto);
                y += altoFila;
            }
            if (agentes.Count == 0) TextoEn(g, "Sin agentes para los períodos seleccionados.", 70, 260, 1100, 45, 23, Muted);
        }

        private static byte[] Empaquetar(IList<byte[]> imagenes)
        {
            using (var salida = new MemoryStream())
            {
                var offsets = new List<long> { 0 };
                Action<string> escribir = texto => { var bytes = Encoding.ASCII.GetBytes(texto); salida.Write(bytes, 0, bytes.Length); };
                escribir("%PDF-1.4\n");
                int totalObjetos = 2 + imagenes.Count * 3;
                Action<int, Action> objeto = (id, contenido) => { while (offsets.Count <= id) offsets.Add(0); offsets[id] = salida.Position; escribir(id + " 0 obj\n"); contenido(); escribir("\nendobj\n"); };
                objeto(1, () => escribir("<< /Type /Catalog /Pages 2 0 R >>"));
                objeto(2, () => escribir("<< /Type /Pages /Count " + imagenes.Count + " /Kids [" + string.Join(" ", Enumerable.Range(0, imagenes.Count).Select(i => (5 + i * 3) + " 0 R")) + "] >>"));
                for (int i = 0; i < imagenes.Count; i++)
                {
                    int imagenId = 3 + i * 3, contenidoId = imagenId + 1, paginaId = imagenId + 2;
                    byte[] jpeg = imagenes[i];
                    objeto(imagenId, () => { escribir("<< /Type /XObject /Subtype /Image /Width " + Ancho + " /Height " + Alto + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + jpeg.Length + " >>\nstream\n"); salida.Write(jpeg, 0, jpeg.Length); escribir("\nendstream"); });
                    byte[] comandos = Encoding.ASCII.GetBytes("q 842 0 0 595 0 0 cm /Im1 Do Q\n");
                    objeto(contenidoId, () => { escribir("<< /Length " + comandos.Length + " >>\nstream\n"); salida.Write(comandos, 0, comandos.Length); escribir("endstream"); });
                    objeto(paginaId, () => escribir("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /XObject << /Im1 " + imagenId + " 0 R >> >> /Contents " + contenidoId + " 0 R >>"));
                }
                long xref = salida.Position;
                escribir("xref\n0 " + (totalObjetos + 1) + "\n0000000000 65535 f \n");
                for (int id = 1; id <= totalObjetos; id++) escribir(offsets[id].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
                escribir("trailer\n<< /Size " + (totalObjetos + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
                return salida.ToArray();
            }
        }
    }
}
