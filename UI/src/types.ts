export interface SearchResult {
  tmdbId: number;
  name: string;
  mediaType: 'movie' | 'tv';
  thumbnailUrl: string;
  rating: number;
  overview: string;
  year: number;
  relevanceScore: number;
  reasoning: string;
}

export interface FlexiSearchResponse {
  results: SearchResult[];
  traceId: string;
}

export interface FlexiSearchRequest {
  query: string;
}
