namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarUsuarioColumnaVendedor : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Usuario", "Vendedor_Id", c => c.Long());
            CreateIndex("dbo.Usuario", "Vendedor_Id");
            AddForeignKey("dbo.Usuario", "Vendedor_Id", "dbo.Vendedor", "Vendedor_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Usuario", "Vendedor_Id", "dbo.Vendedor");
            DropIndex("dbo.Usuario", new[] { "Vendedor_Id" });
            DropColumn("dbo.Usuario", "Vendedor_Id");
        }
    }
}
