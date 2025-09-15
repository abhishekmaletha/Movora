import React, { KeyboardEvent } from 'react';

interface SearchBarProps {
  query: string;
  onQueryChange: (query: string) => void;
  onSearch: () => void;
  isLoading: boolean;
  placeholder?: string;
}

export function SearchBar({
  query,
  onQueryChange,
  onSearch,
  isLoading,
  placeholder = "Search for movies and TV shows..."
}: SearchBarProps) {
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault();
      onSearch();
    }
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    onSearch();
  };

  return (
    <div role="search" className="search-container">
      <form onSubmit={handleSubmit} className="search-form">
        <div className="search-input-container">
          <input
            type="text"
            role="searchbox"
            aria-label="Search movies and TV shows"
            placeholder={placeholder}
            value={query}
            onChange={(e) => onQueryChange(e.target.value)}
            onKeyDown={handleKeyDown}
            className="search-input"
            disabled={isLoading}
          />
          <button
            type="submit"
            className="search-button"
            disabled={isLoading || !query.trim()}
            aria-label="Search"
          >
            {isLoading ? (
              <span className="loading-spinner" aria-hidden="true">⟳</span>
            ) : (
              <span aria-hidden="true">🔍</span>
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
