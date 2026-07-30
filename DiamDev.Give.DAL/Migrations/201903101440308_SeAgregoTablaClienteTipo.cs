namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoTablaClienteTipo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cliente_Tipo",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Descripcion = c.String(maxLength: 500),
                        Motivo = c.String(maxLength: 500),
                        Porcentaje_Descuento = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Cliente_Tipo");
        }
    }
}
