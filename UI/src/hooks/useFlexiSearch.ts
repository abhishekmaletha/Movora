import { useState, useCallback, useRef, useEffect } from 'react';
import { apiClient, ApiError } from '../lib/api';
import { SearchResult } from '../types';

interface UseFlexiSearchReturn {
  query: string;
  setQuery: (query: string) => void;
  results: SearchResult[];
  isLoading: boolean;
  error: string | null;
  search: () => void;
  clearResults: () => void;
}

const STORAGE_KEY = 'movora_last_search_query';

export function useFlexiSearch(): UseFlexiSearchReturn {
  const [query, setQuery] = useState(() => {
    // Load last query from localStorage
    try {
      return localStorage.getItem(STORAGE_KEY) || '';
    } catch {
      return '';
    }
  });
  
  const [results, setResults] = useState<SearchResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Save query to localStorage whenever it changes
  useEffect(() => {
    try {
      if (query.trim()) {
        localStorage.setItem(STORAGE_KEY, query);
      }
    } catch {
      // Ignore localStorage errors
    }
  }, [query]);

  const search = useCallback(async () => {
    const trimmedQuery = query.trim();
    if (!trimmedQuery) {
      setError('Please enter a search query');
      return;
    }

    // Cancel any existing request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new abort controller
    abortControllerRef.current = new AbortController();

    setIsLoading(true);
    setError(null);

    try {
      const response = await apiClient.searchFlexi(
        trimmedQuery,
        abortControllerRef.current.signal
      );
      
      setResults(response.results);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.message !== 'Request was cancelled') {
          setError(err.message);
        }
      } else {
        setError('An unexpected error occurred');
      }
    } finally {
      setIsLoading(false);
    }
  }, [query]);

  const clearResults = useCallback(() => {
    setResults([]);
    setError(null);
    
    // Cancel any pending request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    
    setIsLoading(false);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  return {
    query,
    setQuery,
    results,
    isLoading,
    error,
    search,
    clearResults,
  };
}
