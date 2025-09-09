# Movora Database Project

This project contains all SQL scripts and database-related files for the Movora movie search platform.

## Project Structure

```
Movora.Database/
├── Scripts/
│   ├── Setup/           # Database setup scripts (run in order)
│   │   ├── 001_EnableExtensions.sql     # Enable PostgreSQL extensions
│   │   ├── 002_CreateTables.sql         # Create all database tables
│   │   └── 003_CreateTriggers.sql       # Create triggers for auto-updates
│   ├── Indexes/         # Performance indexes
│   │   └── 001_CreateIndexes.sql        # All database indexes
│   ├── Functions/       # PostgreSQL functions
│   │   └── 001_VectorSearchFunctions.sql # Vector similarity search functions
│   └── Seeds/           # Sample data for development
│       └── 001_SeedData.sql             # Development seed data
└── README.md
```

## Prerequisites

- PostgreSQL 14+ with the following extensions:
  - `vector` (pgvector) - For similarity search
  - `uuid-ossp` - For UUID generation
  - `pgcrypto` - For cryptographic functions
  - `unaccent` - For full-text search

## Setup Instructions

### 1. Install pgvector Extension

Follow the [pgvector installation guide](https://github.com/pgvector/pgvector) for your PostgreSQL installation.

### 2. Create Database

```sql
CREATE DATABASE movora_db;
\c movora_db;
```

### 3. Run Scripts in Order

Execute the scripts in the following order:

1. **Setup Scripts** (required, run in order):
   ```bash
   psql -d movora_db -f Scripts/Setup/001_EnableExtensions.sql
   psql -d movora_db -f Scripts/Setup/002_CreateTables.sql
   psql -d movora_db -f Scripts/Setup/003_CreateTriggers.sql
   ```

2. **Indexes** (recommended for performance):
   ```bash
   psql -d movora_db -f Scripts/Indexes/001_CreateIndexes.sql
   ```

3. **Functions** (required for semantic search):
   ```bash
   psql -d movora_db -f Scripts/Functions/001_VectorSearchFunctions.sql
   ```

4. **Seed Data** (optional, for development):
   ```bash
   psql -d movora_db -f Scripts/Seeds/001_SeedData.sql
   ```

## Database Schema

### Core Tables

- **users** - User accounts and preferences
- **movies** - Movie content with TMDB integration
- **series** - TV series content with TMDB integration
- **episodes** - Individual episodes for series
- **user_watched** - User viewing history and progress
- **reviews** - User reviews and ratings
- **comments** - Comments on reviews
- **search_history** - User search queries and results
- **recommendations** - AI-generated recommendations

### Key Features

- **Vector Search**: Uses pgvector for semantic similarity search
- **Full-Text Search**: PostgreSQL full-text search on titles and descriptions
- **JSONB Storage**: Flexible storage for preferences and search results
- **Automatic Timestamps**: Auto-updating created_date and updated_date fields
- **Comprehensive Indexing**: Optimized for common query patterns

## Vector Search Functions

The database includes specialized functions for similarity search:

- `find_similar_movies(embedding, threshold, limit)` - Find similar movies
- `find_similar_series(embedding, threshold, limit)` - Find similar series
- `find_similar_content(embedding, threshold, limit)` - Find similar content (movies + series)
- `get_user_preference_vector(user_id)` - Generate user preference vector

## Environment Configuration

For Entity Framework integration, ensure your connection string includes:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=movora_db;Username=your_user;Password=your_password"
  }
}
```

## Development Notes

- All tables use UUIDs as primary keys for better scalability
- Vector embeddings are designed for OpenAI's text-embedding-ada-002 (1536 dimensions)
- The database is optimized for PostgreSQL-specific features
- Seed data includes real TMDB IDs for API integration testing

## Migrations

This project uses raw SQL scripts instead of Entity Framework migrations for:
- Better control over PostgreSQL-specific features
- Easier review of database changes
- Support for complex functions and indexes
- Vector extension compatibility

For production deployments, consider using migration tools like Flyway or Liquibase for script versioning and deployment tracking.
