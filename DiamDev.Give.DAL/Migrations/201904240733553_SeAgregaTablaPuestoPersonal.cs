namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaTablaPuestoPersonal : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Puesto",
                c => new
                    {
                        Puesto_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 200),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Puesto_Id);
            
            AddColumn("dbo.Personal", "Puesto_Id", c => c.Long(nullable: false));
            AddColumn("dbo.Personal", "Fecha_Nacimiento", c => c.DateTime());
            AddColumn("dbo.Personal", "DPI", c => c.String(maxLength: 20));
            AddColumn("dbo.Personal", "Nit", c => c.String(maxLength: 20));
            AddColumn("dbo.Personal", "Licencia_Vehiculo", c => c.String(maxLength: 50));
            AddColumn("dbo.Personal", "Licencia_Moto", c => c.String(maxLength: 50));
            AddColumn("dbo.Personal", "No_Afiliacion_IGSS", c => c.String(maxLength: 50));
            CreateIndex("dbo.Personal", "Puesto_Id");
            AddForeignKey("dbo.Personal", "Puesto_Id", "dbo.Puesto", "Puesto_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Personal", "Puesto_Id", "dbo.Puesto");
            DropIndex("dbo.Personal", new[] { "Puesto_Id" });
            DropColumn("dbo.Personal", "No_Afiliacion_IGSS");
            DropColumn("dbo.Personal", "Licencia_Moto");
            DropColumn("dbo.Personal", "Licencia_Vehiculo");
            DropColumn("dbo.Personal", "Nit");
            DropColumn("dbo.Personal", "DPI");
            DropColumn("dbo.Personal", "Fecha_Nacimiento");
            DropColumn("dbo.Personal", "Puesto_Id");
            DropTable("dbo.Puesto");
        }
    }
}
