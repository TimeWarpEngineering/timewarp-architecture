# EfSharedVariables.ps1

# $projectPath: is used to specify the path to the project that has the DbContext and entity classes (the data model).
# This is the project where the migrations will be added.
# In a solution with multiple projects, this is typically a class library project that contains the data access layer.
$projectPath = "..\..\Source\ContainerApps\Web\Web.Infrastructure\Web.Infrastructure.csproj"

# $startupProjectPath: is used to specify the path to the startup project. 
# The startup project is the one that the application starts running from.
# It's typically the project that contains the Main method.
# This is important because the EF Core tools build and run your project to gather details about your DbContext and entity classes via reflection.
# The tools need to know the startup project to do this correctly.
$startupProjectPath = "..\..\Source\ContainerApps\Web\Web.Server\Web.Server.csproj"

# $dbContext: This variable is used to specify the name of the DbContext class to use for the migration.
# If you have multiple DbContexts in your project, you can use this variable to specify which one to use.
$dbContext = "PostgresDbContext"

# $migrationsOutput: This variable is used to specify the directory (relative to the $projectPath) where the migration files will be output.
# This allows you to control where the migration files are placed in your project.
$migrationsOutput = "Persistence\Migrations"; # relative to the $projectPath

# $migrationsFolderPath: This variable is used to specify the absolute path to the directory where the migration files will be placed.
# This might be used in other parts of your scripts to directly access the migration files.
$migrationsFolderPath = "..\..\Source\ContainerApps\Web\Web.Infrastructure\$migrationsOutput"
