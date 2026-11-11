# Nordia-Leave

Small internal tool: employees request leave, managers approve or reject, everyone sees balance and history.

- Backend: C# / ASP.NET Core minimal API targeting .NET 8, with Entity Framework Core and Oracle's EF Core provider for Oracle Database access.
- Frontend: React 18 with JavaScript and JSX, built and served by Vite.
- Tests: xUnit for backend service rules; Vitest, React Testing Library, and jsdom for frontend component tests.


nordia-leave/
├─ backend/
│  ├─ NordiaLeave.sln
│  ├─ LeaveApi/
│  │  ├─ Endpoints/      ← HTTP route registration
│  │  ├─ Services/       ← business rules and use cases
│  │  ├─ Models/
│  │  │  ├─ Domain/      ← core business entities and exceptions
│  │  │  ├─ Requests/    ← API request bodies
│  │  │  └─ Responses/   ← API response models
│  │  ├─ Data/           ← Oracle mapping and schema upgrade script
│  │  └─ Program.cs      ← app configuration and middleware
│  └─ LeaveApi.Tests/    ← xUnit tests
└─ frontend/             ← React app
   └─ src/
      ├─ app/            ← app-level state and data loading
      ├─ features/
      │  ├─ employee/    ← balance, request form, and leave history
      │  └─ manager/     ← pending request review
      ├─ services/       ← backend API client
      ├─ shared/         ← reusable helpers
      ├─ main.jsx        ← React entry point
      └─ styles/         ← global styles
   └─ tests/
      └─ features/
         ├─ employee/    ← leave form tests
         └─ manager/     ← pending request tests   



 1. Prerequisites

| Tool | Version | Check |
|---|---|---|
| Visual Studio 2022 (17.8+) or 2026 with the ASP.NET and web development workload | – | Visual Studio Installer |
| .NET SDK | 8.0 or newer | `dotnet --list-sdks` |
| Node.js | 20 or newer | `node -v` |
| VS Code | any | – |


 2. Run the backend (Visual Studio)

- Set LeaveApi as startup project (right-click → Set as Startup Project) and pick the http profile in the green-arrow dropdown.
- Press Ctrl+F5 (run without debugging). A browser opens on <http://localhost:5080/api/employees> and reads employees from Oracle. Leave it running.


 3. Run the frontend (VS Code)

- In VS Code: *File → Open Folder…* → `nordia-leave/frontend`.
- Open a terminal (Ctrl+`) and run:
   npm install
   npm run dev
- Open <http://localhost:5173>.


 4. Run the tests

- Backend: in Visual Studio, Test → Test Explorer → Run All Tests, or `cd backend && dotnet test`
- Frontend: `cd frontend && npm test`

There are 15 backend service tests and 9 frontend component tests (24 test cases total). The backend tests use fixture data and do not require a live Oracle database. They test business rules rather than HTTP endpoints or Oracle integration.


 5. Database storage

- The API uses Entity Framework Core with Oracle's provider to read employees and leave requests from `EMPLOYEES` and `LEAVES`; 
- new requests and manager decisions are saved to Oracle. 
- Each API operation loads the current employee and leave data, and mutations are committed transactionally. 

