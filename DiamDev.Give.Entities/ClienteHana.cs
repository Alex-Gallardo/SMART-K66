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

    /// <summary>Datos maestros de SAP consultados al iniciar una actualización CRM.</summary>
    public class ClienteSapDetalle : ClienteHana
    {
        public int? GroupCode { get; set; }
        public string GroupName { get; set; }
        public string ForeignName { get; set; }
        public string SecondaryName { get; set; }
        public string AlternateNit { get; set; }
        public string SapPaymentMethod { get; set; }
        public string MailAddress { get; set; }
        public string Phone1 { get; set; }
        public string Cellular { get; set; }
        public string ContactPerson { get; set; }
        public string PaymentTerms { get; set; }
        public int? PaymentExtraDays { get; set; }
        public int? PaymentExtraMonths { get; set; }
        public string PaymentDueMonth { get; set; }
        public System.Collections.Generic.List<ClienteSapContacto> Contactos { get; set; } =
            new System.Collections.Generic.List<ClienteSapContacto>();
        public System.Collections.Generic.List<ClienteSapDireccion> Direcciones { get; set; } =
            new System.Collections.Generic.List<ClienteSapDireccion>();
    }

    public class ClienteSapContacto
    {
        public string Nombre { get; set; }
        public string Puesto { get; set; }
        public string Profesion { get; set; }
        public string Telefono { get; set; }
        public string Celular { get; set; }
        public string Correo { get; set; }
    }

    public class ClienteSapDireccion
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Colonia { get; set; }
        public string Ciudad { get; set; }
        public string Municipio { get; set; }
        public string Departamento { get; set; }
        public string Pais { get; set; }
        public string CodigoPostal { get; set; }
    }

    public class ClienteGrupoHana
    {
        public int GroupCode { get; set; }
        public string GroupName { get; set; }
    }
}
