namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaProductoAlertaK66 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Producto_Alerta_K66",
                c => new
                    {
                        Alerta_Id = c.Long(nullable: false),
                        Nombre = c.String(),
                        Mensaje = c.String(),
                        Rango_Inicial = c.Int(nullable: false),
                        Rango_Final = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Alerta_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Producto_Alerta_K66");
        }
    }
}
