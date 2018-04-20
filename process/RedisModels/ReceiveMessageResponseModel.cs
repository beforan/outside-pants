namespace process.RedisModels
{
    public class ReceiveMessageResponseModel
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public long rc { get; set; }
        public long fr { get; set; }
        public long sent { get; set; }
    }
}
