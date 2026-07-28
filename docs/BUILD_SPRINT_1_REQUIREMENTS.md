# Capstone Build Sprint 1: Core Implementation (Gate)

## Objective

Deliver a complete, locally running full-stack .NET application within 6 hours, based on the capstone specification created in the Planning & Spec gate. This is a high-pressure sprint simulating a real-world project deadline.

## Time Constraint & Prioritisation

- You have exactly 360 minutes from the start. Use a timer.
- The task scope is intentionally larger than the available time.
- Prioritise the core user journey and a working admin panel over peripheral features.
- If a feature would cost too much time, cut scope, hardcode stubs, or simplify—but ensure the application is fully functional end-to-end for the primary scenarios.

## Mandatory Features

Regardless of the chosen domain, the final application must include all of the following.

### 1. Authentication & Authorisation

- JWT-based login and registration for end users.
- Role-based access control with at least two roles, such as `User` and `Admin`.
- Protected frontend routes and API endpoints that enforce role checks.

### 2. Full CRUD for Main Entities

- At least two core domain entities, such as Products and Categories, Events and Registrations, or Tasks and Projects.
- Backend APIs with complete Create, Read, Update, and Delete operations.
- Frontend UI for listing, creating, editing, and deleting instances.

### 3. Local File Upload

- Allow users to upload files, such as images or documents, to the server.
- Store uploaded files locally, for example in an `uploads` directory.
- Serve uploaded files through the API with appropriate access controls.
- Display uploaded files in the frontend.

### 4. Third-Party API Integration

- Consume at least one real external API, such as a weather API, map geocoding service, currency converter, or another free public API.
- Use the external data meaningfully within the application, such as showing weather for an event location or converting prices.
- Implement resilient API calls with timeout handling, error handling, and appropriate caching.

### 5. Admin Dashboard with Role Management

- Provide a separate protected area for administrators.
- Allow administrators to list all users, change roles, and disable accounts.
- Provide a summary dashboard showing key application metrics, such as total orders or new registrations, using live database data.

### 6. AI-Driven Tests (≥80% Coverage)

- Use AI tools to generate unit and integration tests for all critical paths.
- Achieve at least 80% backend code coverage. Generated migration files may be excluded.
- Provide a coverage report as evidence, for example:

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### 7. UAT Plan (Simulated)

- Write a brief UAT script covering the main happy paths and edge cases.
- Execute the plan manually.
- Document each result as pass or fail.
- Commit the completed UAT report to the repository.

### 8. Docker Deployment

Use AI to generate a Dockerfile for the backend and frontend, or a single multi-stage build where applicable, and a `docker-compose.yml` that starts:

- The .NET backend API.
- A SQL Server container with a persistent volume.
- The frontend, unless it is served by the backend.

The application must:

- Start successfully with a single `docker-compose up` command.
- Be accessible at `http://localhost:{PORT}`.
- Support login, CRUD, file upload, and admin functionality end-to-end in the Docker environment.

## Deliverables

### 1. Git Repository

- The repository may be public or private but must be accessible to the reviewer.
- Use clean, frequent commits with meaningful AI-generated commit messages.
- Do not create one giant final-dump commit; the history must reflect iterative development.
- Include a `README.md` with clear setup instructions, including how to run `docker-compose up` and which environment variables are required.

### 2. Test Coverage Report

- Commit or attach a generated coverage report, such as a `coveragereport/` folder.

### 3. Prompt Log (Top 20)

- Provide a Markdown or text file containing the 20 most impactful AI prompts used.
- Group prompts by category, such as code generation, debugging, test generation, and Docker.
- Briefly explain why each prompt was critical.

### 4. Loom Video 1: Technical Walkthrough (Maximum 15 Minutes)

The technical walkthrough must:

- Explain the architecture and important design decisions.
- Walk through the folder structure and database schema.
- Explain the third-party API integration.
- Show the test suite and coverage report.

### 5. Loom Video 2: Non-Technical Demo (Maximum 10 Minutes)

The non-technical demonstration must:

- Show the application from a user’s perspective.
- Demonstrate registration and login.
- Perform a core CRUD flow.
- Upload and display a file.
- Show the third-party data.
- Switch to the admin perspective.
- Demonstrate role management and the admin dashboard.
- Avoid showing code and focus on features and user experience.

## Assessment Criteria

### Completeness

- Are all mandatory features implemented and functioning in Docker?
- Is the core user journey intact?

### Code Quality & AI Usage

- Is the code well structured?
- Does the repository contain meaningful commits?
- Is there evidence of effective AI usage through the prompt log and test coverage?

### Professional Presentation

- Are the Loom videos clear and well organised?
- Do the videos cover all required content?

### Time Management

- Is there evidence of prioritisation under time pressure, such as scope cuts documented in commit messages or the README?

## Getting Started

1. Review the approved capstone specification from the Planning & Spec gate.
2. Set a timer for 6 hours.
3. Treat AI tools as junior developer pair-programming partners.
4. Keep a running log of prompts.
5. Commit after every meaningful chunk of work.
6. When the timer ends, stop and ensure all artifacts are submitted.

Good luck—this sprint simulates a real deadline. Embrace the pressure and let AI amplify your productivity.

## User Stories

These stories describe the core behaviours every capstone project must satisfy. Tailor the generic items to the chosen domain.

1. As a new visitor, I want to register an account and log in so that I can access protected features.
2. As an authenticated user, I want to create, view, update, and delete my own items so that I can manage my data.
3. As an authenticated user, I want to upload files and see them in the application so that I can attach images or documents.
4. As an admin, I want to view a dashboard summary, such as total users and recent activity, so that I can monitor the system at a glance.
5. As an admin, I want to list all users, change their roles, and disable accounts so that I can manage access.
6. As a user, I want to see relevant third-party data, such as weather, location, or prices, integrated into the application so that the information is enriched.
7. As a developer, I want the application to run locally with a single Docker Compose command so that it is easy to set up and test.

## Acceptance Criteria Checklist

Mark each item as completed. Every box must be ticked for the Gate to pass.

- [ ] The repository includes a clean Git history with at least 15 meaningful commits and no single giant commit.
- [ ] `docker-compose up` starts the full stack without errors, and the application is accessible at `http://localhost:{PORT}`.
- [ ] Registration and JWT-based login work correctly; protected routes return `401 Unauthorized` when a token is missing or invalid.
- [ ] Role-based access control is enforced; admin-only endpoints return `403 Forbidden` for non-admin users.
- [ ] Full CRUD operations for at least two domain entities work end-to-end; the API returns correct data, and the frontend supports listing, creating, editing, and deleting records.
- [ ] A user can upload a file through the frontend; it is stored locally, retrieved through the API, and displayed in the application.
- [ ] The admin dashboard lists all users and allows role changes, confirmed through the UI or an API test.
- [ ] The admin dashboard includes summary metrics populated with live database data.
- [ ] The application fetches data from an external service, uses it meaningfully, and handles errors gracefully.
- [ ] Backend test coverage is at least 80% as measured by Coverlet, and the coverage report is included in the submission.
- [ ] A committed UAT script contains at least five test scenarios, and execution results are documented as pass or fail.
- [ ] A categorised top-20 prompt log is present and explains the impact of each prompt.
- [ ] Loom Video 1 is no longer than 15 minutes and covers architecture, code structure, key decisions, tests, and coverage.
- [ ] Loom Video 2 is no longer than 10 minutes, demonstrates the user and admin perspectives, and does not show code.
