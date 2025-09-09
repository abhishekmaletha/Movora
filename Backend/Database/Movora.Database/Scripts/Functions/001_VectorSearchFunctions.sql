-- Vector Search Functions for Similarity Queries
-- Functions for semantic search using pgvector

-- Function to find similar movies based on embedding
CREATE OR REPLACE FUNCTION find_similar_movies(
    target_embedding VECTOR(1536),
    similarity_threshold FLOAT DEFAULT 0.7,
    max_results INTEGER DEFAULT 10
)
RETURNS TABLE (
    id UUID,
    title VARCHAR(500),
    description TEXT,
    release_date DATE,
    poster_url VARCHAR(1000),
    similarity_score FLOAT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        m.id,
        m.title,
        m.description,
        m.release_date,
        m.poster_url,
        1 - (m.embedding <=> target_embedding) as similarity_score
    FROM movies m
    WHERE m.embedding IS NOT NULL
        AND 1 - (m.embedding <=> target_embedding) >= similarity_threshold
    ORDER BY m.embedding <=> target_embedding
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql;

-- Function to find similar series based on embedding
CREATE OR REPLACE FUNCTION find_similar_series(
    target_embedding VECTOR(1536),
    similarity_threshold FLOAT DEFAULT 0.7,
    max_results INTEGER DEFAULT 10
)
RETURNS TABLE (
    id UUID,
    title VARCHAR(500),
    description TEXT,
    first_air_date DATE,
    poster_url VARCHAR(1000),
    similarity_score FLOAT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.id,
        s.title,
        s.description,
        s.first_air_date,
        s.poster_url,
        1 - (s.embedding <=> target_embedding) as similarity_score
    FROM series s
    WHERE s.embedding IS NOT NULL
        AND 1 - (s.embedding <=> target_embedding) >= similarity_threshold
    ORDER BY s.embedding <=> target_embedding
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql;

-- Function to find similar content (both movies and series)
CREATE OR REPLACE FUNCTION find_similar_content(
    target_embedding VECTOR(1536),
    similarity_threshold FLOAT DEFAULT 0.7,
    max_results INTEGER DEFAULT 10
)
RETURNS TABLE (
    id UUID,
    title VARCHAR(500),
    description TEXT,
    content_type TEXT,
    release_date DATE,
    poster_url VARCHAR(1000),
    similarity_score FLOAT
) AS $$
BEGIN
    RETURN QUERY
    (
        SELECT 
            m.id,
            m.title,
            m.description,
            'Movie'::TEXT as content_type,
            m.release_date,
            m.poster_url,
            1 - (m.embedding <=> target_embedding) as similarity_score
        FROM movies m
        WHERE m.embedding IS NOT NULL
            AND 1 - (m.embedding <=> target_embedding) >= similarity_threshold
        
        UNION ALL
        
        SELECT 
            s.id,
            s.title,
            s.description,
            'Series'::TEXT as content_type,
            s.first_air_date as release_date,
            s.poster_url,
            1 - (s.embedding <=> target_embedding) as similarity_score
        FROM series s
        WHERE s.embedding IS NOT NULL
            AND 1 - (s.embedding <=> target_embedding) >= similarity_threshold
    )
    ORDER BY similarity_score DESC
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql;

-- Function to get user's content preference vector (simplified)
CREATE OR REPLACE FUNCTION get_user_preference_vector(user_uuid UUID)
RETURNS VECTOR(1536) AS $$
DECLARE
    preference_vector VECTOR(1536);
BEGIN
    -- This is a simplified version - in practice, you'd calculate this based on
    -- user's watched content, ratings, and other behavioral data
    
    -- For now, return an average of embeddings from highly rated content
    SELECT AVG(
        CASE 
            WHEN r.content_type = 'Movie' THEN m.embedding
            WHEN r.content_type = 'Series' THEN s.embedding
        END
    )::VECTOR(1536)
    INTO preference_vector
    FROM reviews r
    LEFT JOIN movies m ON r.content_id = m.id AND r.content_type = 'Movie'
    LEFT JOIN series s ON r.content_id = s.id AND r.content_type = 'Series'
    WHERE r.user_id = user_uuid 
        AND r.rating >= 8
        AND (m.embedding IS NOT NULL OR s.embedding IS NOT NULL);
    
    RETURN preference_vector;
END;
$$ LANGUAGE plpgsql;

-- Add comments for documentation
COMMENT ON FUNCTION find_similar_movies(VECTOR, FLOAT, INTEGER) IS 'Find similar movies based on embedding vector similarity';
COMMENT ON FUNCTION find_similar_series(VECTOR, FLOAT, INTEGER) IS 'Find similar series based on embedding vector similarity';
COMMENT ON FUNCTION find_similar_content(VECTOR, FLOAT, INTEGER) IS 'Find similar content (movies and series) based on embedding vector similarity';
COMMENT ON FUNCTION get_user_preference_vector(UUID) IS 'Generate user preference vector based on their rating history';
