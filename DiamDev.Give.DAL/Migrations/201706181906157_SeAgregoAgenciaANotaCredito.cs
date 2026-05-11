namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoAgenciaANotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Nota_Credito", "Agencia_Id", c => c.Long(nullable: false));
            AddColumn("dbo.Nota_Credito", "Anulada", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Nota_Credito", "Agencia_Id");
            AddForeignKey("dbo.Nota_Credito", "Agencia_Id", "dbo.Agencia", "Agencia_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Nota_Credito", "Agencia_Id", "dbo.Agencia");
            DropIndex("dbo.Nota_Credito", new[] { "Agencia_Id" });
            DropColumn("dbo.Nota_Credito", "Anulada");
            DropColumn("dbo.Nota_Credito", "Agencia_Id");
        }
    }
}
