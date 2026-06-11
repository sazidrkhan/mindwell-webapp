# MindWell WebApp

MindWell is an ASP.NET Web Forms mental-health learning platform built as an academic web application project. It combines public educational content, authenticated learner tools, and administrator management pages in a classic .NET Framework application.

> MindWell is a coursework/demo application. It is not medical advice, crisis support, or a production healthcare system. Do not store real private health information in this project.

## Features

- Public learning pages for modules, article previews, resources, FAQ, contact, registration, login, and password-reset requests.
- Learner dashboard with profile management, password changes, learning progress, quizzes, mood tracking, mood history, reflection journal, forum posts, and activity submissions.
- Admin dashboard for managing users, modules, learning content, quizzes, contact/feedback records, and demo data quality.
- Automatic LocalDB database creation, schema setup, migrations, and seeded learning content on application startup.
- Authentication with ASP.NET Forms Authentication, session checks, role-based authorization, password hashing, CSRF validation, and optional TOTP multi-factor authentication support.
- Calm & Organic visual design using custom CSS, responsive layouts, light/dark theme support, and small vanilla JavaScript enhancements.

## Tech Stack

- ASP.NET Web Forms
- .NET Framework 4.8
- C# code-behind
- SQL Server LocalDB
- System.Data.SqlClient
- Visual Studio and IIS Express
- HTML, CSS, and vanilla JavaScript
- QRCoder 1.4.3 for authenticator QR code generation

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

## Page Scope

The application is organized around exactly 30 ASPX pages:

- 12 public/auth pages
- 12 authenticated learner pages
- 6 administrator pages

Master pages are layout files and are not counted as ASPX feature pages.

## Prerequisites

- Windows
- Visual Studio 2019 or Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- SQL Server Express LocalDB
- NuGet package restore enabled in Visual Studio

GitHub Pages cannot host this project because ASP.NET Web Forms requires a Windows/.NET Framework server runtime.

## Run Locally

1. Clone the repository.
2. Open `MindWell.sln` in Visual Studio.
3. Restore NuGet packages if Visual Studio prompts you.
4. Select `MindWell` as the startup project if needed.
5. Run `Build > Clean Solution`.
6. Run `Build > Rebuild Solution`.
7. Start the project with IIS Express.
8. Open `http://localhost:53820/Default.aspx` if the browser does not open automatically.

On first startup, the application creates and initializes a local SQL Server LocalDB database under `MindWell/App_Data`. Database files and runtime logs are intentionally ignored by Git.

## Database

The runtime connection string uses LocalDB with `AttachDbFilename=|DataDirectory|\MindWellDB.mdf`. The startup initializer creates the database when it is missing, applies schema setup, adds supplemental runtime tables, and seeds demo learning content.

Tracked database-related source files:

- `MindWell/SQL/MindWellDB_Schema.sql`
- `MindWell/SQL/MindWellDB_Seed.sql`
- `MindWell/Core/DatabaseInitializer.cs`

Ignored local runtime files:

- `MindWell/App_Data/*.mdf`
- `MindWell/App_Data/*.ldf`
- `MindWell/App_Data/*.log`

## Security Notes

This project includes coursework-level security controls such as parameterized SQL helpers, password hashing, output encoding, CSRF token validation, role-based page guards, security headers, and optional TOTP verification.

Production deployment would still require additional hardening, including HTTPS-only cookies, production secret management, deployment-specific Content Security Policy review, logging review, accessibility testing, and a full security audit.

## Dependency License

This project uses QRCoder, which is licensed under the MIT License.

## License

Copyright (C) 2026 Sazid R Khan

This project is licensed under the GNU Affero General Public License v3.0. See the `LICENSE` file for details.
