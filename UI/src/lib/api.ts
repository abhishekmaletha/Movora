import { FlexiSearchRequest, FlexiSearchResponse } from '../types';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:51818';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  async post<TRequest, TResponse>(
    endpoint: string,
    data: TRequest,
    signal?: AbortSignal
  ): Promise<TResponse> {
    const url = `${this.baseUrl}${endpoint}`;
    
    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
        signal,
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new ApiError(`HTTP ${response.status}: ${errorText}`, response.status);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          throw new ApiError('Request was cancelled');
        }
        throw new ApiError(error.message);
      }
      throw new ApiError('An unknown error occurred');
    }
  }

  async searchFlexi(
    query: string,
    signal?: AbortSignal
  ): Promise<FlexiSearchResponse> {
    return this.post<FlexiSearchRequest, FlexiSearchResponse>(
      '/api/search/flexi',
      { query },
      signal
    );
  }
}

// Custom error class
class ApiError extends Error {
  status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export const apiClient = new ApiClient();
export { ApiError };
