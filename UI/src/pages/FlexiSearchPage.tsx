import React, { useEffect, useRef } from 'react';
import { SearchBar } from '../components/SearchBar';
import { MovieGrid } from '../components/MovieGrid';
import { useFlexiSearch } from '../hooks/useFlexiSearch';

export function FlexiSearchPage() {
  const {
    query,
    setQuery,
    results,
    isLoading,
    error,
    search,
    clearResults
  } = useFlexiSearch();

  const gridRef = useRef<HTMLDivElement>(null);

  // Focus on grid after results load (accessibility)
  useEffect(() => {
    if (!isLoading && results.length > 0 && gridRef.current) {
      gridRef.current.focus();
    }
  }, [isLoading, results.length]);

  const handleQueryChange = (newQuery: string) => {
    setQuery(newQuery);
    
    // Clear results if query is empty
    if (!newQuery.trim()) {
      clearResults();
    }
  };

  return (
    <div className="flexi-search-page">
      <header className="page-header">
        <h1 className="page-title">
          <span className="logo">🎬</span>
          Movora
        </h1>
        <p className="page-subtitle">
          Discover movies and TV shows with intelligent search
        </p>
      </header>

      <main className="page-main">
        <SearchBar
          query={query}
          onQueryChange={handleQueryChange}
          onSearch={search}
          isLoading={isLoading}
        />

        {error && (
          <div className="error-message" role="alert">
            <span className="error-icon">⚠️</span>
            {error}
          </div>
        )}

        <div className="results-section" ref={gridRef} tabIndex={-1}>
          {(results.length > 0 || isLoading) && (
            <>
              <div className="results-header">
                <h2 className="results-title">
                  {isLoading ? 'Searching...' : `Found ${results.length} result${results.length === 1 ? '' : 's'}`}
                </h2>
              </div>
              
              <MovieGrid
                results={results}
                isLoading={isLoading}
                className="main-grid"
              />
            </>
          )}
        </div>
      </main>
    </div>
  );
}
