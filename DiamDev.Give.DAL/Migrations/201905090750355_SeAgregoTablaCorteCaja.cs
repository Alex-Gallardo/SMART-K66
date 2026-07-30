namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaCorteCaja : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Corte_Caja",
                c => new
                    {
                        Corte_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Cajero_Id = c.Long(nullable: false),
                        Responsable_Id = c.Long(nullable: false),
                        Opero_Id = c.Long(nullable: false),
                        Monto = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Fecha_Hora = c.DateTime(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Corte_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Opero_Id, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Opero_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Corte_Caja", "Opero_Id", "dbo.Usuario");
            DropForeignKey("dbo.Corte_Caja", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Corte_Caja", new[] { "Opero_Id" });
            DropIndex("dbo.Corte_Caja", new[] { "Agencia_Id" });
            DropTable("dbo.Corte_Caja");
        }
    }
}
