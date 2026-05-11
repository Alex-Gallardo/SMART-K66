namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaLaboratorio : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Laboratorio",
                c => new
                    {
                        Laboratorio_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Producto_Base_Id = c.String(nullable: false, maxLength: 50),
                        Cantidad_Base = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Producto_Destino_Id = c.String(maxLength: 50),
                        Cantidad_Destino = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Usr_Creo = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Laboratorio_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Base_Id)
                .ForeignKey("dbo.Producto", t => t.Producto_Destino_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Producto_Base_Id)
                .Index(t => t.Producto_Destino_Id)
                .Index(t => t.Usr_Creo);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Laboratorio", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Laboratorio", "Producto_Destino_Id", "dbo.Producto");
            DropForeignKey("dbo.Laboratorio", "Producto_Base_Id", "dbo.Producto");
            DropForeignKey("dbo.Laboratorio", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Laboratorio", new[] { "Usr_Creo" });
            DropIndex("dbo.Laboratorio", new[] { "Producto_Destino_Id" });
            DropIndex("dbo.Laboratorio", new[] { "Producto_Base_Id" });
            DropIndex("dbo.Laboratorio", new[] { "Agencia_Id" });
            DropTable("dbo.Laboratorio");
        }
    }
}
