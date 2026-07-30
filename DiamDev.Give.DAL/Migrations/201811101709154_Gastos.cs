namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Gastos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Gasto_Fotografia",
                c => new
                    {
                        Fotografia_Id = c.Int(nullable: false),
                        Gasto_Id = c.Long(nullable: false),
                        Nombre = c.String(maxLength: 200),
                        ContentType = c.String(maxLength: 150),
                        Length = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => new { t.Fotografia_Id, t.Gasto_Id })
                .ForeignKey("dbo.Gasto", t => t.Gasto_Id, cascadeDelete: true)
                .Index(t => t.Gasto_Id);
            
            CreateTable(
                "dbo.Gasto",
                c => new
                    {
                        Gasto_Id = c.Long(nullable: false),
                        Documento = c.String(maxLength: 150),
                        Concepto = c.String(),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha_Factura = c.DateTime(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Gasto_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Usr_Creo);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Gasto", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Gasto_Fotografia", "Gasto_Id", "dbo.Gasto");
            DropIndex("dbo.Gasto", new[] { "Usr_Creo" });
            DropIndex("dbo.Gasto_Fotografia", new[] { "Gasto_Id" });
            DropTable("dbo.Gasto");
            DropTable("dbo.Gasto_Fotografia");
        }
    }
}
