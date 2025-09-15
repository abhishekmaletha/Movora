# Movora UI

A React TypeScript application for searching and discovering movies and TV shows using FlexiSearch.

## Features

- 🔍 Intelligent search with FlexiSearch integration
- 🎬 Movie and TV show discovery
- ⭐ Rating display with color coding
- 📱 Responsive design
- ♿ Accessibility focused
- 🌙 Dark theme UI
- 💾 Search query persistence
- ⚡ Loading states and error handling

## Getting Started

### Prerequisites

- Node.js 16+ 
- npm or yarn

### Installation

1. Install dependencies:
```bash
npm install
```

2. Copy environment configuration:
```bash
cp .env.example .env
```

3. Update the API base URL in `.env` if needed:
```
REACT_APP_API_BASE_URL=https://localhost:7001
```

4. Start the development server:
```bash
npm start
```

The app will open at [http://localhost:3000](http://localhost:3000).

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── SearchBar.tsx   # Search input with accessibility
│   ├── Rating.tsx      # Star rating display
│   ├── MovieCard.tsx   # Individual result card
│   └── MovieGrid.tsx   # Results grid with loading states
├── hooks/              # Custom React hooks
│   └── useFlexiSearch.ts # Search state management
├── lib/                # Utilities and API clients
│   └── api.ts          # API client for FlexiSearch
├── pages/              # Page components
│   └── FlexiSearchPage.tsx # Main search page
├── types.ts            # TypeScript type definitions
├── App.tsx             # Root component
├── index.tsx           # App entry point
└── index.css           # Global styles
```

## API Integration

The app integrates with the Movora backend FlexiSearch endpoint:

- **Endpoint**: `POST /api/search/flexi`
- **Request**: `{ "query": "search terms" }`
- **Response**: `{ "results": [...], "traceId": "..." }`

## Accessibility Features

- Semantic HTML with proper ARIA labels
- Keyboard navigation support
- Screen reader friendly
- Focus management
- High contrast mode support
- Reduced motion support

## Build for Production

```bash
npm run build
```

This creates an optimized production build in the `build` folder.

## Environment Variables

- `REACT_APP_API_BASE_URL`: Backend API base URL (default: https://localhost:7001)

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+
