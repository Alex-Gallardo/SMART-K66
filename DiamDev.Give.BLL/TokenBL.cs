using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiamDev.Give.BLL
{
    public class TokenBL
    {
        #region Variables Globales

            private GiveContext db;

        #endregion

        #region Constructores

            public TokenBL()
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
                    Token TokenActual = db.Set<Token>().Where(x => x.Fecha.Year == DateTime.Today.Year && x.Fecha.Month == DateTime.Today.Month && x.Fecha.Day == DateTime.Today.Day).OrderByDescending(x => x.Correlativo).FirstOrDefault();
                    int Inicial_Id = 1;

                    if (TokenActual != null)
                    {
                        Inicial_Id = TokenActual.Correlativo + 1;
                    }

                    Id = Inicial_Id;
                }
                catch (Exception)
                {}

                return Id;
            }

            private string Agregar(Token entidad)
            {
                string Mensaje = "OK";

                try
                {
                    int Id = Correlativo();

                    if (Id > 0)
                    {
                        long lngTokenId = new Herramienta().Formato_Correlativo(Id);

                        if (lngTokenId > 0)
                        {
                            entidad.TokenId = lngTokenId;
                            entidad.Correlativo = Id;
                            entidad.Fecha = DateTime.Today;

                            int longitud = 7;
                            Guid miGuid = Guid.NewGuid();
                            string token = Convert.ToBase64String(miGuid.ToByteArray());
                            token = token.Replace("=", "").Replace("+", "");

                            //Se asigna el token
                            entidad.TokenValido = token.Substring(0, longitud);
                            entidad.Activo = true;

                            db.Set<Token>().Add(entidad);
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

            public string Guardar(Token entidad)
            {
                string Mensaje = "OK";
               
                if (entidad.TokenId == 0)
                {
                    Mensaje = Agregar(entidad);
                }                    

                return Mensaje;
            }

            public bool Validar(string token) 
            {
                return db.Set<Token>().AsNoTracking().Where(x => x.TokenValido.Equals(token) && x.Activo).Count() > 0;
            }

            public Token ObtenerPorId(long id)
            {
                Token TokenActual = new Token();

                try
                {
                    TokenActual = db.Set<Token>().Where(x => x.TokenId == id).FirstOrDefault();
                }
                catch (Exception)
                {}

                return TokenActual;
            }

            public List<Token> ObtenerListado(DateTime fechaInicial, DateTime fechaFinal)
            {
                List<Token> Tokens = new List<Token>();

                try
                {
                    Tokens = db.Set<Token>().AsNoTracking().Where(x => x.Fecha >= fechaInicial && x.Fecha <= fechaFinal).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.TokenId).Take(500).ToList();
                }
                catch (Exception)
                {}

                return Tokens;
            }            

        #endregion
    }
}
