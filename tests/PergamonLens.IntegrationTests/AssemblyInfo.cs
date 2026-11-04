using Xunit;

// Las clases de integración comparten la misma base de datos PostgreSQL (PergamonLensDb);
// deshabilitar la paralelización entre colecciones evita condiciones de carrera en Database.Migrate()
// cuando el contenedor PostgreSQL arranca vacío en GitHub Actions CI.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
