-- Create Core Tables for Movora Movie Platform
-- Run after 001_EnableExtensions.sql

-- Create enum types
CREATE TYPE content_type AS ENUM ('Movie', 'Series');

-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(320) NOT NULL UNIQUE,
    google_id VARCHAR(100) UNIQUE,
    preferences JSONB,
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Movies table
CREATE TABLE movies (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tmdb_id INTEGER NOT NULL UNIQUE,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    genres TEXT[],
    release_date DATE,
    poster_url VARCHAR(1000),
    embedding VECTOR(1536), -- OpenAI embedding dimension
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Series table
CREATE TABLE series (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tmdb_id INTEGER NOT NULL UNIQUE,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    genres TEXT[],
    first_air_date DATE,
    poster_url VARCHAR(1000),
    embedding VECTOR(1536), -- OpenAI embedding dimension
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Episodes table
CREATE TABLE episodes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    series_id UUID NOT NULL REFERENCES series(id) ON DELETE CASCADE,
    season_number INTEGER NOT NULL,
    episode_number INTEGER NOT NULL,
    title VARCHAR(500) NOT NULL,
    air_date DATE,
    description TEXT,
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(series_id, season_number, episode_number)
);

-- User watched content tracking
CREATE TABLE user_watched (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content_id UUID NOT NULL,
    content_type content_type NOT NULL,
    watched_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    episodes_watched INTEGER, -- For series tracking
    progress_percentage DECIMAL(5,2) CHECK (progress_percentage >= 0 AND progress_percentage <= 100),
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, content_id, content_type)
);

-- Reviews table
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content_id UUID NOT NULL,
    content_type content_type NOT NULL,
    rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 10),
    review_text TEXT,
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_spoiler BOOLEAN DEFAULT FALSE,
    helpfulness_score INTEGER DEFAULT 0,
    UNIQUE(user_id, content_id, content_type)
);

-- Comments table
CREATE TABLE comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    comment_text TEXT NOT NULL,
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Search history table
CREATE TABLE search_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    query VARCHAR(1000) NOT NULL,
    results JSONB, -- Store search results as JSON
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    search_type VARCHAR(50), -- e.g., "text", "semantic", "advanced"
    result_count INTEGER DEFAULT 0
);

-- Recommendations table
CREATE TABLE recommendations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content_id UUID NOT NULL,
    content_type content_type NOT NULL,
    score DECIMAL(5,4) NOT NULL CHECK (score >= 0 AND score <= 1),
    reasoning TEXT, -- AI-generated explanation
    recommendation_type VARCHAR(50), -- e.g., "collaborative", "content-based", "hybrid"
    created_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    viewed_date TIMESTAMP WITH TIME ZONE,
    is_dismissed BOOLEAN DEFAULT FALSE,
    UNIQUE(user_id, content_id, content_type)
);
