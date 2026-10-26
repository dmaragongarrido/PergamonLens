# Contexto del Proyecto: PergamonLens (G)

Este archivo se carga automáticamente en el contexto de G al abrir el espacio de trabajo `c:\source\PergamonLens`.

---

## 🏛️ Identidad y Misión del Proyecto
- **Proyecto:** **PergamonLens** (Catálogo visual para coleccionistas de vinilos, libros y videojuegos con IA multimodal).
- **Desarrollador:** **Diego M. Aragón Garrido** (Senior Software Engineer, +20 años de experiencia en .NET/C#).
- **Copiloto IA:** **G** (Pair programmer, estratega técnico y arquitecto de software).
- **Objetivo:** Construir una pieza de ingeniería de autor en GitHub que sirva como escaparate técnico irrefutable de arquitectura limpia, concurrencia avanzada, resiliencia y .NET moderno (.NET 10 / C#).

---

## 🏗️ Estándares Técnicos & Arquitectura
- **Patrón:** Clean Architecture / principios DDD (separación estricta de responsabilidades).
  - `PergamonLens.Domain`: Cero dependencias externas. Entidades ricas, Value Objects, eventos de dominio.
  - `PergamonLens.Application`: Casos de uso (CQRS / Mediador), DTOs, validaciones, interfaces de abstracción.
  - `PergamonLens.Infrastructure`: EF Core, PostgreSQL (Npgsql), clientes externos (Discogs API, Vision AI), colas en segundo plano (`System.Threading.Channels`), resiliencia con Polly, ASP.NET Core Identity.
  - `PergamonLens.Web`: ASP.NET Core Minimal APIs / Web API + UI responsive adaptada a móvil (acceso a cámara).
- **Aislamiento Multi-usuario & Seguridad (OWASP / Anti-BOLA):** Todo lanzamiento (`MusicRelease`) pertenece estrictamente a un propietario (`UserId`). En la persistencia se aplican obligatoriamente *Global Query Filters* en EF Core (`WHERE UserId = @currentUserId`) para garantizar el aislamiento estricto e inviolable entre colecciones de usuarios.
- **Identidad & Autenticación:** Basado en el estándar nativo de código abierto *ASP.NET Core Identity* sobre PostgreSQL (`MapIdentityApi`), hash PBKDF2/Argon2 con sal, tokens criptográficos y reseteo seguro sin costes de infraestructura ni servicios externos de pago.
- **Motor de Visión e IA Multimodal (Estrategia Híbrida 0,00 €):**
  - Abstracción desacoplada en Dominio mediante `IVisionService`.
  - Pipeline de dos fases: Fase 1 determinista local en memoria para códigos de barras (`ZXing.Net`, ~2 ms, 0 tokens) + Fase 2 de inferencia multimodal con **Google Gemini 2.0 Flash / 1.5 Flash** (Google AI Studio, tier gratuito permanente de 15 RPM sin tarjeta, Structured Outputs con JSON Schema estricto y reconocimiento iconográfico de portadas sin texto).
  - Soporte alternativo para privacidad total y uso offline con modelos open-source (**Llama 3.2 Vision / Qwen2-VL** vía Ollama).
- **Persistencia:** PostgreSQL 16 (vía contenedor Docker ultraligero `postgres:16-alpine`, ~40 MB RAM) con migraciones Code-First de EF Core.
- **Testing:** xUnit, FluentAssertions, Moq, Testcontainers PostgreSQL para integración.

---

## 🌿 Estrategia de Ramas & Git Flow (GitHub Flow)
A partir del Hito 4, el proyecto opera rigurosamente bajo **GitHub Flow**:
1. **Rama principal:** `main` (siempre estable, protegida y con build en verde).
2. **Ramas de funcionalidad:** `feature/<nombre-feature-o-hito>` (ej. `feature/hito-4-infrastructure`).
3. **Pull Requests obligatorias:**
   - Todo trabajo se integra vía Pull Request hacia `main`.
   - Se verifica que el pipeline de GitHub Actions (`.github/workflows/ci.yml`) aprueba en verde antes del merge.
   - Usar *Squash and Merge* para mantener un historial lineal y limpio en `main`.

---

## 🔄 Protocolo de Inicio de Sesión
Al comenzar una nueva conversación en este workspace:
1. **Revisar estado:** Consultar inmediatamente `PROGRESS.md` y `README.md`.
2. **Resumen proactivo:** En tu primer saludo o respuesta, recuerda de forma breve y concisa:
   - En qué hito técnico exacto nos quedamos.
   - Cuál es la siguiente acción inmediata recomendada de desarrollo.
3. **Mantener actualizado el sistema:** Actualizar `PROGRESS.md` con cada avance o hito completado.
