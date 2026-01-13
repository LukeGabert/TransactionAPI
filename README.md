# Transaction Anomaly Detection API

A full-stack application for detecting suspicious financial transactions using AI-powered analysis. This project combines a .NET 8 Web API backend with a React TypeScript frontend to provide real-time transaction anomaly detection using OpenAI's GPT-4o-mini model.

## Features

- 🤖 **AI-Powered Anomaly Detection** - Uses OpenAI GPT-4o-mini to analyze transactions for suspicious patterns
- 📊 **Risk Assessment Dashboard** - Beautiful React dashboard to view and manage risk reports
- 📁 **CSV Data Generation** - Python script to generate realistic transaction data with built-in anomalies
- 🔍 **Real-Time Scanning** - Scan transactions in batches and get instant risk assessments
- ⚠️ **Risk Level Classification** - Automatic categorization into Low, Medium, and High risk levels
- 🛡️ **Mitigation Strategies** - AI-generated recommendations for each flagged transaction
- ✅ **Report Resolution** - Mark risk reports as resolved directly from the dashboard
- 💾 **SQLite Database** - Lightweight database for transaction and risk report storage

## Tech Stack

### Backend
- **.NET 8** - Web API framework
- **Entity Framework Core 8.0** - ORM for database operations
- **SQLite** - Lightweight database
- **OpenAI API** - GPT-4o-mini for anomaly detection
- **Minimal APIs** - Modern .NET API endpoints

### Frontend
- **React 19** - UI library
- **TypeScript** - Type-safe JavaScript
- **Vite** - Fast build tool and dev server
- **Tailwind CSS 3.4** - Utility-first CSS framework
- **Axios** - HTTP client for API calls
- **Lucide React** - Beautiful icon library

### Scripts
- **Python 3** - CSV data generation script

## Project Structure

