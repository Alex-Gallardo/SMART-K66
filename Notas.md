# Notas

## Migrations

> Todos los comandos siguientes deben ejecutarse en el **Package Manager Console** del Visual Studio.

Para habilitar los Migrations (no será necesario solo para nuevos DbContexts):

```sh
Enable-Migrations -ProjectName DiamDev.Give.DAL -StartupProjectName DiamDev.Give.UI
```

> Asumiendo que `DiamDev.Give.DAL es el proyecto donde está el DbContext y DiamDev.Give.UI es el proyecto donde
> está la cofiguracion (connection string).

Para agregar nuevos migratios, necesario cada vez que se agrega, elimina o modifica una entidad:

```sh
Add-Migration NombreDelMigration -ProjectName DiamDev.Give.DAL -StartupProjectName DiamDev.Give.UI
```

> Donde **NombreDelMigration** es el nombre que se quiere asignar al nuevo Migration.

para actualizar la **base de datos de desarrollo**:

```sh
Update-Database -ProjectName DiamDev.Give.DAL -StartupProjectName DiamDev.Give.UI
```

### Actualizar Base de Datos de Producción

Para generar un script `.sql` con los cambios necesarios para actualizar la base de datos, ejecutar el siguiente
comando y guardar el archivo en la carpeta `\SqlMigrations` para incluirla en el control de codigo fuente.

```sh
Update-Database -Script -SourceMigration: MigrationInicial -TargetMigration: MigrationFinal -ProjectName DiamDev.Give.DAL -StartupProjectName DiamDev.Give.UI
```
> Donde **MigrationInicial** es el último Migration que se ejecutó en producción (ver la tabla __MigrationsHistory)
> y **MigrationFinal** es el Migration hasta el cual se quiere aplicar en la base de datos.

Despues ya se puede ejecutar el script en la base de datos de producción.
 