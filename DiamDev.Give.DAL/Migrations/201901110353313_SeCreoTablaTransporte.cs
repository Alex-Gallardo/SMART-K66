namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeCreoTablaTransporte : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Transporte",
                c => new
                    {
                        Transporte_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 500),
                        Descripcion = c.String(maxLength: 1000),
                        Descripcion_Empaque = c.String(maxLength: 1000),
                        Sitio_Web = c.String(maxLength: 200),
                        Contacto = c.String(maxLength: 500),
                        No_Telefono = c.String(maxLength: 20),
                        Nit = c.String(maxLength: 20),
                        Nombre_Pago = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Transporte_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Transporte");
        }
    }
}
