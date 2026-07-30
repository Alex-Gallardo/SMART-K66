namespace DiamDev.Give.Entities
{
    /// <summary>
    /// Resultado del stored procedure INF_CLIENTES_REC en SAP HANA.
    /// Los campos coinciden con los que llena el lstClientes en el desktop.
    /// </summary>
    public class ClienteHana
    {
        public string CardCode { get; set; }  // Código del cliente
        public string CardName { get; set; }  // Nombre
        public string Address { get; set; }  // Dirección
        public string LicTradNum { get; set; }  // NIT
        public string SlpName { get; set; }  // Nombre del agente
        public string Email { get; set; }  // Correo
        public string Currency { get; set; }  // Moneda (GTQ, USD)
    }
}