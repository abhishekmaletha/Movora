import React from 'react';
import { SearchResult } from '../types';
import { MovieCard } from './MovieCard';

interface MovieGridProps {
  results: SearchResult[];
  isLoading: boolean;
  className?: string;
}

function SkeletonCard() {
  return (
    <div className="movie-card skeleton">
      <div className="movie-card-image-container skeleton-image">
        <div className="skeleton-shimmer"></div>
      </div>
      <div className="movie-card-content">
        <div className="skeleton-text skeleton-title"></div>
        <div className="skeleton-text skeleton-year"></div>
        <div className="skeleton-text skeleton-rating"></div>
        <div className="skeleton-text skeleton-overview"></div>
        <div className="skeleton-text skeleton-overview-short"></div>
      </div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="empty-state">
      <div className="empty-state-icon">🎬</div>
      <h3 className="empty-state-title">No results found</h3>
      <p className="empty-state-message">
        Try searching for a different movie or TV show
      </p>
    </div>
  );
}

export function MovieGrid({ results, isLoading, className = '' }: MovieGridProps) {
  if (isLoading) {
    return (
      <div className={`movie-grid ${className}`} aria-label="Loading search results">
        {Array.from({ length: 8 }, (_, index) => (
          <SkeletonCard key={index} />
        ))}
      </div>
    );
  }

  if (results.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className={`movie-grid ${className}`} role="grid" aria-label="Search results">
      {results.map((result) => (
        <MovieCard
          key={`${result.mediaType}-${result.tmdbId}`}
          result={result}
        />
      ))}
    </div>
  );
}
