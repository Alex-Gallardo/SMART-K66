namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarNotaCredito : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Nota_Credito", "Cliente_Id", c => c.Long(nullable: false));
            AddColumn("dbo.Nota_Credito", "Serie", c => c.String(maxLength: 15));
            AddColumn("dbo.Nota_Credito", "No_Nota_Credito", c => c.String(maxLength: 30));
            CreateIndex("dbo.Nota_Credito", "Cliente_Id");
            AddForeignKey("dbo.Nota_Credito", "Cliente_Id", "dbo.Cliente", "Cliente_Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Nota_Credito", "Cliente_Id", "dbo.Cliente");
            DropIndex("dbo.Nota_Credito", new[] { "Cliente_Id" });
            DropColumn("dbo.Nota_Credito", "No_Nota_Credito");
            DropColumn("dbo.Nota_Credito", "Serie");
            DropColumn("dbo.Nota_Credito", "Cliente_Id");
        }
    }
}
