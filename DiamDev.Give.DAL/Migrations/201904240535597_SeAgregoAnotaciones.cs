namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoAnotaciones : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Anotacion",
                c => new
                    {
                        Anotacion_Id = c.Long(nullable: false),
                        Personal_Id = c.Long(nullable: false),
                        Tipo_Id = c.Long(nullable: false),
                        Fecha_Inicial = c.DateTime(nullable: false),
                        Fecha_Final = c.DateTime(nullable: false),
                        Descripcion = c.String(maxLength: 500),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Anotacion_Id)
                .ForeignKey("dbo.Personal", t => t.Personal_Id, cascadeDelete: true)
                .ForeignKey("dbo.Anotacion_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .Index(t => t.Personal_Id)
                .Index(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Anotacion_Tipo",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 200),
                        Descuento = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Anotacion", "Tipo_Id", "dbo.Anotacion_Tipo");
            DropForeignKey("dbo.Anotacion", "Personal_Id", "dbo.Personal");
            DropIndex("dbo.Anotacion", new[] { "Tipo_Id" });
            DropIndex("dbo.Anotacion", new[] { "Personal_Id" });
            DropTable("dbo.Anotacion_Tipo");
            DropTable("dbo.Anotacion");
        }
    }
}
