-- Create Performance Indexes for Movora Database
-- Run after all tables are created

-- Indexes for users table
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_google_id ON users(google_id);
CREATE INDEX idx_users_created_date ON users(created_date);

-- Indexes for movies table
CREATE INDEX idx_movies_tmdb_id ON movies(tmdb_id);
CREATE INDEX idx_movies_title ON movies(title);
CREATE INDEX idx_movies_release_date ON movies(release_date);
CREATE INDEX idx_movies_genres ON movies USING GIN(genres);
CREATE INDEX idx_movies_created_date ON movies(created_date);

-- Indexes for series table
CREATE INDEX idx_series_tmdb_id ON series(tmdb_id);
CREATE INDEX idx_series_title ON series(title);
CREATE INDEX idx_series_first_air_date ON series(first_air_date);
CREATE INDEX idx_series_genres ON series USING GIN(genres);
CREATE INDEX idx_series_created_date ON series(created_date);

-- Indexes for episodes table
CREATE INDEX idx_episodes_series_id ON episodes(series_id);
CREATE INDEX idx_episodes_season_episode ON episodes(series_id, season_number, episode_number);
CREATE INDEX idx_episodes_air_date ON episodes(air_date);

-- Indexes for user_watched table
CREATE INDEX idx_user_watched_user_id ON user_watched(user_id);
CREATE INDEX idx_user_watched_content ON user_watched(content_id, content_type);
CREATE INDEX idx_user_watched_watched_date ON user_watched(watched_date);
CREATE INDEX idx_user_watched_user_content ON user_watched(user_id, content_id, content_type);

-- Indexes for reviews table
CREATE INDEX idx_reviews_user_id ON reviews(user_id);
CREATE INDEX idx_reviews_content ON reviews(content_id, content_type);
CREATE INDEX idx_reviews_rating ON reviews(rating);
CREATE INDEX idx_reviews_created_date ON reviews(created_date);
CREATE INDEX idx_reviews_user_content ON reviews(user_id, content_id, content_type);

-- Indexes for comments table
CREATE INDEX idx_comments_user_id ON comments(user_id);
CREATE INDEX idx_comments_review_id ON comments(review_id);
CREATE INDEX idx_comments_created_date ON comments(created_date);
CREATE INDEX idx_comments_is_deleted ON comments(is_deleted);

-- Indexes for search_history table
CREATE INDEX idx_search_history_user_id ON search_history(user_id);
CREATE INDEX idx_search_history_created_date ON search_history(created_date);
CREATE INDEX idx_search_history_query ON search_history(query);
CREATE INDEX idx_search_history_search_type ON search_history(search_type);

-- Indexes for recommendations table
CREATE INDEX idx_recommendations_user_id ON recommendations(user_id);
CREATE INDEX idx_recommendations_content ON recommendations(content_id, content_type);
CREATE INDEX idx_recommendations_score ON recommendations(score);
CREATE INDEX idx_recommendations_created_date ON recommendations(created_date);
CREATE INDEX idx_recommendations_user_content ON recommendations(user_id, content_id, content_type);
CREATE INDEX idx_recommendations_type ON recommendations(recommendation_type);

-- Vector similarity search indexes (for pgvector)
CREATE INDEX idx_movies_embedding ON movies USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_series_embedding ON series USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- Full text search indexes
CREATE INDEX idx_movies_title_fts ON movies USING GIN(to_tsvector('english', title));
CREATE INDEX idx_movies_description_fts ON movies USING GIN(to_tsvector('english', description));
CREATE INDEX idx_series_title_fts ON series USING GIN(to_tsvector('english', title));
CREATE INDEX idx_series_description_fts ON series USING GIN(to_tsvector('english', description));

-- Composite indexes for common query patterns
CREATE INDEX idx_user_watched_user_date ON user_watched(user_id, watched_date DESC);
CREATE INDEX idx_reviews_content_rating ON reviews(content_id, content_type, rating DESC);
CREATE INDEX idx_recommendations_user_score ON recommendations(user_id, score DESC) WHERE is_dismissed = FALSE;
