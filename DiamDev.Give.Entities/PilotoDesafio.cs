using System;
using System.Security.Cryptography;

namespace DiamDev.Give.Entities
{
    [Serializable]
    public sealed class PilotoDesafio
    {
        public string Login { get; private set; }
        public string Codigo { get; private set; }
        public DateTime VenceUtc { get; private set; }
        public int Intentos { get; private set; }
        public bool Consumido { get; private set; }
        public PilotoDesafio(string login, DateTime ahoraUtc)
        {
            Login=login; VenceUtc=ahoraUtc.AddMinutes(5);
            var bytes=new byte[4]; using(var rng=RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            Codigo=(100000+(BitConverter.ToUInt32(bytes,0)%900000)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        public bool Verificar(string codigo, DateTime ahoraUtc)
        {
            if(Consumido || Intentos>=5 || ahoraUtc>=VenceUtc) return false;
            Intentos++;
            if(!string.Equals(Codigo,codigo,StringComparison.Ordinal)) return false;
            Consumido=true; return true;
        }
    }
}
