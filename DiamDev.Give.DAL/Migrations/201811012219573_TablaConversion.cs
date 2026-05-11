namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TablaConversion : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Unidad_Conversion",
                c => new
                    {
                        Conversion_Id = c.Long(nullable: false),
                        Operacion_Id = c.Int(nullable: false),
                        Unidad_Base_Id = c.Long(nullable: false),
                        Cantidad_Base = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Unidad_Destino_Id = c.Long(nullable: false),
                        Cantidad_Destino = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Conversion_Id)
                .ForeignKey("dbo.Unidad_Operacion", t => t.Operacion_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Base_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Destino_Id, cascadeDelete: true)
                .Index(t => t.Operacion_Id)
                .Index(t => t.Unidad_Base_Id)
                .Index(t => t.Unidad_Destino_Id);
            
            CreateTable(
                "dbo.Unidad_Operacion",
                c => new
                    {
                        Operacion_Id = c.Int(nullable: false),
                        Nombre = c.String(),
                    })
                .PrimaryKey(t => t.Operacion_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Unidad_Conversion", "Unidad_Destino_Id", "dbo.Unidad");
            DropForeignKey("dbo.Unidad_Conversion", "Unidad_Base_Id", "dbo.Unidad");
            DropForeignKey("dbo.Unidad_Conversion", "Operacion_Id", "dbo.Unidad_Operacion");
            DropIndex("dbo.Unidad_Conversion", new[] { "Unidad_Destino_Id" });
            DropIndex("dbo.Unidad_Conversion", new[] { "Unidad_Base_Id" });
            DropIndex("dbo.Unidad_Conversion", new[] { "Operacion_Id" });
            DropTable("dbo.Unidad_Operacion");
            DropTable("dbo.Unidad_Conversion");
        }
    }
}
