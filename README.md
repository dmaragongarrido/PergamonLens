<div align="center">

# 🏛️ PergamonLens

**Catálogo Visual con IA Multimodal para Coleccionistas de Formatos Físicos**

*Música (Vinilos, CDs, Casetes) · Libros · Videojuegos*

[![CI Build & Test](https://github.com/dmaragongarrido/PergamonLens/actions/workflows/ci.yml/badge.svg)](https://github.com/dmaragongarrido/PergamonLens/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

</div>

---

## 🎯 Visión del Proyecto

**PergamonLens** es una solución de catálogo inteligente diseñada para coleccionistas de medios físicos. El objetivo principal es eliminar la fricción manual de inventariar estanterías de vinilos, libros o videojuegos mediante una experiencia visual ágil: **apuntas la cámara del móvil a un álbum o estantería y la IA se encarga de identificar, clasificar, enriquecer y tasar cada pieza**.

### El Problema
Catalogar colecciones físicas suele ser tedioso: introducir a mano códigos de barras o referencias alfanuméricas (números de matriz, sello discográfico, año de prensado) resulta lento y propenso a errores. Muchas veces, ediciones antiguas o especiales ni siquiera cuentan con código de barras legible.

### La Solución PergamonLens
1. **Fotografía Rápida:** Tomas una foto de la portada, lomo o código de barras desde el móvil o navegador.
2. **Extracción Visual con IA:** Un modelo de visión multimodal identifica título, artista/autor, edición, sello discográfico o editorial.
3. **Enriquecimiento Asíncrono con APIs Especializadas:** El sistema cruza los datos automáticamente con bases de datos canónicas:
   - 🎵 **Música:** Discogs API & MusicBrainz.
   - 📚 **Libros:** Open Library & Google Books API.
   - 🎮 **Videojuegos:** IGDB (Twitch) & RAWG API.
4. **Bóveda Personal:** Tu colección organizada con estadísticas, valoración estimada, búsqueda instantánea y filtros avanzados.

---

## 🏗️ Arquitectura Técnica

El proyecto está diseñado siguiendo las mejores prácticas de ingeniería de software en **.NET moderno (C# / .NET 10)**, aplicando principios de **Clean Architecture**, alta cohesión y bajo acoplamiento:

```text
PergamonLens/
├── src/
│   ├── PergamonLens.Domain/          # Entidades puras, Value Objects, eventos de dominio
│   ├── PergamonLens.Application/     # Casos de uso (CQRS / MediatR), validaciones, DTOs
│   ├── PergamonLens.Infrastructure/  # EF Core, PostgreSQL (Npgsql), Identity, APIs externas, IA
│   └── PergamonLens.Web/             # ASP.NET Core Minimal APIs + Frontend Web Responsive SPA
├── tests/
│   ├── PergamonLens.UnitTests/       # Pruebas unitarias de dominio y casos de uso
│   └── PergamonLens.IntegrationTests/# Pruebas de integración con PostgreSQL real
└── docker/
    └── docker-compose.yml            # Orquestación de PostgreSQL 16 (Alpine)
```

### 🧱 Principios de Diseño
- **Clean Architecture & DDD Puro:** Reglas de negocio independientes de frameworks, bases de datos y librerías externas.
  - **Aggregate Root Polivalente (`MusicRelease`):** Modela lanzamientos musicales físicos en múltiples formatos (`MediaFormat`: Vinilo, CD, Casete y Box Set), controlando el inventario de pistas (`Track`), graduación oficial Goldmine (`MediaCondition`) y especificaciones físicas (pulgadas, RPM, número de soportes).
  - **Value Objects con Validación Estricta:** Inmutabilidad y blindaje contra *Primitive Obsession* (`Barcode` con cálculo y validación de checksum GS1, `CatalogueNumber`, `ReleaseYear`, `MusicReleaseId`, `CollectionStatistics`).
  - **Eventos de Dominio:** Desacoplamiento total para disparar acciones reactivas asíncronas (`MusicReleaseCreatedEvent`, `MusicReleaseEnrichedEvent`, etc.).
- **Seguridad & Aislamiento Multi-usuario:** Modelo multi-coleccionista donde cada lanzamiento pertenece estrictamente a su propietario (`UserId`). Aislamiento garantizado en base de datos mediante *Global Query Filters* de EF Core contra vulnerabilidades de acceso cruzado (OWASP BOLA/IDOR). Autenticación de serie con ASP.NET Core Identity API Endpoints (PBKDF2 con sal, tokens criptográficos, bloqueo por fuerza bruta y recuperación segura a coste cero de licencias).
- **Pipeline de Visión e IA Multimodal (Estrategia Híbrida & Polimórfica):**
  - **Fase 1 (Escudo Anti-Cuota Determinista Local, 0 coste, ~2 ms):** Decodificación algorítmica local en memoria de códigos de barras (EAN-13, UPC, Code 128, QR) con `ZXing.Net` y `SkiaSharp`. Detección determinista de prefijos Bookland (ISBN 978/979) para categorización automática de libros sin IA.
  - **Fase 2 (Visión Multimodal Polimórfica con Inferencia Estructurada):** Modelo de alta velocidad (**Google Gemini 2.0 Flash**) vía Google AI Studio (Tier gratuito permanente de 15 RPM sin tarjeta) con *Structured Outputs* (JSON Schema estricto) para catalogación polimórfica de **música (vinilos/CDs), libros y videojuegos**, incluyendo plataformas de consola, sellos/editoriales, identificadores y reconocimiento iconográfico de portadas sin texto.
  - **Resiliencia & Rate Limiting (Polly):** Políticas de reintento exponencial con *jitter* ante posibles errores `429 Too Many Requests`, limitador de tasa interno (*Rate Limiter*) y degradación elegante (*Graceful Degradation*) a estado `PendingAnalysis` o entrada manual si la red falla.
  - **Privacidad & Offline (Ollama Fallback):** Soporte alternativo 100% desacoplado mediante el contrato [`IVisionService`](file:///src/PergamonLens.Domain/Services/IVisionService.cs) para inferencia local y privada con modelos *open source* (**Llama 3.2 Vision / Qwen2-VL** vía Ollama) sin depender de proveedores cloud externos.
- **Concurrencia & Asincronía con Channels:** Procesamiento en segundo plano de imágenes e indexación mediante `System.Threading.Channels` para garantizar tiempos de respuesta inmediatos al usuario.
- **Resiliencia & Rate Limiting (Polly):** Control estricto de cuotas y políticas de reintento exponencial contra APIs externas que imponen límites de peticiones (ej. Discogs con 60 req/min).
- **Persistencia Robusta & Eficiente:** **PostgreSQL 16** (vía `Npgsql.EntityFrameworkCore.PostgreSQL`) con **Entity Framework Core**, migraciones automatizadas e índices optimizados, con huella de memoria ultraligera (~40 MB de RAM).
- **Integración Continua & GitHub Flow:** Desarrollo guiado por ramas cortas (`feature/*`), validación estricta automatizada mediante **GitHub Actions** en cada Pull Request (`dotnet build` + `dotnet test`) y estrategia de *Squash & Merge* hacia `main`.

---

## 🛠️ Stack Tecnológico

| Capa | Tecnología | Propósito |
| :--- | :--- | :--- |
| **Backend Core** | **.NET 10 / C#** | Alto rendimiento, sintaxis moderna y tipado estricto. |
| **Base de Datos** | **PostgreSQL 16 (Alpine)** | Almacenamiento relacional ultraligero (~40 MB RAM), escalable y compatible con hosting gratuito. |
| **ORM & Migraciones** | **Entity Framework Core** | Mapeo objeto-relacional y control de esquemas. |
| **Seguridad & Identidad** | **ASP.NET Core Identity** | Autenticación REST nativa, hash PBKDF2 y sesiones sin coste externo. |
| **Motor de Visión / IA** | **Gemini 2.0 Flash / Llama 3.2 Vision** | Inferencia multimodal estructurada (JSON) + OCR determinista local. |
| **APIs Externas** | **Discogs, OpenLibrary, IGDB** | Enriquecimiento canónico de música, libros y videojuegos. |
| **Frontend Web** | **Web Responsive / SPA** | Interfaz optimizada para móvil con acceso directo a cámara WebRTC. |
| **Testing** | **xUnit, FluentAssertions, Moq** | Cobertura exhaustiva de lógica de negocio y casos de uso. |
| **Contenedores** | **Docker & Docker Compose** | Entorno de desarrollo reproducible con un solo comando. |

---

## 🚀 Hoja de Ruta del MVP (Fase 1)

- [x] **Hito 1: Scaffolding y Entorno Base**
  - Solución Clean Architecture en .NET 10 (`Domain`, `Application`, `Infrastructure`, `Web`, `UnitTests`, `IntegrationTests`).
  - Configuración de `docker-compose.yml`, `.editorconfig` y `.gitignore`.
  - Repositorio público configurado en GitHub.
- [x] **Hito 2: Core de Dominio (Música & Formatos Físicos: Vinilo, CD, Casete)**
  - Raíz de agregado `MusicRelease` con invariantes de negocio, ciclo de vida (`ProcessingStatus`) y soporte multiformato.
  - Value Objects inmutables (`Barcode` con checksum GS1, `CatalogueNumber`, `ReleaseYear`, `MusicReleaseId`, `CollectionStatistics`).
  - Eventos de dominio y contratos/puertos de abstracción (`IMusicReleaseRepository`, `ICollectionRepository`, `IVisionService`, `IDiscogsService`).
  - Batería de **59 tests unitarios** en verde con xUnit y FluentAssertions (~0.5s de ejecución).
- [x] **Hito 3: Capa de Aplicación (Casos de Uso / CQRS)**
  - Pipeline de MediatR con validación transversal (`ValidationBehavior`) y FluentValidation.
  - Comandos y Handlers: `CreateReleaseCommand`, `ScanReleaseCommand`, `EnrichReleaseFromDiscogsCommand`, `ConfirmReleaseCommand`, `DeleteReleaseCommand`.
  - Consultas y Handlers: `GetReleaseByIdQuery`, `GetCollectionQuery` (filtros combinados), `GetCollectionStatisticsQuery`, `SearchDiscogsQuery`.
  - DTOs inmutables y mapeos estáticos puros (`MappingExtensions`).
  - Suite de **97 tests unitarios** al 100% en verde (~250 ms).
- [x] **Hito 4: Capa de Infraestructura & Persistencia (PostgreSQL)**
  - `PergamonLensDbContext` con mapeos Fluent API para PostgreSQL 16 (Value Objects, enums, cascada e índices).
  - Migración inicial Code-First (`InitialPostgreSqlMigration`) aplicada con éxito.
  - Repositorios de persistencia y consultas agregadas (`MusicReleaseRepository`, `CollectionRepository`).
  - Cliente HTTP resiliente para Discogs API con Polly (`AddStandardResilienceHandler`).
  - Procesamiento asíncrono en segundo plano mediante `System.Threading.Channels` y `BackgroundService`.
  - Suite de tests al 100% en verde.
- [x] **Hito 5: Capa Web, Identidad & Interfaz Móvil**
  - Soporte multi-usuario con *Global Query Filters* en EF Core y pertenencia de lanzamientos (`UserId`).
  - Endpoints de autenticación REST con ASP.NET Core Identity (`/auth/register`, `/auth/login`, recuperación de contraseña).
  - Endpoints RESTful de catálogo en Minimal APIs protegidos con autorización (`[Authorize]`).
  - Frontend Web responsive SPA servida en `wwwroot` optimizada para móvil con captura WebRTC y cámara nativa.
  - Suite de **135 tests** (115 unitarios + 20 de integración) pasando al 100% en verde sobre PostgreSQL.
- [x] **Hito 6: Motor de Visión & IA Multimodal (Google Gemini + ZXing.Net)**
  - Pipeline de inferencia visual híbrido en dos fases: Fase 1 determinista local en memoria (`ZXing.Net`, ~2 ms, 0 tokens) + Fase 2 multimodal con Google Gemini (`gemini-flash-lite-latest`, ~1 s, 0,00 € permanente).
  - Structured Outputs con JSON Schema estricto (Música, Libros, Videojuegos) y reconocimiento iconográfico de portadas sin texto.
  - Resiliencia interactiva con Polly (fast-fail) para respuesta en tiempo real sin bloqueo de interfaz.
  - Auto-deducción inteligente del soporte físico (CD vs Vinilo) y enriquecimiento canónico automático desde Discogs (portada oficial en alta resolución y tracklist).
  - Batería de **160 tests** (140 unitarios + 20 de integración sobre PostgreSQL) al 100% en verde.
- [ ] **Hito 7: Rediseño UI/UX Premium & Experiencia Móvil**
  - Rediseño de la interfaz visual con estética de autor y navegación táctil mobile-first.
  - Arquitectura visual y de interacción extensible y polimórfica (diseñada para música actual y preparada para libros, videojuegos y futuros coleccionables).
  - Flujo de escaneo e ingesta mágica con transiciones de estado inmersivas.
  - Graduación de estado y vistas de Bóveda optimizadas para coleccionistas.
- [ ] **Hito 8: CI/CD, Despliegue & Producción**
  - Dockerización final multi-stage y optimización del pipeline de GitHub Actions.
  - Configuración de variables de entorno, secrets y publicación en producción.
  - Verificación final de escaparate técnico de autor.

---

## 💻 Inicio Rápido en Local

### Requisitos Previos
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/)

### Pasos
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/dmaragongarrido/PergamonLens.git
   cd PergamonLens
   ```
2. Levantar la base de datos PostgreSQL:
   ```bash
   docker compose -f docker/docker-compose.yml up -d
   ```
3. Ejecutar la batería de tests (Unitarios + Integración):
   ```bash
   dotnet test
   ```
4. Ejecutar la aplicación:
   ```bash
   dotnet run --project src/PergamonLens.Web
   ```

---

## 📄 Licencia
Distribuido bajo la licencia MIT. Consulta el archivo `LICENSE` para más información.
