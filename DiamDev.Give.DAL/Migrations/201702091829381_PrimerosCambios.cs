namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PrimerosCambios : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Personal_Horario",
                c => new
                    {
                        Personal_Id = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Entrada = c.Time(nullable: false, precision: 7),
                        Salida = c.Time(precision: 7),
                    })
                .PrimaryKey(t => new { t.Personal_Id, t.Fecha })
                .ForeignKey("dbo.Personal", t => t.Personal_Id, cascadeDelete: true)
                .Index(t => t.Personal_Id);
            
            CreateTable(
                "dbo.Personal",
                c => new
                    {
                        Personal_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Direccion = c.String(nullable: false, maxLength: 500),
                        No_Telefono = c.String(maxLength: 20),
                        No_Telefono_Alterno = c.String(maxLength: 20),
                        No_Celular_Principal = c.String(maxLength: 20),
                        No_Celular_Alterno = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        Activo = c.Boolean(nullable: false),
                        Huella = c.Binary(),
                        Template_Bytes = c.Binary(),
                        Template_Size = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Personal_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Personal_Horario", "Personal_Id", "dbo.Personal");
            DropIndex("dbo.Personal_Horario", new[] { "Personal_Id" });
            DropTable("dbo.Personal");
            DropTable("dbo.Personal_Horario");
        }
    }
}
