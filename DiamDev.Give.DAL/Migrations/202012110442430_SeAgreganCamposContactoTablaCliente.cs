namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposContactoTablaCliente : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Cliente", "Nombre_Contacto", c => c.String());
            AddColumn("dbo.Cliente", "Telefono_Contacto", c => c.String());
            AddColumn("dbo.Cliente", "Celular_Contacto", c => c.String());
            AddColumn("dbo.Cliente", "Correo_Contacto", c => c.String());
            AddColumn("dbo.Cliente", "Nota_Contacto", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Cliente", "Nota_Contacto");
            DropColumn("dbo.Cliente", "Correo_Contacto");
            DropColumn("dbo.Cliente", "Celular_Contacto");
            DropColumn("dbo.Cliente", "Telefono_Contacto");
            DropColumn("dbo.Cliente", "Nombre_Contacto");
        }
    }
}
