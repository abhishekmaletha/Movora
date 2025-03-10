// add namespace here
namespace Movora.Application.AI.Interfaces
{
    public interface ILLM
    {
        Task<List<string>> GetMovieResponseASync(string message, string model);
    }
}