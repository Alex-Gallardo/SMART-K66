namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablasNomina : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Nomina_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Nomina_Id = c.Long(nullable: false),
                        Personal_Id = c.Long(nullable: false),
                        Puesto = c.String(maxLength: 200),
                        Dias = c.Int(nullable: false),
                        Sueldo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Bonificacion = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Otros_Ingresos = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IGSS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Otros_Descuentos = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Nomina_Id })
                .ForeignKey("dbo.Nomina", t => t.Nomina_Id, cascadeDelete: true)
                .ForeignKey("dbo.Personal", t => t.Personal_Id, cascadeDelete: true)
                .Index(t => t.Nomina_Id)
                .Index(t => t.Personal_Id);
            
            CreateTable(
                "dbo.Nomina",
                c => new
                    {
                        Nomina_Id = c.Long(nullable: false),
                        Tipo_Id = c.Int(nullable: false),
                        Fecha_Inicial = c.DateTime(nullable: false),
                        Fecha_Final = c.DateTime(nullable: false),
                        Descripcion = c.String(),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Nomina_Id)
                .ForeignKey("dbo.Nomina_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .Index(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Nomina_Tipo",
                c => new
                    {
                        Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(maxLength: 150),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Nomina_Detalle", "Personal_Id", "dbo.Personal");
            DropForeignKey("dbo.Nomina", "Tipo_Id", "dbo.Nomina_Tipo");
            DropForeignKey("dbo.Nomina_Detalle", "Nomina_Id", "dbo.Nomina");
            DropIndex("dbo.Nomina", new[] { "Tipo_Id" });
            DropIndex("dbo.Nomina_Detalle", new[] { "Personal_Id" });
            DropIndex("dbo.Nomina_Detalle", new[] { "Nomina_Id" });
            DropTable("dbo.Nomina_Tipo");
            DropTable("dbo.Nomina");
            DropTable("dbo.Nomina_Detalle");
        }
    }
}
