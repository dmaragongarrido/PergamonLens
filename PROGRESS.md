# Registro de Progreso: PergamonLens

## Estado Actual: HITO 2 - Core de Dominio (Música & Vinilos)

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

- [ ] **Hito 2: Core de Dominio (Música & Vinilos)**
  - [ ] Definir entidad base y agregados (`Collection`, `VinylRecord`, `PhysicalRelease`, `Artist`).
  - [ ] Value Objects (`Barcode`, `CatalogueNumber`, `ReleaseYear`).
  - [ ] Interfaces de abstracción (`IVisionService`, `IDiscogsService`, `ICollectionRepository`).
  - [ ] Tests unitarios de dominio (xUnit + FluentAssertions).

- [ ] **Hito 3: Capa de Aplicación (Casos de Uso)**
  - [ ] Caso de uso: Subir imagen y encolar análisis visual.
  - [ ] Caso de uso: Búsqueda y enriquecimiento en Discogs.
  - [ ] Caso de uso: Confirmar y persistir ítem en la colección personal.
  - [ ] DTOs, Mappings y Validaciones (FluentValidation).

- [ ] **Hito 4: Capa de Infraestructura & Persistencia**
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
