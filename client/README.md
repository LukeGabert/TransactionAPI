# Transaction API Client

A React + TypeScript client application built with Vite, Tailwind CSS, Axios, and Lucide React icons.

## Features

- ⚡️ **Vite** - Fast build tool and dev server
- ⚛️ **React 19** - Latest React with TypeScript
- 🎨 **Tailwind CSS 4** - Utility-first CSS framework
- 🌐 **Axios** - HTTP client for API calls
- 🎯 **Lucide React** - Beautiful icon library
- 📁 **Organized Structure** - Components, services, and hooks folders

## Project Structure

```
src/
├── components/     # React components
├── hooks/         # Custom React hooks
├── services/      # API services and utilities
├── App.tsx        # Main application component
├── main.tsx       # Application entry point
└── index.css      # Global styles with Tailwind
```

## Getting Started

### Install Dependencies

```bash
npm install
```

### Development Server

```bash
npm run dev
```

### Build for Production

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Configuration

### API Base URL

Configure the API base URL by setting the `VITE_API_URL` environment variable or modifying `src/services/api.ts`.

Default: `http://localhost:5000/api`

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Dependencies

- **react** ^19.2.0
- **react-dom** ^19.2.0
- **axios** ^1.13.2 - HTTP client
- **lucide-react** ^0.562.0 - Icons

## Development Dependencies

- **vite** ^7.2.4 - Build tool
- **typescript** ~5.9.3 - Type safety
- **tailwindcss** ^4.1.18 - CSS framework
- **@tailwindcss/postcss** ^4.1.18 - PostCSS plugin for Tailwind
- **autoprefixer** ^10.4.23 - CSS vendor prefixing
