namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoReponsableTablaPaqueteEmpresa : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Paquete_Empresa", "Responsable_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.Paquete_Empresa", "Responsable_Id");
            AddForeignKey("dbo.Paquete_Empresa", "Responsable_Id", "dbo.Usuario", "Usuario_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Paquete_Empresa", "Responsable_Id", "dbo.Usuario");
            DropIndex("dbo.Paquete_Empresa", new[] { "Responsable_Id" });
            DropColumn("dbo.Paquete_Empresa", "Responsable_Id");
        }
    }
}
