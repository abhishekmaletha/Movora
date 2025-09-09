-- Enable PostgreSQL Extensions for Movora Database
-- This script should be run first to enable required extensions

-- Enable UUID extension for generating UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable pgvector extension for vector similarity search
CREATE EXTENSION IF NOT EXISTS vector;

-- Enable PostgreSQL crypto extension for security functions
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Enable full text search extension (if not already enabled)
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- Create a function to update the updated_date column automatically
CREATE OR REPLACE FUNCTION update_updated_date_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_date = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language plpgsql;

-- Comment to document the script
COMMENT ON FUNCTION update_updated_date_column() IS 'Trigger function to automatically update updated_date column';
