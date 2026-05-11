namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeActualizoModeloPersonal : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Personal", "Fecha_Ingreso", c => c.DateTime());
            AddColumn("dbo.Personal", "Fecha_Egreso", c => c.DateTime());
            AddColumn("dbo.Personal", "Banco_Id", c => c.Long());
            AddColumn("dbo.Personal", "Planilla", c => c.String(maxLength: 100));
            AddColumn("dbo.Personal", "Contrato", c => c.String(maxLength: 50));
            AddColumn("dbo.Personal", "Sueldo", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Personal", "Bonificacion", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Personal", "IGSS", c => c.Boolean(nullable: false));
            AddColumn("dbo.Personal", "Motivo_Egreso", c => c.String(maxLength: 500));
            CreateIndex("dbo.Personal", "Banco_Id");
            AddForeignKey("dbo.Personal", "Banco_Id", "dbo.Banco", "Banco_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Personal", "Banco_Id", "dbo.Banco");
            DropIndex("dbo.Personal", new[] { "Banco_Id" });
            DropColumn("dbo.Personal", "Motivo_Egreso");
            DropColumn("dbo.Personal", "IGSS");
            DropColumn("dbo.Personal", "Bonificacion");
            DropColumn("dbo.Personal", "Sueldo");
            DropColumn("dbo.Personal", "Contrato");
            DropColumn("dbo.Personal", "Planilla");
            DropColumn("dbo.Personal", "Banco_Id");
            DropColumn("dbo.Personal", "Fecha_Egreso");
            DropColumn("dbo.Personal", "Fecha_Ingreso");
        }
    }
}
