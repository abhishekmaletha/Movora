namespace Movora.Application.Dtos
{
    public class MovieDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public List<string> Cast { get; set; }
        public decimal Rating { get; set; }
    }
}