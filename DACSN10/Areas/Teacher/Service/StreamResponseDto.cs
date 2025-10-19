namespace DACSN10.Areas.Teacher.Service
{
    public class StreamResponseDto
    {
        public long? Id { get; set; }
        public string Key { get; set; }
        public string HlsUrl { get; set; }
        public bool Active { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
    }
}
