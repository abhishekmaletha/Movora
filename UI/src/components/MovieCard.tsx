import React from 'react';
import { SearchResult } from '../types';
import { Rating } from './Rating';

interface MovieCardProps {
  result: SearchResult;
  className?: string;
}

export function MovieCard({ result, className = '' }: MovieCardProps) {
  const {
    name,
    year,
    rating,
    thumbnailUrl,
    mediaType,
    overview
  } = result;

  const handleImageError = (event: React.SyntheticEvent<HTMLImageElement>) => {
    event.currentTarget.src = '/placeholder-poster.svg';
    event.currentTarget.alt = `${name} poster not available`;
  };

  return (
    <div className={`movie-card ${className}`}>
      <div className="movie-card-image-container">
        <img
          src={thumbnailUrl}
          alt={`${name} poster`}
          className="movie-card-image"
          onError={handleImageError}
          loading="lazy"
        />
        <div className="media-type-badge">
          {mediaType === 'movie' ? '🎬' : '📺'}
        </div>
      </div>
      
      <div className="movie-card-content">
        <div className="movie-card-header">
          <h3 className="movie-card-title" title={name}>
            {name}
          </h3>
          <span className="movie-card-year">
            {year}
          </span>
        </div>
        
        <div className="movie-card-rating">
          <Rating rating={rating} />
        </div>
        
        {overview && (
          <p className="movie-card-overview" title={overview}>
            {overview}
          </p>
        )}
      </div>
    </div>
  );
}
