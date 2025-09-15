import React from 'react';

interface RatingProps {
  rating: number;
  maxRating?: number;
  className?: string;
}

export function Rating({ rating, maxRating = 10, className = '' }: RatingProps) {
  const normalizedRating = Math.max(0, Math.min(rating, maxRating));
  const displayRating = normalizedRating.toFixed(1);
  
  // Determine color based on rating
  const getRatingColor = (rating: number): string => {
    if (rating >= 8) return 'rating-excellent';
    if (rating >= 6.5) return 'rating-good';
    if (rating >= 5) return 'rating-average';
    return 'rating-poor';
  };

  const colorClass = getRatingColor(normalizedRating);

  return (
    <div className={`rating ${colorClass} ${className}`}>
      <span className="rating-value" aria-label={`Rating: ${displayRating} out of ${maxRating}`}>
        ★ {displayRating}
      </span>
    </div>
  );
}
