namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaEmpresa : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Empresa",
                c => new
                    {
                        Empresa_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Nombre_Comercial = c.String(),
                        Nombre_Contacto = c.String(),
                        Telefono_Contacto = c.String(),
                        Telefono_Contacto_2 = c.String(),
                        Correo_Contacto = c.String(),
                        Any_Desk_Id = c.Int(),
                        Nit_Emisor_DIGIFACT = c.String(),
                        Nombre_Comercial_DIGIFACT = c.String(),
                        Nombre_Emisor_DIGIFACT = c.String(),
                        Direccion_Emisor_DIGIFACT = c.String(),
                        Codigo_Postal_Emisor_DIGIFACT = c.String(),
                        Departamento_Emisor_DIGIFACT = c.String(),
                        Municipio_Emisor_DIGIFACT = c.String(),
                        Pais_Emisor_DIGIFACT = c.String(),
                        Codigo_Escenario_DIGIFACT = c.String(),
                        Tipo_Frase_DIGIFACT = c.String(),
                        Afiliacion_Iva_DIGIFACT = c.String(),
                        Usuario_DIGIFACT = c.String(),
                        Password_DIGIFACT = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Empresa_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Empresa");
        }
    }
}
