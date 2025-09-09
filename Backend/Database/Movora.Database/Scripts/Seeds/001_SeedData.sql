-- Sample Seed Data for Development and Testing
-- Run after all tables, indexes, and functions are created

-- Insert sample users
INSERT INTO users (id, email, google_id, preferences) VALUES
('550e8400-e29b-41d4-a716-446655440000', 'john.doe@example.com', 'google_123456789', '{"favoriteGenres": ["Action", "Sci-Fi"], "preferredLanguage": "en"}'),
('550e8400-e29b-41d4-a716-446655440001', 'jane.smith@example.com', 'google_987654321', '{"favoriteGenres": ["Drama", "Romance"], "preferredLanguage": "en"}'),
('550e8400-e29b-41d4-a716-446655440002', 'movie.lover@example.com', NULL, '{"favoriteGenres": ["Horror", "Thriller"], "preferredLanguage": "en"}');

-- Insert sample movies (using real TMDB IDs for reference)
INSERT INTO movies (id, tmdb_id, title, description, genres, release_date, poster_url) VALUES
('660e8400-e29b-41d4-a716-446655440000', 550, 'Fight Club', 'An insomniac office worker and a devil-may-care soapmaker form an underground fight club.', ARRAY['Drama', 'Thriller'], '1999-10-15', 'https://image.tmdb.org/t/p/w500/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg'),
('660e8400-e29b-41d4-a716-446655440001', 13, 'Forrest Gump', 'The presidencies of Kennedy and Johnson through the perspective of an Alabama man with an IQ of 75.', ARRAY['Drama', 'Romance'], '1994-07-06', 'https://image.tmdb.org/t/p/w500/arw2vcBveWOVZr6pxd9XTd1TdQa.jpg'),
('660e8400-e29b-41d4-a716-446655440002', 157336, 'Interstellar', 'A team of explorers travel through a wormhole in space in an attempt to ensure humanity''s survival.', ARRAY['Adventure', 'Drama', 'Sci-Fi'], '2014-11-07', 'https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg'),
('660e8400-e29b-41d4-a716-446655440003', 155, 'The Dark Knight', 'When the menace known as the Joker wreaks havoc on Gotham, Batman must accept one of the greatest psychological tests.', ARRAY['Action', 'Crime', 'Drama'], '2008-07-18', 'https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg');

-- Insert sample series
INSERT INTO series (id, tmdb_id, title, description, genres, first_air_date, poster_url) VALUES
('770e8400-e29b-41d4-a716-446655440000', 1399, 'Game of Thrones', 'Nine noble families fight for control over the lands of Westeros, while an ancient enemy returns.', ARRAY['Sci-Fi & Fantasy', 'Drama', 'Action & Adventure'], '2011-04-17', 'https://image.tmdb.org/t/p/w500/u3bZgnGQ9T01sWNhyveQz0wH0Hl.jpg'),
('770e8400-e29b-41d4-a716-446655440001', 1396, 'Breaking Bad', 'A high school chemistry teacher diagnosed with inoperable lung cancer turns to manufacturing drugs.', ARRAY['Drama', 'Crime'], '2008-01-20', 'https://image.tmdb.org/t/p/w500/ggFHVNu6YYI5L9pCfOacjizRGt.jpg'),
('770e8400-e29b-41d4-a716-446655440002', 66732, 'Stranger Things', 'When a young boy vanishes, a small town uncovers a mystery involving secret experiments.', ARRAY['Sci-Fi & Fantasy', 'Horror', 'Drama'], '2016-07-15', 'https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg');

-- Insert sample episodes for Breaking Bad
INSERT INTO episodes (id, series_id, season_number, episode_number, title, air_date, description) VALUES
('880e8400-e29b-41d4-a716-446655440000', '770e8400-e29b-41d4-a716-446655440001', 1, 1, 'Pilot', '2008-01-20', 'Walter White, a struggling high school chemistry teacher, is diagnosed with lung cancer.'),
('880e8400-e29b-41d4-a716-446655440001', '770e8400-e29b-41d4-a716-446655440001', 1, 2, 'Cat''s in the Bag...', '2008-01-27', 'Walt and Jesse attempt to tie up loose ends.'),
('880e8400-e29b-41d4-a716-446655440002', '770e8400-e29b-41d4-a716-446655440001', 1, 3, '...And the Bag''s in the River', '2008-02-10', 'Walter faces a difficult decision about what to do with Krazy-8.');

-- Insert sample user watched data
INSERT INTO user_watched (id, user_id, content_id, content_type, watched_date, episodes_watched, progress_percentage) VALUES
('990e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440000', '660e8400-e29b-41d4-a716-446655440000', 'Movie', '2024-01-15 20:30:00', NULL, 100.00),
('990e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440000', '770e8400-e29b-41d4-a716-446655440001', 'Series', '2024-01-20 21:00:00', 3, 15.00),
('990e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440001', '660e8400-e29b-41d4-a716-446655440001', 'Movie', '2024-01-18 19:45:00', NULL, 100.00);

-- Insert sample reviews
INSERT INTO reviews (id, user_id, content_id, content_type, rating, review_text, is_spoiler, helpfulness_score) VALUES
('aa0e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440000', '660e8400-e29b-41d4-a716-446655440000', 'Movie', 9, 'Incredible movie with amazing plot twists. Fight Club is a masterpiece!', FALSE, 15),
('aa0e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440001', '660e8400-e29b-41d4-a716-446655440001', 'Movie', 10, 'Tom Hanks delivers an outstanding performance. Heartwarming story!', FALSE, 12),
('aa0e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440000', '770e8400-e29b-41d4-a716-446655440001', 'Series', 10, 'Breaking Bad is simply the best TV series ever made. Perfect character development!', FALSE, 25);

-- Insert sample comments
INSERT INTO comments (id, user_id, review_id, comment_text) VALUES
('bb0e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440001', 'aa0e8400-e29b-41d4-a716-446655440000', 'I totally agree! The ending was mind-blowing.'),
('bb0e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440002', 'aa0e8400-e29b-41d4-a716-446655440001', 'Forrest Gump is such an emotional journey. Loved every minute of it.');

-- Insert sample search history
INSERT INTO search_history (id, user_id, query, search_type, result_count) VALUES
('cc0e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440000', 'action movies', 'text', 25),
('cc0e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440001', 'romantic comedies', 'text', 18),
('cc0e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440000', 'movies like inception', 'semantic', 12);

-- Insert sample recommendations
INSERT INTO recommendations (id, user_id, content_id, content_type, score, reasoning, recommendation_type) VALUES
('dd0e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440000', '660e8400-e29b-41d4-a716-446655440002', 'Movie', 0.92, 'Based on your love for psychological thrillers and sci-fi elements, Interstellar combines complex themes with emotional depth.', 'content-based'),
('dd0e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440001', '770e8400-e29b-41d4-a716-446655440002', 'Series', 0.85, 'Users with similar taste in emotional dramas also enjoyed Stranger Things for its character development and nostalgic themes.', 'collaborative'),
('dd0e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440000', '660e8400-e29b-41d4-a716-446655440003', 'Movie', 0.89, 'Given your high ratings for complex narratives and character studies, The Dark Knight offers similar psychological depth.', 'hybrid');

-- Add comments for seed data context
COMMENT ON TABLE users IS 'Sample users for development and testing';
COMMENT ON TABLE movies IS 'Sample movies with real TMDB IDs for API integration testing';
COMMENT ON TABLE series IS 'Sample TV series with real TMDB IDs for API integration testing';
