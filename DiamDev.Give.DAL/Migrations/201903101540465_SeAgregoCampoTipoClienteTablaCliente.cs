namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoTipoClienteTablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Tipo_Id", c => c.Long());
            CreateIndex("dbo.Cliente", "Tipo_Id");
            AddForeignKey("dbo.Cliente", "Tipo_Id", "dbo.Cliente_Tipo", "Tipo_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cliente", "Tipo_Id", "dbo.Cliente_Tipo");
            DropIndex("dbo.Cliente", new[] { "Tipo_Id" });
            DropColumn("dbo.Cliente", "Tipo_Id");
        }
    }
}
