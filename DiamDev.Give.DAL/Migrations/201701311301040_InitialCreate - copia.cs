namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Agencia",
                c => new
                    {
                        Agencia_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Cliente",
                c => new
                    {
                        Cliente_Id = c.Long(nullable: false),
                        Nit = c.String(maxLength: 20),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Direccion = c.String(nullable: false, maxLength: 500),
                        DPI = c.String(maxLength: 20),
                        No_Telefono = c.String(maxLength: 20),
                        Email_Cliente = c.String(maxLength: 100),
                        Descuento = c.Int(nullable: false),
                        Vip = c.Boolean(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Cliente_Id);
            
            CreateTable(
                "dbo.Configuracion",
                c => new
                    {
                        Configuracion_Id = c.Long(nullable: false),
                        Configuracion_Padre_Id = c.Long(),
                        Nombre = c.String(maxLength: 250),
                        Identificador = c.String(maxLength: 200),
                        Valor = c.String(maxLength: 200),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Configuracion_Id);
            
            CreateTable(
                "dbo.Cuenta_Contable",
                c => new
                    {
                        Cuenta_Id = c.Long(nullable: false),
                        Cuenta_Padre_Id = c.Long(),
                        Tipo_Id = c.Long(nullable: false),
                        Cuenta = c.String(nullable: false),
                        Nombre = c.String(nullable: false),
                        Descripcion = c.String(),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Cuenta_Id)
                .ForeignKey("dbo.Cuenta_Contable_Tipo", t => t.Tipo_Id, cascadeDelete: true)
                .Index(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Cuenta_Contable_Tipo",
                c => new
                    {
                        Tipo_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Tipo_Id);
            
            CreateTable(
                "dbo.Diario_Agencia",
                c => new
                    {
                        Diario_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Diario_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Diario", t => t.Diario_Id, cascadeDelete: true)
                .Index(t => t.Diario_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Diario",
                c => new
                    {
                        Diario_Id = c.Long(nullable: false),
                        Descripcion = c.String(nullable: false),
                        Partida_Id = c.Int(nullable: false),
                        General = c.Boolean(nullable: false),
                        Fecha_Documento = c.DateTime(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Diario_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Usr_Creo);
            
            CreateTable(
                "dbo.Diario_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Diario_Id = c.Long(nullable: false),
                        Cuenta_Id = c.Long(nullable: false),
                        Debe = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Haber = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Diario_Id })
                .ForeignKey("dbo.Cuenta_Contable", t => t.Cuenta_Id, cascadeDelete: true)
                .ForeignKey("dbo.Diario", t => t.Diario_Id, cascadeDelete: true)
                .Index(t => t.Diario_Id)
                .Index(t => t.Cuenta_Id);
            
            CreateTable(
                "dbo.Usuario",
                c => new
                    {
                        Usuario_Id = c.Long(nullable: false),
                        Login = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 150),
                        Password_Android = c.String(maxLength: 150),
                        Nombre = c.String(nullable: false, maxLength: 200),
                        Fecha = c.DateTime(nullable: false),
                        Fecha_Ultima_Actividad = c.DateTime(),
                        Reiniciar_Password = c.Boolean(nullable: false),
                        Autenticar_Site = c.Boolean(nullable: false),
                        Autenticar_Android = c.Boolean(nullable: false),
                        Activo = c.Boolean(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Usuario_Id);
            
            CreateTable(
                "dbo.Usuario_Agencia",
                c => new
                    {
                        Usuario_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Usuario_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usuario_Id, cascadeDelete: true)
                .Index(t => t.Usuario_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Usuario_Rol",
                c => new
                    {
                        Usuario_Id = c.Long(nullable: false),
                        Rol_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Usuario_Id, t.Rol_Id })
                .ForeignKey("dbo.Rol", t => t.Rol_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usuario_Id, cascadeDelete: true)
                .Index(t => t.Usuario_Id)
                .Index(t => t.Rol_Id);
            
            CreateTable(
                "dbo.Rol",
                c => new
                    {
                        Rol_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Rol_Id);
            
            CreateTable(
                "dbo.Rol_Permiso",
                c => new
                    {
                        Rol_Id = c.Int(nullable: false),
                        Permiso_Id = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => new { t.Rol_Id, t.Permiso_Id })
                .ForeignKey("dbo.Permiso", t => t.Permiso_Id, cascadeDelete: true)
                .ForeignKey("dbo.Rol", t => t.Rol_Id, cascadeDelete: true)
                .Index(t => t.Rol_Id)
                .Index(t => t.Permiso_Id);
            
            CreateTable(
                "dbo.Permiso",
                c => new
                    {
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Descripcion = c.String(nullable: false, maxLength: 500),
                    })
                .PrimaryKey(t => t.Nombre);
            
            CreateTable(
                "dbo.Factura_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Factura_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Factura_Id })
                .ForeignKey("dbo.Factura", t => t.Factura_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id)
                .Index(t => t.Factura_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Factura",
                c => new
                    {
                        Factura_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Serie_Id = c.Long(nullable: false),
                        Vendedor_Id = c.Long(nullable: false),
                        Cliente_Id = c.Long(nullable: false),
                        Comentario = c.String(),
                        Descuento = c.Int(nullable: false),
                        No_Factura = c.Long(nullable: false),
                        Anulada = c.Boolean(nullable: false),
                        Empleado = c.Boolean(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Usr_Anular = c.Long(),
                        Fecha_Anular = c.DateTime(),
                        Factura_Electronica = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Factura_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id, cascadeDelete: true)
                .ForeignKey("dbo.Serie", t => t.Serie_Id, cascadeDelete: true)
                .ForeignKey("dbo.Usuario", t => t.Usr_Anular)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo)
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Serie_Id)
                .Index(t => t.Vendedor_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Usr_Creo)
                .Index(t => t.Usr_Anular);
            
            CreateTable(
                "dbo.Factura_Forma_Pago",
                c => new
                    {
                        Factura_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Factura_Id, t.Forma_Pago_Id })
                .ForeignKey("dbo.Factura", t => t.Factura_Id, cascadeDelete: true)
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .Index(t => t.Factura_Id)
                .Index(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Forma_Pago",
                c => new
                    {
                        Forma_Pago_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Serie",
                c => new
                    {
                        Serie_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Serie_Id);
            
            CreateTable(
                "dbo.Serie_Agencia",
                c => new
                    {
                        Serie_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Serie_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Serie", t => t.Serie_Id, cascadeDelete: true)
                .Index(t => t.Serie_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Vendedor",
                c => new
                    {
                        Vendedor_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Vendedor_Id);
            
            CreateTable(
                "dbo.Vendedor_Agencia",
                c => new
                    {
                        Vendedor_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Vendedor_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Vendedor", t => t.Vendedor_Id, cascadeDelete: true)
                .Index(t => t.Vendedor_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Producto",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Producto_Padre_Id = c.String(maxLength: 50),
                        Categoria_Id = c.Long(nullable: false),
                        Marca_Id = c.Long(nullable: false),
                        Unidad_Id = c.Long(nullable: false),
                        Codigo = c.String(maxLength: 250),
                        Nombre = c.String(nullable: false),
                        Descripcion = c.String(),
                        Minimo = c.Int(nullable: false),
                        Maximo = c.Int(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Producto_Id)
                .ForeignKey("dbo.Producto_Categoria", t => t.Categoria_Id, cascadeDelete: true)
                .ForeignKey("dbo.Marca", t => t.Marca_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id, cascadeDelete: true)
                .Index(t => t.Categoria_Id)
                .Index(t => t.Marca_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Producto_Categoria",
                c => new
                    {
                        Producto_Categoria_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 250),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Producto_Categoria_Id);
            
            CreateTable(
                "dbo.Producto_Fotografia",
                c => new
                    {
                        Fotografia_Id = c.Int(nullable: false),
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Nombre = c.String(maxLength: 200),
                        ContentType = c.String(maxLength: 150),
                        Length = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => new { t.Fotografia_Id, t.Producto_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Marca",
                c => new
                    {
                        Marca_Id = c.Long(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Marca_Id);
            
            CreateTable(
                "dbo.Producto_Precio",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Precio_Id = c.Int(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Producto_Id, t.Precio_Id })
                .ForeignKey("dbo.Precio", t => t.Precio_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id)
                .Index(t => t.Precio_Id);
            
            CreateTable(
                "dbo.Precio",
                c => new
                    {
                        Precio_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Precio_Id);
            
            CreateTable(
                "dbo.Unidad",
                c => new
                    {
                        Unidad_Id = c.Long(nullable: false),
                        Codigo = c.String(nullable: false, maxLength: 100),
                        Nombre = c.String(nullable: false, maxLength: 500),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Menu",
                c => new
                    {
                        Menu_Id = c.Int(nullable: false),
                        Menu_Padre_Id = c.Int(),
                        Nombre = c.String(nullable: false, maxLength: 150),
                        Titulo = c.String(nullable: false, maxLength: 150),
                        Action = c.String(maxLength: 50),
                        Controller = c.String(maxLength: 50),
                        Orden = c.Int(nullable: false),
                        IconName = c.String(maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        PermisoId = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Menu_Id)
                .ForeignKey("dbo.Permiso", t => t.PermisoId, cascadeDelete: true)
                .Index(t => t.PermisoId);
            
            CreateTable(
                "dbo.Movimiento_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Movimiento_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio_Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Precio = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Movimiento_Id })
                .ForeignKey("dbo.Movimiento", t => t.Movimiento_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id)
                .Index(t => t.Movimiento_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Movimiento",
                c => new
                    {
                        Movimiento_Id = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Movimiento_Tipo_Id = c.Int(nullable: false),
                        Proveedor_Id = c.Long(),
                        Cliente_Id = c.Long(),
                        Descripcion = c.String(nullable: false),
                        Descuento = c.Int(nullable: false),
                        Operado = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Usr_Creo = c.Long(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Movimiento_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Cliente", t => t.Cliente_Id)
                .ForeignKey("dbo.Movimiento_Tipo", t => t.Movimiento_Tipo_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Creo, cascadeDelete: true)
                .Index(t => t.Agencia_Id)
                .Index(t => t.Movimiento_Tipo_Id)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Cliente_Id)
                .Index(t => t.Usr_Creo);
            
            CreateTable(
                "dbo.Movimiento_Tipo",
                c => new
                    {
                        Movimiento_Tipo_Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 250),
                    })
                .PrimaryKey(t => t.Movimiento_Tipo_Id);
            
            CreateTable(
                "dbo.Movimiento_Forma_Pago",
                c => new
                    {
                        Movimiento_Id = c.Long(nullable: false),
                        Forma_Pago_Id = c.Long(nullable: false),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Nota = c.String(),
                    })
                .PrimaryKey(t => new { t.Movimiento_Id, t.Forma_Pago_Id })
                .ForeignKey("dbo.Forma_Pago", t => t.Forma_Pago_Id, cascadeDelete: true)
                .ForeignKey("dbo.Movimiento", t => t.Movimiento_Id, cascadeDelete: true)
                .Index(t => t.Movimiento_Id)
                .Index(t => t.Forma_Pago_Id);
            
            CreateTable(
                "dbo.Proveedor",
                c => new
                    {
                        Proveedor_Id = c.Long(nullable: false),
                        Nit = c.String(maxLength: 20),
                        Nombre = c.String(nullable: false, maxLength: 300),
                        Nombre_Cheque = c.String(maxLength: 300),
                        Direccion = c.String(nullable: false, maxLength: 500),
                        No_Telefono_Oficina = c.String(nullable: false, maxLength: 20),
                        Email_Proveedor = c.String(maxLength: 100),
                        Patente = c.String(maxLength: 300),
                        Contacto = c.String(maxLength: 300),
                        No_Telefono_Contacto = c.String(maxLength: 20),
                        Email_Contacto = c.String(maxLength: 100),
                        Activo = c.Boolean(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Proveedor_Id);
            
            CreateTable(
                "dbo.Proveedor_Producto",
                c => new
                    {
                        Proveedor_Id = c.Long(nullable: false),
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => new { t.Proveedor_Id, t.Producto_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .ForeignKey("dbo.Proveedor", t => t.Proveedor_Id, cascadeDelete: true)
                .Index(t => t.Proveedor_Id)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Producto_Inventario",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Agencia_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Transito = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Producto_Id, t.Agencia_Id })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Producto", t => t.Producto_Id, cascadeDelete: true)
                .Index(t => t.Producto_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Producto_Precio_Costo",
                c => new
                    {
                        Producto_Id = c.String(nullable: false, maxLength: 50),
                        Precio_Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Producto_Id)
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .Index(t => t.Producto_Id);
            
            CreateTable(
                "dbo.Serie_Agencia_Factura",
                c => new
                    {
                        Serie_Id = c.Long(nullable: false),
                        Factura = c.Long(nullable: false),
                        Agencia_Id = c.Long(nullable: false),
                        Operada = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.Serie_Id, t.Factura })
                .ForeignKey("dbo.Agencia", t => t.Agencia_Id, cascadeDelete: true)
                .ForeignKey("dbo.Serie", t => t.Serie_Id, cascadeDelete: true)
                .Index(t => t.Serie_Id)
                .Index(t => t.Agencia_Id);
            
            CreateTable(
                "dbo.Traslado_Detalle",
                c => new
                    {
                        Detalle_Id = c.Int(nullable: false),
                        Traslado_Id = c.Long(nullable: false),
                        Producto_Id = c.String(maxLength: 50),
                        Unidad_Id = c.Long(nullable: false),
                        Cantidad = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.Detalle_Id, t.Traslado_Id })
                .ForeignKey("dbo.Producto", t => t.Producto_Id)
                .ForeignKey("dbo.Traslado", t => t.Traslado_Id, cascadeDelete: true)
                .ForeignKey("dbo.Unidad", t => t.Unidad_Id)
                .Index(t => t.Traslado_Id)
                .Index(t => t.Producto_Id)
                .Index(t => t.Unidad_Id);
            
            CreateTable(
                "dbo.Traslado",
                c => new
                    {
                        Traslado_Id = c.Long(nullable: false),
                        Agencia_Origen_Id = c.Long(nullable: false),
                        Agencia_Destino_Id = c.Long(nullable: false),
                        Descripcion = c.String(nullable: false),
                        Usr_Inicial = c.Long(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        Correlativo = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Traslado_Id)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Destino_Id, cascadeDelete: true)
                .ForeignKey("dbo.Agencia", t => t.Agencia_Origen_Id)
                .ForeignKey("dbo.Usuario", t => t.Usr_Inicial, cascadeDelete: true)
                .Index(t => t.Agencia_Origen_Id)
                .Index(t => t.Agencia_Destino_Id)
                .Index(t => t.Usr_Inicial);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Traslado_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Traslado", "Usr_Inicial", "dbo.Usuario");
            DropForeignKey("dbo.Traslado_Detalle", "Traslado_Id", "dbo.Traslado");
            DropForeignKey("dbo.Traslado", "Agencia_Origen_Id", "dbo.Agencia");
            DropForeignKey("dbo.Traslado", "Agencia_Destino_Id", "dbo.Agencia");
            DropForeignKey("dbo.Traslado_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Serie_Agencia_Factura", "Serie_Id", "dbo.Serie");
            DropForeignKey("dbo.Serie_Agencia_Factura", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Producto_Precio_Costo", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto_Inventario", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto_Inventario", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Movimiento_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Movimiento_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Movimiento", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Movimiento", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Proveedor_Producto", "Proveedor_Id", "dbo.Proveedor");
            DropForeignKey("dbo.Proveedor_Producto", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Movimiento_Forma_Pago", "Movimiento_Id", "dbo.Movimiento");
            DropForeignKey("dbo.Movimiento_Forma_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Movimiento", "Movimiento_Tipo_Id", "dbo.Movimiento_Tipo");
            DropForeignKey("dbo.Movimiento_Detalle", "Movimiento_Id", "dbo.Movimiento");
            DropForeignKey("dbo.Movimiento", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Movimiento", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Menu", "PermisoId", "dbo.Permiso");
            DropForeignKey("dbo.Factura_Detalle", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Factura_Detalle", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto", "Unidad_Id", "dbo.Unidad");
            DropForeignKey("dbo.Producto_Precio", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto_Precio", "Precio_Id", "dbo.Precio");
            DropForeignKey("dbo.Producto", "Marca_Id", "dbo.Marca");
            DropForeignKey("dbo.Producto_Fotografia", "Producto_Id", "dbo.Producto");
            DropForeignKey("dbo.Producto", "Categoria_Id", "dbo.Producto_Categoria");
            DropForeignKey("dbo.Factura", "Vendedor_Id", "dbo.Vendedor");
            DropForeignKey("dbo.Vendedor_Agencia", "Vendedor_Id", "dbo.Vendedor");
            DropForeignKey("dbo.Vendedor_Agencia", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Factura", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Factura", "Usr_Anular", "dbo.Usuario");
            DropForeignKey("dbo.Factura", "Serie_Id", "dbo.Serie");
            DropForeignKey("dbo.Serie_Agencia", "Serie_Id", "dbo.Serie");
            DropForeignKey("dbo.Serie_Agencia", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Factura_Forma_Pago", "Forma_Pago_Id", "dbo.Forma_Pago");
            DropForeignKey("dbo.Factura_Forma_Pago", "Factura_Id", "dbo.Factura");
            DropForeignKey("dbo.Factura_Detalle", "Factura_Id", "dbo.Factura");
            DropForeignKey("dbo.Factura", "Cliente_Id", "dbo.Cliente");
            DropForeignKey("dbo.Factura", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Diario", "Usr_Creo", "dbo.Usuario");
            DropForeignKey("dbo.Usuario_Rol", "Usuario_Id", "dbo.Usuario");
            DropForeignKey("dbo.Usuario_Rol", "Rol_Id", "dbo.Rol");
            DropForeignKey("dbo.Rol_Permiso", "Rol_Id", "dbo.Rol");
            DropForeignKey("dbo.Rol_Permiso", "Permiso_Id", "dbo.Permiso");
            DropForeignKey("dbo.Usuario_Agencia", "Usuario_Id", "dbo.Usuario");
            DropForeignKey("dbo.Usuario_Agencia", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Diario_Detalle", "Diario_Id", "dbo.Diario");
            DropForeignKey("dbo.Diario_Detalle", "Cuenta_Id", "dbo.Cuenta_Contable");
            DropForeignKey("dbo.Diario_Agencia", "Diario_Id", "dbo.Diario");
            DropForeignKey("dbo.Diario_Agencia", "Agencia_Id", "dbo.Agencia");
            DropForeignKey("dbo.Cuenta_Contable", "Tipo_Id", "dbo.Cuenta_Contable_Tipo");
            DropIndex("dbo.Traslado", new[] { "Usr_Inicial" });
            DropIndex("dbo.Traslado", new[] { "Agencia_Destino_Id" });
            DropIndex("dbo.Traslado", new[] { "Agencia_Origen_Id" });
            DropIndex("dbo.Traslado_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Traslado_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Traslado_Detalle", new[] { "Traslado_Id" });
            DropIndex("dbo.Serie_Agencia_Factura", new[] { "Agencia_Id" });
            DropIndex("dbo.Serie_Agencia_Factura", new[] { "Serie_Id" });
            DropIndex("dbo.Producto_Precio_Costo", new[] { "Producto_Id" });
            DropIndex("dbo.Producto_Inventario", new[] { "Agencia_Id" });
            DropIndex("dbo.Producto_Inventario", new[] { "Producto_Id" });
            DropIndex("dbo.Proveedor_Producto", new[] { "Producto_Id" });
            DropIndex("dbo.Proveedor_Producto", new[] { "Proveedor_Id" });
            DropIndex("dbo.Movimiento_Forma_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Movimiento_Forma_Pago", new[] { "Movimiento_Id" });
            DropIndex("dbo.Movimiento", new[] { "Usr_Creo" });
            DropIndex("dbo.Movimiento", new[] { "Cliente_Id" });
            DropIndex("dbo.Movimiento", new[] { "Proveedor_Id" });
            DropIndex("dbo.Movimiento", new[] { "Movimiento_Tipo_Id" });
            DropIndex("dbo.Movimiento", new[] { "Agencia_Id" });
            DropIndex("dbo.Movimiento_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Movimiento_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Movimiento_Detalle", new[] { "Movimiento_Id" });
            DropIndex("dbo.Menu", new[] { "PermisoId" });
            DropIndex("dbo.Producto_Precio", new[] { "Precio_Id" });
            DropIndex("dbo.Producto_Precio", new[] { "Producto_Id" });
            DropIndex("dbo.Producto_Fotografia", new[] { "Producto_Id" });
            DropIndex("dbo.Producto", new[] { "Unidad_Id" });
            DropIndex("dbo.Producto", new[] { "Marca_Id" });
            DropIndex("dbo.Producto", new[] { "Categoria_Id" });
            DropIndex("dbo.Vendedor_Agencia", new[] { "Agencia_Id" });
            DropIndex("dbo.Vendedor_Agencia", new[] { "Vendedor_Id" });
            DropIndex("dbo.Serie_Agencia", new[] { "Agencia_Id" });
            DropIndex("dbo.Serie_Agencia", new[] { "Serie_Id" });
            DropIndex("dbo.Factura_Forma_Pago", new[] { "Forma_Pago_Id" });
            DropIndex("dbo.Factura_Forma_Pago", new[] { "Factura_Id" });
            DropIndex("dbo.Factura", new[] { "Usr_Anular" });
            DropIndex("dbo.Factura", new[] { "Usr_Creo" });
            DropIndex("dbo.Factura", new[] { "Cliente_Id" });
            DropIndex("dbo.Factura", new[] { "Vendedor_Id" });
            DropIndex("dbo.Factura", new[] { "Serie_Id" });
            DropIndex("dbo.Factura", new[] { "Agencia_Id" });
            DropIndex("dbo.Factura_Detalle", new[] { "Unidad_Id" });
            DropIndex("dbo.Factura_Detalle", new[] { "Producto_Id" });
            DropIndex("dbo.Factura_Detalle", new[] { "Factura_Id" });
            DropIndex("dbo.Rol_Permiso", new[] { "Permiso_Id" });
            DropIndex("dbo.Rol_Permiso", new[] { "Rol_Id" });
            DropIndex("dbo.Usuario_Rol", new[] { "Rol_Id" });
            DropIndex("dbo.Usuario_Rol", new[] { "Usuario_Id" });
            DropIndex("dbo.Usuario_Agencia", new[] { "Agencia_Id" });
            DropIndex("dbo.Usuario_Agencia", new[] { "Usuario_Id" });
            DropIndex("dbo.Diario_Detalle", new[] { "Cuenta_Id" });
            DropIndex("dbo.Diario_Detalle", new[] { "Diario_Id" });
            DropIndex("dbo.Diario", new[] { "Usr_Creo" });
            DropIndex("dbo.Diario_Agencia", new[] { "Agencia_Id" });
            DropIndex("dbo.Diario_Agencia", new[] { "Diario_Id" });
            DropIndex("dbo.Cuenta_Contable", new[] { "Tipo_Id" });
            DropTable("dbo.Traslado");
            DropTable("dbo.Traslado_Detalle");
            DropTable("dbo.Serie_Agencia_Factura");
            DropTable("dbo.Producto_Precio_Costo");
            DropTable("dbo.Producto_Inventario");
            DropTable("dbo.Proveedor_Producto");
            DropTable("dbo.Proveedor");
            DropTable("dbo.Movimiento_Forma_Pago");
            DropTable("dbo.Movimiento_Tipo");
            DropTable("dbo.Movimiento");
            DropTable("dbo.Movimiento_Detalle");
            DropTable("dbo.Menu");
            DropTable("dbo.Unidad");
            DropTable("dbo.Precio");
            DropTable("dbo.Producto_Precio");
            DropTable("dbo.Marca");
            DropTable("dbo.Producto_Fotografia");
            DropTable("dbo.Producto_Categoria");
            DropTable("dbo.Producto");
            DropTable("dbo.Vendedor_Agencia");
            DropTable("dbo.Vendedor");
            DropTable("dbo.Serie_Agencia");
            DropTable("dbo.Serie");
            DropTable("dbo.Forma_Pago");
            DropTable("dbo.Factura_Forma_Pago");
            DropTable("dbo.Factura");
            DropTable("dbo.Factura_Detalle");
            DropTable("dbo.Permiso");
            DropTable("dbo.Rol_Permiso");
            DropTable("dbo.Rol");
            DropTable("dbo.Usuario_Rol");
            DropTable("dbo.Usuario_Agencia");
            DropTable("dbo.Usuario");
            DropTable("dbo.Diario_Detalle");
            DropTable("dbo.Diario");
            DropTable("dbo.Diario_Agencia");
            DropTable("dbo.Cuenta_Contable_Tipo");
            DropTable("dbo.Cuenta_Contable");
            DropTable("dbo.Configuracion");
            DropTable("dbo.Cliente");
            DropTable("dbo.Agencia");
        }
    }
}
