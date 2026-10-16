# Registro de Progreso: PergamonLens

## Estado Actual: HITO 3 - Capa de Aplicación (Casos de Uso)

---

### 📋 Hoja de Ruta de Desarrollo (Roadmap MVP)

- [x] **Hito 1: Scaffolding y Entorno Base**
  - [x] Especificación de arquitectura y visión técnica (`README.md`, `GEMINI.md`).
  - [x] Crear solución `PergamonLens.slnx` (.NET 10).
  - [x] Crear proyectos de Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Web`, `UnitTests`, `IntegrationTests`).
  - [x] Configurar referencias entre proyectos y paquetes de testing (`FluentAssertions`, `Moq`, `Mvc.Testing`).
  - [x] Crear `docker-compose.yml` para SQL Server 2022.
  - [x] Configurar `.gitignore` y `.editorconfig`.
  - [x] Inicializar Git y realizar primer commit limpio.
  - [x] Conectar y publicar repositorio público en GitHub: `https://github.com/dmaragongarrido/PergamonLens`.

- [x] **Hito 2: Core de Dominio (Música & Vinilos)**
  - [x] Definir entidad base y agregados (`VinylRecord`, `Track`, `Entity<T>`, `AggregateRoot<T>`).
  - [x] Value Objects (`Barcode`, `CatalogueNumber`, `ReleaseYear`, `VinylRecordId`, `CollectionStatistics`).
  - [x] Enums de dominio (`MediaCondition`, `ProcessingStatus`, `VinylSize`, `VinylSpeed`).
  - [x] Eventos de dominio (`VinylRecordCreatedEvent`, `VinylRecordEnrichedEvent`, `VinylRecordConfirmedEvent`, `VinylRecordFailedEvent`).
  - [x] Interfaces de abstracción (`IVisionService`, `IDiscogsService`, `IVinylRecordRepository`, `ICollectionRepository`).
  - [x] Tests unitarios de dominio (55 tests en verde con xUnit + FluentAssertions).

- [ ] **Hito 3: Capa de Aplicación (Casos de Uso)**
  - [ ] Caso de uso: Subir imagen y encolar análisis visual.
  - [ ] Caso de uso: Búsqueda y enriquecimiento en Discogs.
  - [ ] Caso de uso: Confirmar y persistir ítem en la colección personal.
  - [ ] DTOs, Mappings y Validaciones (FluentValidation).

- [ ] **Hito 4: Capa de Infraestructura & Persistencia**
  - [ ] Instalación y arranque de Docker Desktop / contenedor SQL Server 2022 (verificación de conexión con SSMS).
  - [ ] `ApplicationDbContext` de EF Core y mapeos Fluent API para SQL Server.
  - [ ] Migraciones iniciales de EF Core sobre SQL Server.
  - [ ] Cliente HTTP resiliente para Discogs API (Polly: rate limiting, reintentos).
  - [ ] Adaptador de Visión IA (análisis de portada / código de barras).
  - [ ] Canal asíncrono en segundo plano (`System.Threading.Channels` + `BackgroundService`).

- [ ] **Hito 5: Capa Web / API & Interfaz Móvil**
  - [ ] Endpoints RESTful en ASP.NET Core (Minimal APIs).
  - [ ] Frontend Web responsive adaptado a móvil (captura directa con cámara).
  - [ ] Visualización de la colección y detalles del vinilo.

- [ ] **Hito 6: Testing & CI/CD**
  - [ ] Tests de integración con Testcontainers MSSQL.
  - [ ] Pipeline de GitHub Actions (build + tests en cada PR).

---

## 🪵 Diario de A Bordo

### Día 1 (09/10/2026)
- Creación de la carpeta del proyecto `c:\source\PergamonLens`.
- Redacción del `README.md` técnico con visión, arquitectura y stack.
- Creación de `GEMINI.md` y `PROGRESS.md` para el contexto autónomo del repositorio.
- Instalación y validación del entorno: .NET 10 SDK (`10.0.401`), Git y GitHub CLI.
- Scaffolding de la solución `PergamonLens.slnx` con Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Web`, `UnitTests`, `IntegrationTests`).
- Configuración de `.editorconfig`, `.gitignore` y `docker-compose.yml` para SQL Server 2022.
- Inicialización de Git, primer commit limpio y publicación del repositorio público en GitHub: [github.com/dmaragongarrido/PergamonLens](https://github.com/dmaragongarrido/PergamonLens).
- **HITO 1 COMPLETADO**. Listo para arrancar el Hito 2 (Core de Dominio).

### Día 2 (16/10/2026)
- Migración y configuración del nuevo entorno de trabajo: .NET 10 SDK (`10.0.402`), Git (`2.55`), GitHub CLI (`2.102`), Docker Desktop y WSL.
- Implementación del Core de Dominio táctico bajo DDD en `PergamonLens.Domain`:
  - Clases base: `Entity<T>`, `AggregateRoot<T>`, `IDomainEvent`, `DomainException`.
  - Value Objects inmutables con validación y checksums: `Barcode`, `CatalogueNumber`, `ReleaseYear`, `VinylRecordId`, `CollectionStatistics`.
  - Raíz de agregado `VinylRecord` encapsulando metadatos discográficos, especificaciones físicas, estados (`ProcessingStatus`), escala Goldmine (`MediaCondition`) y colección interna de pistas (`Track`).
  - Eventos de dominio: `VinylRecordCreatedEvent`, `VinylRecordEnrichedEvent`, `VinylRecordConfirmedEvent`, `VinylRecordFailedEvent`.
  - Contratos de persistencia y servicios: `IVinylRecordRepository`, `ICollectionRepository`, `IVisionService`, `IDiscogsService`.
- Batería completa de pruebas unitarias en `PergamonLens.UnitTests`: 55 pruebas pasando en verde (0 errores).
- **HITO 2 COMPLETADO**. Listo para arrancar el Hito 3 (Capa de Aplicación).