```
TransactionAPI/
├── Data/
│   └── ApplicationDbContext.cs      # EF Core DbContext
├── Models/
│   ├── Transaction.cs                # Transaction entity model
│   └── RiskReport.cs                 # Risk report entity model
├── Services/
│   ├── AnomalyDetectionService.cs    # OpenAI API integration service
│   └── CsvImportService.cs           # CSV import service
├── Scripts/
│   ├── generate_transactions.py      # Python script to generate transaction CSV
│   └── transactions.csv              # Generated transaction data
├── client/                           # React frontend application
│   ├── src/
│   │   ├── components/
│   │   │   ├── AnomalyDashboard.tsx  # Main dashboard component
│   │   │   └── Toast.tsx             # Toast notification component
│   │   ├── services/
│   │   │   ├── api.ts                # Axios API client
│   │   │   ├── riskReportService.ts  # Risk report API service
│   │   │   └── transactionService.ts # Transaction API service
│   │   ├── types/
│   │   │   └── riskReport.ts         # TypeScript type definitions
│   │   └── hooks/
│   │       └── useApi.ts             # Custom API hook
│   └── package.json
├── Program.cs                        # Application entry point and API endpoints
├── appsettings.json                  # Application configuration
└── TransactionAPI.csproj             # Project file
```

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js** (v18 or higher) and **npm** - [Download here](https://nodejs.org/)
- **Python 3** (for CSV generation script) - [Download here](https://www.python.org/downloads/)
- **OpenAI API Key** - Get one from [OpenAI Platform](https://platform.openai.com/api-keys)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd TransactionAPI
```

### 2. Backend Setup

#### Install Dependencies

The .NET dependencies are automatically restored when you build the project, but you can explicitly restore them:

```bash
dotnet restore
```

#### Configure OpenAI API Key

Set your OpenAI API key using .NET user secrets (recommended for development):

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key-here"
```

Alternatively, you can set it in `appsettings.json` or `appsettings.Development.json`, but this is **not recommended** for production.

#### Generate Transaction Data (Optional)

If you want to generate new transaction data:

```bash
cd Scripts
python generate_transactions.py
```

This will create a `transactions.csv` file with 500 transactions, including approximately 5% anomalies. The script uses accurate merchant-to-category mappings (e.g., Netflix → Subscription, Shell → Gas, United Airlines → Travel).

#### Build the Backend

```bash
dotnet build
```

#### Run the Backend

```bash
dotnet run
```

The API will start on `http://localhost:5100` (check the console output for the actual port).

Swagger UI will be available at `http://localhost:5100/swagger` for API documentation and testing.

### 3. Frontend Setup

#### Navigate to Client Directory

```bash
cd client
```

#### Install Dependencies

```bash
npm install
```

#### Configure API URL (Optional)

The frontend is configured to connect to `http://localhost:5100/api` by default. If your backend runs on a different port, you can:

- Set the `VITE_API_URL` environment variable, or
- Modify `src/services/api.ts`

#### Run the Frontend Development Server

```bash
npm run dev
```

The frontend will start on `http://localhost:5173`.

### 4. Database Setup

The SQLite database (`TransactionDB.db`) is automatically created when you first run the application. No manual database setup is required.

## Configuration

### Backend Configuration

#### Database Connection

The default SQLite database connection string is in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TransactionDB.db"
  }
}
```

#### OpenAI API Key

The API key is configured via user secrets (recommended) or `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

**Important**: Never commit your API key to version control. Use user secrets for local development.

### Frontend Configuration

The frontend API URL can be configured via environment variable:

```bash
# Create a .env file in the client directory
VITE_API_URL=http://localhost:5100/api
```

## API Endpoints

### Risk Reports

- **GET** `/api/risk-reports` - Get all risk reports with transaction details
- **GET** `/api/risk-reports/{id}` - Get a specific risk report by ID
- **PUT** `/api/risk-reports/{id}/resolve` - Mark a risk report as resolved (deletes the report)

### Transactions

- **POST** `/api/transactions/scan` - Scan transactions for anomalies using AI
  - Automatically imports CSV data if database is empty
  - Analyzes transactions in batches of 50 sequentially
  - Each scan processes the next batch (50, then 100, then 150, etc.)
  - Returns the number of risk reports created and total transactions analyzed

- **GET** `/api/transactions/stats` - Get transaction statistics (total count)

## Usage

### 1. Generate Transaction Data

If you haven't already, generate transaction data:

```bash
cd Scripts
python generate_transactions.py
```

This creates `transactions.csv` with 500 transactions.

### 2. Start the Application

Start both backend and frontend:

**Terminal 1 (Backend):**
```bash
dotnet run
```

**Terminal 2 (Frontend):**
```bash
cd client
npm run dev
```

### 3. Scan Transactions

1. Open your browser to `http://localhost:5173`
2. Click the **"Scan Transactions"** button in the dashboard
3. The system will:
   - Import transactions from CSV if the database is empty (all 500 transactions)
   - Analyze the first 50 transactions using OpenAI
   - Create risk reports for suspicious transactions
4. View the results in the dashboard table
5. Click **"Scan Transactions"** again to analyze the next batch (transactions 51-100), and so on

### 4. View Risk Reports

The dashboard displays:
- **Statistics Cards** - Total transactions analyzed, high-risk alerts count, total resolved
- **Risk Level** - Color-coded badges (Low/Medium/High) with icons
- **Transaction Details** - Amount, merchant, category, location, timestamp
- **Detected Anomaly** - Description of the suspicious activity
- **AI-Recommended Mitigation** - Suggested action to take
- **Search Functionality** - Filter risk reports by transaction ID, merchant, category, etc.
- **Resolve Button** - Mark the report as resolved

### 5. Resolve Risk Reports

Click the **"Resolve"** button on any risk report to mark it as resolved. The report will be removed from the active list.

## Development

### Key Components

#### Backend Services

- **AnomalyDetectionService** (`Services/AnomalyDetectionService.cs`)
  - Handles OpenAI API integration
  - Formats transactions for AI analysis
  - Parses AI responses into risk assessments
  - Saves risk reports to the database

- **CsvImportService** (`Services/CsvImportService.cs`)
  - Imports transaction data from CSV files
  - Validates and parses CSV data
  - Handles duplicate transactions

#### Frontend Components

- **AnomalyDashboard** (`client/src/components/AnomalyDashboard.tsx`)
  - Main dashboard component
  - Displays risk reports in a table
  - Handles scanning and resolution actions
  - Manages loading and error states

- **Toast** (`client/src/components/Toast.tsx`)
  - Toast notification component
  - Shows success/error messages

### Adding New Features

1. **Backend**: Add new endpoints in `Program.cs` or create new services in the `Services/` folder
2. **Frontend**: Add new components in `client/src/components/` and services in `client/src/services/`
3. **Database**: Add new models in `Models/` and update `ApplicationDbContext.cs`

### Build for Production

**Backend:**
```bash
dotnet publish -c Release
```

**Frontend:**
```bash
cd client
npm run build
```

The production build will be in `client/dist/`.

## Troubleshooting

### OpenAI API Rate Limits

If you see a "429 Too Many Requests" error:

- **Cause**: You've exceeded OpenAI's rate limit
- **Solution**: Wait a few minutes and try again, or check your OpenAI API quota

### API Key Not Found

If you see an "API key is not configured" error:

- **Solution**: Ensure you've set the API key using user secrets:
  ```bash
  dotnet user-secrets set "OpenAI:ApiKey" "your-key-here"
  ```

### Database Errors

If you encounter database-related errors:

- **Solution**: Delete `TransactionDB.db` and let the application recreate it on startup

### Frontend Can't Connect to Backend

If the frontend shows "Failed to fetch":

- **Check**: Ensure the backend is running on the correct port
- **Check**: Verify CORS is configured (should allow `http://localhost:5173`)
- **Check**: Verify the API URL in `client/src/services/api.ts`

### CSV Import Issues

If transactions aren't importing:

- **Check**: Ensure `Scripts/transactions.csv` exists
- **Check**: Verify the CSV file format matches the expected structure
- **Check**: Check backend logs for import errors

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

## Support

For issues and questions, please open an issue in the repository.
