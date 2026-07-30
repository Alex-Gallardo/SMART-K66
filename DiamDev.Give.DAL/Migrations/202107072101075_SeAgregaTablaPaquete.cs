namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPaquete : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Paquete",
                c => new
                    {
                        Paquete_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Descripcion = c.String(),
                        Cantidad_DTE = c.Int(nullable: false),
                        Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Vigencia = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Paquete_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Paquete");
        }
    }
}
