# Employee Working Tracker

A full-stack web application for managing employees, projects, work tasks, daily work logs, approvals, and reports.

## Tech Stack

**Frontend**
- Angular
- TypeScript
- HTML5 and SCSS
- Reactive Forms
- Angular Signals
- REST API Integration

**Backend**
- ASP.NET Core Web API (.NET 10)
- C#
- Entity Framework Core
- JWT Authentication
- Role-Based Authorization

**Database**
- Microsoft SQL Server

**Version Control**
- Git and GitHub

## Key Features

### Authentication and Authorization
- User login using JWT authentication
- Role-based access for Administrator, Manager, and Employee
- Change password functionality

### Employee Management
- Employee CRUD operations
- Department and designation management
- Employee–manager relationships
- Employee profile management

### Project and Task Management
- Project management
- Project member assignment
- Work task management
- Project file upload functionality

### Daily Work Tracking
- Daily work log management
- Hours worked and progress tracking
- Work log submission and approval workflows

### Dashboard and Reports
- Dashboard summary statistics
- Employee work summary reports
- Role-based report access

## Project Structure

```text
EmployeeWorkingTracker/
├── backend/
│   └── EmployeeTracker.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Migrations/
│       ├── Models/
│       └── Program.cs
├── frontend/
│   └── employee-tracker-ui/
│       ├── src/
│       │   └── app/
│       ├── angular.json
│       └── package.json
└── README.md
```

## How to Run Locally

### Prerequisites

- Node.js and npm
- Angular CLI
- .NET 10 SDK
- SQL Server
- Git

### Backend Setup

```powershell
cd backend/EmployeeTracker.Api
dotnet restore
dotnet ef database update
dotnet run
```

Configure your SQL Server connection string and JWT settings locally before starting the API.

Backend development URL:

`http://localhost:5027`

### Frontend Setup

```powershell
cd frontend/employee-tracker-ui
npm install
npm start
```

Frontend development URL:

`http://localhost:4200`

## Application Screenshots

Screenshots will be added for the login page, dashboards, employee management, projects, work logs, and reports.

## Future Enhancements

- Cloud deployment
- Automated unit and integration tests
- CI/CD pipeline
- Additional reporting features

## Source Code

[GitHub Repository](https://github.com/Indhujaasenthil/Employee-Working-Tracker)