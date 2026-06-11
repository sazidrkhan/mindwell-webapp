<div align="center">

# MindWell WebApp

**An academic ASP.NET Web Forms mental-health learning platform**

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
[![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-5C2D91)](https://learn.microsoft.com/en-us/aspnet/web-forms/)
[![Database](https://img.shields.io/badge/Database-SQL%20Server%20LocalDB-CC2927)](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](LICENSE)

</div>

MindWell is a coursework web application built with classic ASP.NET Web Forms and .NET Framework 4.8. It provides public mental-health learning content, authenticated learner tools, and administrator management features in a single Visual Studio solution.

> MindWell is a coursework/demo application. It is not medical advice, crisis support, or a production healthcare system. Do not store real private health information in this project.

## Table of Contents

- [Highlights](#highlights)
- [Feature Overview](#feature-overview)
- [Tech Stack](#tech-stack)
- [Application Scope](#application-scope)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Run Locally](#run-locally)
- [Database Behavior](#database-behavior)
- [Security Notes](#security-notes)
- [Limitations](#limitations)
- [License](#license)

## Highlights

- 30-page Web Forms application with public, learner, and admin areas.
- Automatic SQL Server LocalDB setup on first application startup.
- Coursework-level authentication, authorization, CSRF validation, and password hashing.
- Optional TOTP multi-factor authentication flow with QR code generation through QRCoder.
- Custom Calm & Organic interface with responsive layouts, light/dark theme support, and vanilla JavaScript enhancements.
- Administrator tools for managing users, modules, content, quizzes, feedback, and demo data quality.

## Feature Overview

| Area | Features |
| --- | --- |
| Public site | Home, About, FAQ, Contact, learning modules, article previews, resource library, registration, sign-in, and password-reset requests. |
| Learner area | Dashboard, profile, password changes, progress tracking, quizzes, quiz results, mood tracker, mood history, reflection journal, forum, and activity submissions. |
| Admin area | User management, module management, content/resource management, quiz management, feedback/contact review, and dashboard summaries. |
| Data layer | SQL Server LocalDB database, schema scripts, automatic startup initialization, seeded learning content, and parameterized query helpers. |
| UI layer | Shared master pages, custom CSS design system, responsive cards/tables, accessible form patterns, password toggles, theme toggle, and progressive JavaScript. |
| Security layer | Forms Authentication, role checks, session restoration, CSRF tokens, output encoding, password hashing, security headers, and optional TOTP verification. |

## Tech Stack

| Layer | Technology |
| --- | --- |
| Framework | ASP.NET Web Forms on .NET Framework 4.8 |
| Language | C# code-behind |
| Database | SQL Server Express LocalDB |
| Data access | `System.Data.SqlClient` |
| Frontend | HTML, CSS, WebForms server controls, vanilla JavaScript |
| Runtime | Visual Studio with IIS Express |
| NuGet dependency | QRCoder 1.4.3 |

## Application Scope

The application is organized around exactly 30 ASPX pages. Master pages are layout files and are not counted as feature pages.

| Section | Count | Examples |
| --- | ---: | --- |
| Public/Auth | 12 | Home, About, Contact, FAQ, Modules, Articles, Resources, Register, Login |
| Learner | 12 | Dashboard, Profile, Progress, Quiz, Mood Tracker, Journal, Forum, Assignment |
| Admin | 6 | Dashboard, Users, Modules, Content, Quizzes, Feedback |

## Project Structure

```text
mindwell-webapp/
  LICENSE
  README.md
  MindWell.sln
  MindWell/
    Admin/        Administrator pages and admin master page
    Auth/         Register, login, and password-reset request pages
    Content/      Main stylesheet and visual design system
    Core/         Database, security, password, MFA, and shared service helpers
    Public/       Anonymous/public pages and public master page
    Scripts/      Small progressive-enhancement JavaScript
    SQL/          Base schema and seed SQL scripts
    User/         Authenticated learner pages and user master page
    Default.aspx  Home page
    Global.asax   Application startup
    Web.config    Application configuration
```

## Prerequisites

- Windows
- Visual Studio 2019 or Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- SQL Server Express LocalDB
- NuGet package restore enabled in Visual Studio

## Run Locally

1. Clone the repository.
2. Open `MindWell.sln` in Visual Studio.
3. Restore NuGet packages if Visual Studio prompts you.
4. Select `MindWell` as the startup project if needed.
5. Run `Build > Clean Solution`.
6. Run `Build > Rebuild Solution`.
7. Start the project with IIS Express.
8. Open `http://localhost:53820/Default.aspx` if the browser does not open automatically.

After the first run, Visual Studio and IIS Express may create local folders such as `.vs/`, `MindWell/bin/`, `MindWell/obj/`, `packages/`, and `MindWell/App_Data/`. These are intentionally ignored by Git.

## Database Behavior

The application uses SQL Server LocalDB with this runtime pattern:

```text
Data Source=(LocalDB)\MSSQLLocalDB
AttachDbFilename=|DataDirectory|\MindWellDB.mdf
Initial Catalog=MindWellDB
Integrated Security=true
```

On startup, `MindWell/Core/DatabaseInitializer.cs` creates the database when it is missing, applies schema setup, adds supplemental runtime tables, and seeds demo learning content.

Tracked database-related source files:

- `MindWell/SQL/MindWellDB_Schema.sql`
- `MindWell/SQL/MindWellDB_Seed.sql`
- `MindWell/Core/DatabaseInitializer.cs`

Ignored local runtime files:

- `MindWell/App_Data/*.mdf`
- `MindWell/App_Data/*.ldf`
- `MindWell/App_Data/*.log`

## Security Notes

MindWell includes coursework-level security controls appropriate for a local academic demo:

- Parameterized SQL helper methods.
- PBKDF2-style password hashing and verification.
- ASP.NET Forms Authentication and session checks.
- Role-based learner/admin page guards.
- CSRF token rendering and validation.
- Output encoding for rendered user/database content.
- Security response headers in `Web.config`.
- Optional TOTP multi-factor authentication support.

Production deployment would still require additional hardening, including HTTPS-only cookies, production secret management, deployment-specific Content Security Policy review, logging review, accessibility testing, and a full security audit.

## Limitations

- This is a Windows/Visual Studio/.NET Framework Web Forms project.
- GitHub Pages cannot host this application because it requires an ASP.NET server runtime.
- The included database setup is intended for local LocalDB development and coursework demonstration.
- The project should not be used to collect real medical, mental-health, or crisis-support data.

## Dependency License

This project uses [QRCoder](https://github.com/codebude/QRCoder), which is licensed under the MIT License.

## License

Copyright (C) 2026 Sazid R Khan

This project is licensed under the [GNU Affero General Public License v3.0](https://github.com/sazidrkhan/mindwell-webapp/blob/main/LICENSE).
