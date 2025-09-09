-- Create Triggers for Automatic updated_date Updates
-- Run after 002_CreateTables.sql

-- Trigger for users table
CREATE TRIGGER trigger_users_updated_date
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for movies table
CREATE TRIGGER trigger_movies_updated_date
    BEFORE UPDATE ON movies
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for series table
CREATE TRIGGER trigger_series_updated_date
    BEFORE UPDATE ON series
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for episodes table
CREATE TRIGGER trigger_episodes_updated_date
    BEFORE UPDATE ON episodes
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for user_watched table
CREATE TRIGGER trigger_user_watched_updated_date
    BEFORE UPDATE ON user_watched
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for reviews table
CREATE TRIGGER trigger_reviews_updated_date
    BEFORE UPDATE ON reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for comments table
CREATE TRIGGER trigger_comments_updated_date
    BEFORE UPDATE ON comments
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for search_history table
CREATE TRIGGER trigger_search_history_updated_date
    BEFORE UPDATE ON search_history
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();

-- Trigger for recommendations table
CREATE TRIGGER trigger_recommendations_updated_date
    BEFORE UPDATE ON recommendations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_date_column();
